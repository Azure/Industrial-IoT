// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cdm.Services {
    using Microsoft.Azure.IIoT.Cdm;
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
    public class CdmMessageProcessor : ICdmClient {

        /// <summary>
        /// Create the cdm message processor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="storage"></param>
        public CdmMessageProcessor(ICdmClientConfig config,
            ILogger logger, IAdlsStorage storage) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            _lock = new SemaphoreSlim(1, 1);
            _samplesCacheSize = 0;
            _cacheUploadTimer = new Timer(CacheTimer_ElapesedAsync);
            _cacheUploadTriggered = false;
            _cacheUploadInterval = TimeSpan.FromSeconds(20);
            _samplesCache = new Dictionary<string, List<MonitoredItemMessageModel>>();
            _dataSetsCache = new Dictionary<string, List<DataSetMessageModel>>();

            _cdmCorpus = new CdmCorpusDefinition();

            var cdmLogger = _logger.ForContext(typeof(CdmStatusLevel));
            _cdmCorpus.SetEventCallback(new EventCallback {
                Invoke = (level, msg) => {
                    switch (level) {
                        case CdmStatusLevel.Error:
                            cdmLogger.Error("CDM message: {0}", msg);
                            break;
                        case CdmStatusLevel.Warning:
                            cdmLogger.Warning("CDM message: {0}", msg);
                            break;
                        case CdmStatusLevel.Progress:
                            cdmLogger.Verbose("CDM message: {0}", msg);
                            break;
                        case CdmStatusLevel.Info:
                            cdmLogger.Debug("CDM message: {0}", msg);
                            break;
                    }
                }
            });

            _adapter = new ADLSAdapter($"{config.ADLSg2HostName}",
                $"/{config.ADLSg2ContainerName}/{config.RootFolder}",
                config.TenantId, config.AppId, config.AppSecret);
            _cdmCorpus.Storage.Mount("adls", _adapter);
            var gitAdapter = new GithubAdapter();
            _cdmCorpus.Storage.Mount("cdm", gitAdapter);
            _cdmCorpus.Storage.DefaultNamespace = "adls";
        }

        /// <summary>
        /// Processes the payload message from the IoTHub for storage
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="properties"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        public async Task ProcessAsync<T>(T payload,
            IDictionary<string, string> properties = null,
            string partitionKey = null) {
            await ProcessCdmSampleAsync(payload);
        }

        /// <summary>
        /// Open and load the cdm repository
        /// </summary>
        /// <returns></returns>
        public async Task OpenAsync() {

            _logger.Information($"Open CDM Processor ...");
            // Load the model.json file from file system
            Manifest = await _cdmCorpus.FetchObjectAsync<CdmManifestDefinition>(
                "adls:/model.json");

            if (Manifest == null) {
                //  no manifest loaded from the storage
                var adlsRoot = _cdmCorpus.Storage.FetchRootFolder("adls");
                if (adlsRoot == null) {
                    // unable to retrieve the root folder
                    return;
                }

                // validate if the root already exist
                await _storage.CreateBlobRoot(_config.ADLSg2HostName,
                    _config.ADLSg2ContainerName, _config.RootFolder);

                // create a new Manifest definition
                Manifest = _cdmCorpus.MakeObject<CdmManifestDefinition>(
                    CdmObjectType.ManifestDef, "IIoTOpcUaPubSub");
                Manifest.Name = "IIoTOpcUaPubSub";
                Manifest.ManifestName = "IIoT OPC UA Pub/Sub Manifest";
                adlsRoot.Documents.Add(Manifest, "IIoTOpcUaPubSub.manifest.cdm.json");
                Manifest.Imports.Add(kFoundationJsonPath);
                Manifest.Schema = "cdm:/schema.cdm.json";
                Manifest.JsonSchemaSemanticVersion = "1.0.0";
                if (Manifest != null) {
                    await Manifest.SaveAsAsync("model.json", true);
                }
            }
            Try.Op(() => _cacheUploadTimer.Change(_cacheUploadInterval, Timeout.InfiniteTimeSpan));
        }

        /// <summary>
        /// closes the cdm model repository
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync() {
            _logger.Information("Closing CDM Processor ...");
            Try.Op(() => _cacheUploadTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan));
            await PerformWriteCacheAsync();
            Manifest = null;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _adapter.Dispose();
            _cacheUploadTimer.Dispose();
            _lock.Dispose();
        }

        /// <inheritdoc/>
        private async Task PerformWriteCacheAsync() {
            var sw = Stopwatch.StartNew();
            var performSave = false;
            _logger.Information("Sending processed CDM data ...");
            try {
                if (_samplesCacheSize == 0) {
                    _logger.Information("End sending processed CDM data - empty buffer");
                    return;
                }
                if (Manifest == null) {
                    _logger.Warning("Manifest is not assigned yet. Retry ... ");
                    await OpenAsync();
                }
                foreach (var record in _samplesCache) {
                    if (record.Value.Count == 0 || record.Value[0] == null) {
                        _logger.Error("Samples list is empty ...");
                        continue;
                    }
                    performSave |= await WriteRecordToPartitionAsync(
                        record.Key, record.Value);
                }
                foreach (var record in _dataSetsCache) {
                    if (record.Value.Count == 0 || record.Value[0] == null) {
                        _logger.Error("DataSet list is empty ...");
                        continue;
                    }
                    performSave |= await WriteRecordToPartitionAsync(
                        record.Key, record.Value);
                }
                if (performSave) {
                    await Manifest.SaveAsAsync("model.json", true);
                }
                _logger.Information("Finished sending CDM data records - duration {elapsed}).",
                    sw.Elapsed);
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Failed to send processed CDM data after {elapsed}",
                     sw.Elapsed);
            }
            finally {
                foreach (var list in _samplesCache.Values) {
                    list!.Clear();
                }
                _samplesCache.Clear();
                foreach (var list in _dataSetsCache.Values) {
                    list!.Clear();
                }
                _dataSetsCache.Clear();
                _samplesCacheSize = 0;
            }
            sw.Stop();
        }

        /// <inheritdoc/>
        private async Task<bool> WriteRecordToPartitionAsync<T>(string partitionKey, IList<T> record) {
            var retry = false;
            var result = true;
            bool persist;
            var dataSetRecordList = record as List<DataSetMessageModel>;
            var samplesRecordList = record as List<MonitoredItemMessageModel>;
            do {
                var partition = (dataSetRecordList != null)
                    ? GetOrCreateEntityDataPartition(partitionKey, dataSetRecordList[0], out persist, retry)
                    : GetOrCreateEntityDataPartition(partitionKey, samplesRecordList[0], out persist, retry);
                if (partition == null) {
                    _logger.Error("Failed to create CDM Entity for {key} records).", partitionKey);
                    continue;
                }
                var csvTrait = partition.ExhibitsTraits.Item("is.partition.format.CSV");
                var partitionUrl = _cdmCorpus.Storage.CorpusPathToAdapterPath(partition.Location);
                var partitionDelimitor = csvTrait?.Arguments?.FetchValue("delimiter") ?? kCsvPartitionsDelimiter;
                result = (dataSetRecordList != null)
                    ? await _storage.WriteInCsvPartition<DataSetMessageModel>(
                        partitionUrl, dataSetRecordList, partitionDelimitor)
                    : await _storage.WriteInCsvPartition<MonitoredItemMessageModel>(
                        partitionUrl, samplesRecordList, partitionDelimitor);
                if (result == false && retry == false) {
                    retry = true;
                }
                else {
                    retry = false;
                }
            } while (retry);

            if (result) {
                _logger.Information("Successfully processed to CDM {count} records.", record.Count);
            }
            else {
                _logger.Warning("Failed to process CDM data for {record.Key} records.", partitionKey);
            }
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
                .Replace('"', '_').Replace('\'', '_');
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
        private CdmDataPartitionDefinition GetOrCreateEntityDataPartition(string key,
            MonitoredItemMessageModel sample, out bool persist, bool forceNew = false) {

            persist = false;
            if (string.IsNullOrEmpty(key) || sample == null) {
                return null;
            }

            // check if the enetity was aleready added
            var entityDefinition = Manifest.Entities.Item(key);
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
                // TODO: remove - apparently not necessary
                newSampleEntityDoc.Imports.Add(kFoundationJsonPath);
                newSampleEntityDoc.Definitions.Add(newSampleEntity);
                _cdmCorpus.Storage.FetchRootFolder("adls").Documents.Add(
                    newSampleEntityDoc, newSampleEntityDoc.Name);
                entityDefinition = Manifest.Entities.Add(newSampleEntity);
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
        private CdmDataPartitionDefinition GetOrCreateEntityDataPartition(string key,
            DataSetMessageModel dataSet, out bool persist, bool forceNew = false) {

            persist = false;
            if (string.IsNullOrEmpty(key) || dataSet == null) {
                return null;
            }

            // check if the enetity was aleready added
            var entityDefinition = Manifest.Entities.Item(key);
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
                _cdmCorpus.Storage.FetchRootFolder("adls").Documents.Add(
                    newEntityDoc, newEntityDoc.Name);
                entityDefinition = Manifest.Entities.Add(newDataSetEntity);
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
        /// <summary>
        /// Cache Timer Elapesed handler
        /// </summary>
        /// <param name="sender"></param>
        private async void CacheTimer_ElapesedAsync(object sender) {
            try {
                await _lock.WaitAsync();
                _cacheUploadTriggered = true;
                await PerformWriteCacheAsync();
            }
            finally {
                Try.Op(() => _cacheUploadTimer.Change(_cacheUploadInterval, Timeout.InfiniteTimeSpan));
                _cacheUploadTriggered = false;
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        private async Task ProcessCdmSampleAsync<T>(T payload) {
            try {
                await _lock.WaitAsync();
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
        /// Cdm Manifest handler
        /// </summary>
        private CdmManifestDefinition Manifest { get; set; }

        private readonly ADLSAdapter _adapter = null;
        private readonly IAdlsStorage _storage = null;
        private readonly CdmCorpusDefinition _cdmCorpus = null;
        private readonly ILogger _logger;
        private readonly ICdmClientConfig _config;

        private readonly SemaphoreSlim _lock;
        private readonly Timer _cacheUploadTimer;
        private readonly TimeSpan _cacheUploadInterval;
        private bool _cacheUploadTriggered;

        private int _samplesCacheSize;
        private readonly Dictionary<string, List<MonitoredItemMessageModel>> _samplesCache;
        private readonly Dictionary<string, List<DataSetMessageModel>> _dataSetsCache;

        private static readonly int kSamplesCacheMaxSize = 5000;
        private static readonly string kPublisherDataSetEntityName = "OpcUaPubSubDataSet";
        private static readonly string kPublisherSampleEntityName = "OpcUaPubSubSample";
        private static readonly string kFoundationJsonPath = "cdm:/foundations.cdm.json";
        private static readonly string kCsvPartitionsDelimiter = ",";
    }
}
