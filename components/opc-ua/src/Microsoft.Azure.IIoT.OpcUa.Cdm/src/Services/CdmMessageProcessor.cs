// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cdm.Services {
    using Microsoft.Azure.IIoT.OpcUa.Cdm;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.CommonDataModel.ObjectModel.Cdm;
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.Storage;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Process messages and write them to Datalake.
    /// </summary>
    public class CdmMessageProcessor : ISubscriberMessageProcessor {

        /// <summary>
        /// Create the cdm message processor
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="encoder"></param>
        /// <param name="logger"></param>
        public CdmMessageProcessor(IStorageAdapter storage, IRecordEncoder encoder,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _lock = new SemaphoreSlim(1, 1);
            _samplesCacheSize = 0;
            _cacheUploadTriggered = false;
            _samplesCache = new Dictionary<string, List<MonitoredItemMessageModel>>();
            _dataSetsCache = new Dictionary<string, List<DataSetMessageModel>>();
            _cdmCorpus = new CdmCorpusDefinition();
            var cdmLogger = _logger.ForContext(typeof(CdmStatusLevel));
            _cdmCorpus.SetEventCallback(new EventCallback {
                Invoke = (level, msg) => LogCdm(cdmLogger, level, msg)
            });
            _cdmCorpus.Storage.Mount("adls", _storage.Adapter);
            var gitAdapter = new CdmStandardsAdapter();
            _cdmCorpus.Storage.Mount("cdm", gitAdapter);
            _cdmCorpus.Storage.DefaultNamespace = "adls";
            _cdmCorpus.AppId = "Azure Industrial IoT";
            _manifestResolved = null;
            _cacheUploadInterval = TimeSpan.FromSeconds(20);
            _cacheUploadTimer = new Timer(CacheTimer_ElapsedAsync, null,
                _cacheUploadInterval, Timeout.InfiniteTimeSpan);
        }

        /// <inheritdoc/>
        public Task HandleSampleAsync(MonitoredItemMessageModel sample) {
            return ProcessCdmSampleAsync(sample);
        }

        /// <inheritdoc/>
        public Task HandleMessageAsync(DataSetMessageModel message) {
            return ProcessCdmSampleAsync(message);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _cacheUploadTimer.Dispose();
            PerformWriteCacheAsync().Wait();
            _lock.Dispose();
        }

        /// <summary>
        /// Perform write from cache
        /// </summary>
        /// <returns></returns>
        private async Task PerformWriteCacheAsync() {
            if (_samplesCacheSize == 0) {
                _logger.Verbose("End sending processed CDM data - empty buffer");
                return;
            }
            var writeManifest = false;
            try {
                while (true) {
                    await _storage.LockAsync(kManifestFileName);
                    try {
                        var manifest = await CreateOrOpenManifestAsync(kManifestFileName);

                        var sw = Stopwatch.StartNew();
                        _logger.Debug("Writing processed CDM data ...");
                        foreach (var record in _samplesCache) {
                            if (record.Value.Count == 0 || record.Value[0] == null) {
                                continue;
                            }
                            writeManifest |= await WriteRecordToPartitionAsync(
                                manifest, record.Key, record.Value);
                            record.Value.Clear();
                        }
                        foreach (var record in _dataSetsCache) {
                            if (record.Value.Count == 0 || record.Value[0] == null) {
                                continue;
                            }
                            writeManifest |= await WriteRecordToPartitionAsync(
                                manifest, record.Key, record.Value);
                            record.Value.Clear();
                        }
                        if (writeManifest) {
                            await manifest.SaveAsAsync(kManifestFileName, true);
                        }

                        _logger.Information("Finished writing CDM data records - took {elapsed}).",
                            sw.Elapsed);
                        return;
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Failed to send processed CDM data - try again...");
                        continue;
                    }
                    finally {
                        await _storage.UnlockAsync(kManifestFileName);
                    }
                }
            }
            catch { }
            finally {
                _samplesCache.Clear();
                _dataSetsCache.Clear();
                _samplesCacheSize = 0;
            }
        }

        /// <summary>
        /// Write records
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="manifest"></param>
        /// <param name="partitionKey"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        private async Task<bool> WriteRecordToPartitionAsync<T>(CdmManifestDefinition manifest,
            string partitionKey, IList<T> record) {
            var retry = false;
            var persist = false;
            var dataSetRecordList = record as List<DataSetMessageModel>;
            var samplesRecordList = record as List<MonitoredItemMessageModel>;
            var partition = (dataSetRecordList != null)
                ? GetOrCreateEntityDataPartition(manifest, partitionKey, dataSetRecordList[0], out persist, retry)
                : GetOrCreateEntityDataPartition(manifest, partitionKey, samplesRecordList[0], out persist, retry);
            if (partition == null) {
                _logger.Error("Failed to create CDM Entity for {key} records).", partitionKey);
                return persist;
            }
            var csvTrait = partition.ExhibitsTraits.Item("is.partition.format.CSV");
            var partitionDelimitor = csvTrait?.Arguments?.FetchValue("delimiter") ?? kCsvPartitionsDelimiter;
            if (dataSetRecordList != null) {
                await _storage.WriteAsync(partition.Location, first =>
                    _encoder.Encode<DataSetMessageModel>(dataSetRecordList, partitionDelimitor, first));
            }
            else {
                await _storage.WriteAsync(partition.Location, first =>
                    _encoder.Encode<MonitoredItemMessageModel>(samplesRecordList, partitionDelimitor, first));
            }
            _logger.Information("successfully processed {count} records and written as CDM.", record.Count);
            return persist;
        }

        /// <inheritdoc/>
        private string GetNormalizedEntityName(MonitoredItemMessageModel sample) {
            if (string.IsNullOrEmpty(sample.PublisherId) ||
                string.IsNullOrEmpty(sample.DataSetWriterId) ||
                string.IsNullOrEmpty(sample.NodeId)) {
                return null;
            }
            return GetNormalizedKey($"{sample.PublisherId}_{sample.DataSetWriterId}" +
                $"_{sample.NodeId}");
        }

        /// <inheritdoc/>
        private string GetNormalizedEntityName(DataSetMessageModel dataSet) {
            if (string.IsNullOrEmpty(dataSet.PublisherId) ||
                string.IsNullOrEmpty(dataSet.DataSetWriterId)) {
                return null;
            }
            return GetNormalizedKey($"{dataSet.PublisherId}_{dataSet.DataSetWriterId}" +
                $"_{dataSet.DataSetClassId}_{dataSet.MetaDataVersion}");
        }

        /// <inheritdoc/>
        private string GetNormalizedKey(string key) {
            key = string.Join("", key.Split(Path.GetInvalidFileNameChars()));
            return key.Replace('#', '_').Replace('.', '_')
                .Replace('/', '_').Replace(':', '_')
                .Replace('"', '_').Replace('\'', '_')
                .Replace('\\', '_');
        }

        /// <summary>
        /// Get cdm type from variant value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static CdmDataFormat VariantValueTypeToCdmDataFormat(VariantValue value) {
            if (value != null && value.TryGetValue(out var raw)) {
                return DataTypeToCdmDataFormat(raw.GetType());
            }
            var typeCode = value?.GetTypeCode() ?? TypeCode.Empty;
            switch (typeCode) {
                case TypeCode.Single:
                    return CdmDataFormat.Float;
                case TypeCode.Double:
                    return CdmDataFormat.Double;
                case TypeCode.String:
                    return CdmDataFormat.String;
                case TypeCode.Decimal:
                    return CdmDataFormat.Decimal;
                case TypeCode.Boolean:
                    return CdmDataFormat.Boolean;
                default:
                    return CdmDataFormat.String;
            }
        }

        /// <summary>
        /// Get cdm type from type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static CdmDataFormat DataTypeToCdmDataFormat(Type type) {
            var typeCode = Type.GetTypeCode(type);
            if (typeCode == TypeCode.Object) {
                typeCode = Type.GetTypeCode(Nullable.GetUnderlyingType(type));
            }
            switch (typeCode) {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return CdmDataFormat.Int16;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return CdmDataFormat.Int32;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return CdmDataFormat.Int64;
                case TypeCode.Single:
                    return CdmDataFormat.Float;
                case TypeCode.Double:
                    return CdmDataFormat.Double;
                case TypeCode.Char:
                    return CdmDataFormat.Char;
                case TypeCode.String:
                    return CdmDataFormat.String;
                case TypeCode.Decimal:
                    return CdmDataFormat.Decimal;
                case TypeCode.DateTime:
                    return CdmDataFormat.DateTime;
                case TypeCode.Byte:
                case TypeCode.SByte:
                    //  TODO: CDM SDK bug - does not accept Byte for now ...
                    //return CdmDataFormat.Byte;
                    return CdmDataFormat.Int16;
                case TypeCode.Boolean:
                    return CdmDataFormat.Boolean;
                default:
                    // treat anything else as cdm string string
                    return CdmDataFormat.String;
            }
        }

        /// <summary>
        /// Data type to cdm data string
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        private static string DataTypeToCdmDataString(CdmDataFormat? dataType) {
            switch (dataType) {
                case CdmDataFormat.Byte:
                case CdmDataFormat.Int16:
                case CdmDataFormat.Int32:
                case CdmDataFormat.Int64:
                    return "int64";
                case CdmDataFormat.Float:
                case CdmDataFormat.Double:
                    return "double";
                case CdmDataFormat.Char:
                case CdmDataFormat.String:
                    return "string";
                case CdmDataFormat.Guid:
                    return "guid";
                case CdmDataFormat.Binary:
                    return "boolean";
                case CdmDataFormat.Time:
                case CdmDataFormat.Date:
                case CdmDataFormat.DateTime:
                    return "dateTime";
                case CdmDataFormat.DateTimeOffset:
                    return "dateTimeOffset";
                case CdmDataFormat.Boolean:
                    return "boolean";
                case CdmDataFormat.Decimal:
                    return "decimal";
                case CdmDataFormat.Json:
                    return "json";
                default:
                    return "unclassified";
            }
        }

        /// <inheritdoc/>
        private CdmDataPartitionDefinition GetOrCreateEntityDataPartition(CdmManifestDefinition manifest,
            string key, MonitoredItemMessageModel sample, out bool persist, bool forceNew = false) {

            persist = false;
            if (string.IsNullOrEmpty(key) || sample == null) {
                return null;
            }

            // check if the enetity was aleready added
            var entityDefinition = manifest.Entities.Item(key);
            if (entityDefinition == null) {
                // add a new entity for the sample

                // add a new entity for the Message
                var newSampleEntity = _cdmCorpus.MakeObject<CdmEntityDefinition>(
                    CdmObjectType.EntityDef, key, false);

                var info = typeof(MonitoredItemMessageModel).GetProperties();
                foreach (var property in info) {
                    // add the attributes required
                    var attribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                        CdmObjectType.TypeAttributeDef, property.Name, false);
                    attribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                        CdmObjectType.PurposeRef, "hasA", true);
                    //  if we handle a value, lookup it's type property
                    if (property.Name == "Value") {
                        attribute.DataFormat = VariantValueTypeToCdmDataFormat(sample.Value);
                    }
                    else {
                        attribute.DataFormat = DataTypeToCdmDataFormat(property.PropertyType);
                    }

                    newSampleEntity.Attributes.Add(attribute);
                }

                newSampleEntity.DisplayName = kPublisherSampleEntityName;
                newSampleEntity.Version = "0.0.2";
                newSampleEntity.Description = "Opc Ua Monitored Item Sample";

                // Create a new document where the new entity's definition will be stored
                var newSampleEntityDoc = _cdmCorpus.MakeObject<CdmDocumentDefinition>(
                    CdmObjectType.DocumentDef, $"{newSampleEntity.EntityName}.cdm.json", false);
                newSampleEntityDoc.Imports.Add($"{newSampleEntity.EntityName}.cdm.json");
                newSampleEntityDoc.Imports.Add(kFoundationJsonPath);
                newSampleEntityDoc.Definitions.Add(newSampleEntity);
                _cdmCorpus.Storage.FetchRootFolder("adls").Documents.Add(newSampleEntityDoc);
                entityDefinition = manifest.Entities.Add(newSampleEntity);
                persist |= true;
            }

            var partition = entityDefinition.DataPartitions.Count != 0
                ? entityDefinition.DataPartitions.Last() : null;
            if (forceNew || partition == null) {
                // Define a partition and add it to the local declaration
                var newPartition = _cdmCorpus.MakeObject<CdmDataPartitionDefinition>(
                    CdmObjectType.DataPartitionDef, entityDefinition.EntityName);
                var timestamp = DateTime.UtcNow.ToString(
                    "yyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);
                newPartition.Location =
                    $"adls:/{entityDefinition.EntityName}/partition-data-{timestamp}.csv";
                newPartition.Explanation = "OPC UA PubSub DataSet Partition";
                var partitionTrait = newPartition.ExhibitsTraits.Add(
                    "is.partition.format.CSV");
                partitionTrait.Arguments.Add("columnHeaders", "true");
                partitionTrait.Arguments.Add("delimiter", kCsvPartitionsDelimiter);
                partition = entityDefinition.DataPartitions.Add(newPartition);
                persist |= true;
            }
            return partition;
        }

        /// <inheritdoc/>
        private CdmDataPartitionDefinition GetOrCreateEntityDataPartition(CdmManifestDefinition manifest,
            string key, DataSetMessageModel dataSet, out bool persist, bool forceNew = false) {

            persist = false;
            if (string.IsNullOrEmpty(key) || dataSet == null) {
                return null;
            }

            // check if the entity was already added
            var entityDefinition = manifest.Entities.Item(key);
            if (entityDefinition == null) {
                // add a new entity for the DataSet
                var newDataSetEntity = _cdmCorpus.MakeObject<CdmEntityDefinition>(
                    CdmObjectType.EntityDef, key, false);

                var properties = typeof(DataSetMessageModel).GetProperties();
                foreach (var property in properties) {
                    if (property.Name != nameof(DataSetMessageModel.Payload)) {
                        // add the attributes required
                        var attribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                        CdmObjectType.TypeAttributeDef, property.Name, false);
                        attribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                            CdmObjectType.PurposeRef, "hasA", true);
                        attribute.DataFormat = DataTypeToCdmDataFormat(property.PropertyType);
                        newDataSetEntity.Attributes.Add(attribute);
                    }
                    else {
                        //  Parse the message payload
                        foreach (var node in dataSet.Payload.OrderBy(i => i.Key)) {
                            // add the attributes for value, status and timestamp
                            var valueAttribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                                CdmObjectType.TypeAttributeDef, $"{node.Key}_value", false);
                            valueAttribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                                CdmObjectType.PurposeRef, "hasA", true);
                            valueAttribute.DataFormat = VariantValueTypeToCdmDataFormat(node.Value.Value);
                            newDataSetEntity.Attributes.Add(valueAttribute);

                            var statusAttribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                                CdmObjectType.TypeAttributeDef, $"{node.Key}_status", false);
                            statusAttribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                                CdmObjectType.PurposeRef, "hasA", true);
                            statusAttribute.DataFormat = DataTypeToCdmDataFormat(typeof(string));
                            newDataSetEntity.Attributes.Add(statusAttribute);

                            var sourceTimestampAttribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                                CdmObjectType.TypeAttributeDef, $"{node.Key}_sourceTimestamp", false);
                            sourceTimestampAttribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                                CdmObjectType.PurposeRef, "hasA", true);
                            sourceTimestampAttribute.DataFormat = DataTypeToCdmDataFormat(typeof(DateTime));
                            newDataSetEntity.Attributes.Add(sourceTimestampAttribute);

                            var serverTimestampAttribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                                CdmObjectType.TypeAttributeDef, $"{node.Key}_serverTimestamp", false);
                            serverTimestampAttribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                                CdmObjectType.PurposeRef, "hasA", true);
                            serverTimestampAttribute.DataFormat = DataTypeToCdmDataFormat(typeof(DateTime));
                            newDataSetEntity.Attributes.Add(serverTimestampAttribute);
                        }
                    }
                }

                newDataSetEntity.DisplayName = kPublisherDataSetEntityName;
                newDataSetEntity.Version = "0.0.1";
                newDataSetEntity.Description = "OPC UA PubSub DataSet Entity";

                // Create a new document where the new entity's definition will be stored
                var newEntityDoc = _cdmCorpus.MakeObject<CdmDocumentDefinition>(
                    CdmObjectType.DocumentDef, $"{newDataSetEntity.EntityName}.cdm.json", false);
                newEntityDoc.Imports.Add($"{newDataSetEntity.EntityName}.cdm.json");
                newEntityDoc.Imports.Add(kFoundationJsonPath);
                newEntityDoc.Definitions.Add(newDataSetEntity);
                _cdmCorpus.Storage.FetchRootFolder("adls").Documents.Add(newEntityDoc);
                entityDefinition = manifest.Entities.Add(newDataSetEntity);
                persist |= true;
            }

            var partition = entityDefinition.DataPartitions.Count != 0
                ? entityDefinition.DataPartitions.Last() : null;
            if (forceNew || partition == null) {
                // Define a partition and add it to the local declaration
                var newPartition = _cdmCorpus.MakeObject<CdmDataPartitionDefinition>(
                    CdmObjectType.DataPartitionDef, entityDefinition.EntityName);
                var timestamp = DateTime.UtcNow.ToString(
                    "yyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);
                newPartition.Location =
                    $"adls:/{entityDefinition.EntityName}/partition-data-{timestamp}.csv";
                newPartition.Explanation = "OPC UA PubSub DataSet Partition";
                var partitionTrait = newPartition.ExhibitsTraits.Add(
                    "is.partition.format.CSV");
                partitionTrait.Arguments.Add("columnHeaders", "true");
                partitionTrait.Arguments.Add("delimiter", kCsvPartitionsDelimiter);
                partition = entityDefinition.DataPartitions.Add(newPartition);
                persist |= true;
            }
            return partition;
        }

        /// <summary>
        /// Create manifest
        /// </summary>
        /// <returns></returns>
        private async Task<CdmManifestDefinition> CreateOrOpenManifestAsync(string fileName) {
            // Load the model.json file from file system

            var sw = Stopwatch.StartNew();
            _logger.Information("Fetching manifest ...");
            var manifest = await _cdmCorpus.FetchObjectAsync<CdmManifestDefinition>(
                "adls:/" + fileName, _manifestResolved, true);

            if (manifest == null) {
                _logger.Information("Could not find manifest after {elapsed}", sw.Elapsed);
                sw.Restart();
                //  no manifest loaded from the storage
                var adlsRoot = _cdmCorpus.Storage.FetchRootFolder("adls");
                if (adlsRoot == null) {
                    // unable to retrieve the root folder
                    return null;
                }
                _logger.Information("Fetching root folder took {elapsed}", sw.Elapsed);
                sw.Restart();

                // create a new Manifest definition
                manifest = _cdmCorpus.MakeObject<CdmManifestDefinition>(
                    CdmObjectType.ManifestDef, "IIoTOpcUaPubSub");
                _logger.Information("Making new manifest took {elapsed}", sw.Elapsed);

                if (manifest != null) {
                    manifest.Name = "IIoTOpcUaPubSub";
                    manifest.ManifestName = "IIoT OPC UA PubSub Manifest";
                    manifest.Imports.Add(kFoundationJsonPath);
                    manifest.Schema = "cdm:/schema.cdm.json";
                    manifest.JsonSchemaSemanticVersion = "1.0.0";
                    if (adlsRoot.Documents.Item(manifest.Name) == null) {
                        Try.Op(() => adlsRoot.Documents.Add(manifest));
                    }

                    sw.Restart();
                    await manifest.SaveAsAsync(fileName, true);
                    _logger.Information("Saving manifest took {elapsed}", sw.Elapsed);
                    _manifestResolved = manifest;
                }
            }
            else {
                _logger.Information("Loading manifest took {elapsed}", sw.Elapsed);
            }

            return manifest;
        }

        /// <summary>
        /// Cache Timer Elapesed handler
        /// </summary>
        /// <param name="sender"></param>
        private async void CacheTimer_ElapsedAsync(object sender) {
            await _lock.WaitAsync();
            try {
                _cacheUploadTriggered = true;
                await PerformWriteCacheAsync();
            }
            finally {
                Try.Op(() => _cacheUploadTimer.Change(_cacheUploadInterval, Timeout.InfiniteTimeSpan));
                _cacheUploadTriggered = false;
                _lock.Release();
            }
        }

        /// <summary>
        /// Process sample
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="payload"></param>
        /// <returns></returns>
        private async Task ProcessCdmSampleAsync<T>(T payload) {
            await _lock.WaitAsync();
            try {
                if (payload is MonitoredItemMessageModel sample) {

                    var key = GetNormalizedEntityName(sample);
                    if (!_samplesCache.TryGetValue(key, out var samplesList)) {
                        _samplesCache[key] = new List<MonitoredItemMessageModel>();
                    }
                    _samplesCache[key].Add(sample);
                }
                else if (payload is DataSetMessageModel dataSet) {
                    var key = GetNormalizedEntityName(dataSet);
                    if (!_dataSetsCache.TryGetValue(key, out var dataSetList)) {
                        _dataSetsCache[key] = new List<DataSetMessageModel>();
                    }
                    _dataSetsCache[key].Add(dataSet);
                }
                else {
                    throw new ArgumentException("Invalid payload type");
                }
                _samplesCacheSize++;
                if (!_cacheUploadTriggered && _samplesCacheSize >= kSamplesCacheMaxSize) {
                    Try.Op(() => _cacheUploadTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan));
                    _cacheUploadTriggered = true;
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Log cdm messages
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        private void LogCdm(ILogger logger, CdmStatusLevel level, string msg) {
            switch (level) {
                case CdmStatusLevel.Error:
                    logger.Error("{msg}", msg);
                    break;
                case CdmStatusLevel.Warning:
                    logger.Warning("{msg}", msg);
                    break;
                case CdmStatusLevel.Progress:
                    logger.Verbose("{msg}", msg);
                    break;
                case CdmStatusLevel.Info:
                    if (msg.StartsWith("CdmCorpusDefinition")) {
                        logger.Verbose("{msg}", msg);
                    }
                    else {
                        logger.Information("{msg}", msg);
                    }
                    break;
            }
        }

        private readonly CdmCorpusDefinition _cdmCorpus;
        private CdmManifestDefinition _manifestResolved;
        private readonly ILogger _logger;
        private readonly IRecordEncoder _encoder;
        private readonly IStorageAdapter _storage;
        private readonly SemaphoreSlim _lock;
        private readonly Timer _cacheUploadTimer;
        private readonly TimeSpan _cacheUploadInterval;
        private bool _cacheUploadTriggered;
        private int _samplesCacheSize;
        private readonly Dictionary<string, List<MonitoredItemMessageModel>> _samplesCache;
        private readonly Dictionary<string, List<DataSetMessageModel>> _dataSetsCache;

        private const int kSamplesCacheMaxSize = 5000;
        private const string kManifestFileName = "model.json";
        private const string kPublisherDataSetEntityName = "OpcUaPubSubDataSet";
        private const string kPublisherSampleEntityName = "OpcUaPubSubSample";
        private const string kFoundationJsonPath = "cdm:/foundations.cdm.json";
        private const string kCsvPartitionsDelimiter = ",";
    }
}
