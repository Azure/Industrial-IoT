// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cdm.Services {
    using Microsoft.Azure.IIoT.Cdm;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.CommonDataModel.ObjectModel.Cdm;
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.Storage;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
            _samplesCache = new Dictionary<Tuple<string, string>, List<MonitoredItemSampleModel>>();
            _dataSetsCache = new Dictionary<Tuple<string, string, string>, List<DataSetMessageModel>>();

            _cdmCorpus = new CdmCorpusDefinition();
            _cdmCorpus.SetEventCallback(new EventCallback {
                Invoke = (level, msg) => {
                    switch (level) {
                        case CdmStatusLevel.Error:
                            _logger.Error("CDM message: {0}", msg);
                            break;
                        case CdmStatusLevel.Warning:
                            _logger.Warning("CDM message: {0}", msg);
                            break;
                        case CdmStatusLevel.Progress:
                            _logger.Verbose("CDM message: {0}", msg);
                            break;
                        case CdmStatusLevel.Info:
                            _logger.Debug("CDM message: {0}", msg);
                            break;
                    }
                }
            });

            _adapter = new ADLSAdapter($"{config.ADLSg2HostName}",
                $"/{config.ADLSg2ContainerName}/{config.RootFolder}", config.TenantId, config.AppId, config.AppSecret);
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
            _logger.Information($"Closing CDM Processor ...");
            Try.Op(() => _cacheUploadTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan));
            await PerformWriteCache();
            Manifest = null;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _adapter.Dispose();
            _cacheUploadTimer.Dispose();
            _lock.Dispose();
        }

        /// <summary>
        /// PerformWriteCache
        /// </summary>
        private async Task PerformWriteCache() {
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

                foreach (var samplesList in _samplesCache.Values) {
                    if (samplesList.Count == 0 || samplesList[0] == null) {
                        _logger.Error("Samples list is empty ...");
                        continue;
                    }
                    if (!GetEntityData(samplesList[0], out var partitionLocation, out var partitionDelimitor)) {
                        if(!CreateEntityData(samplesList[0], out partitionLocation, out partitionDelimitor)) {
                            _logger.Error("Failed to create CDM Entity for {endpointId}/{nodeId}).",
                                samplesList[0]?.EndpointId, samplesList[0]?.NodeId);
                            continue;
                        }
                        performSave = true;
                    }
                    await _storage.WriteInCsvPartition<MonitoredItemSampleModel>(
                        partitionLocation, samplesList, partitionDelimitor);
                }

                foreach(var dataSetsList in _dataSetsCache.Values) {
                    if (dataSetsList.Count == 0 || dataSetsList[0] == null) {
                        _logger.Error("DataSet list is empty ...");
                        continue;
                    }
                    if (!GetEntityData(dataSetsList[0], out var partitionLocation, out var partitionDelimitor)) {
                        if (!CreateEntityData(dataSetsList[0], out partitionLocation, out partitionDelimitor)) {
                            _logger.Error("Failed to create CDM Entity for {PublilsherId}/{DataSetWriterId}/{MetaDataVersion}).",
                                dataSetsList[0].PublisherId, dataSetsList[0].DataSetWriterId, dataSetsList[0].MetaDataVersion);
                            continue;
                        }
                        performSave = true;
                    }
                    await _storage.WriteInCsvPartition<DataSetMessageModel>(
                        partitionLocation, dataSetsList, partitionDelimitor);
                }

                if (performSave) {
                    if (Manifest != null) {
                        await Manifest.SaveAsAsync("model.json", true);
                    }
                }
                _logger.Information("Successfully sent processed CDM data {count} records (took {elapsed}).",
                    _samplesCacheSize, sw.Elapsed);
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
                _dataSetsCache.Clear();
                _samplesCacheSize = 0;
            }
            sw.Stop();
        }

        private string GetNormalizedEntityName(string publisherId,
            string dataSetWriterId, string metadataVersion) {
            var key = string.Join("", publisherId.Split(Path.GetInvalidFileNameChars())) + '_' +
                      string.Join("", dataSetWriterId.Split(Path.GetInvalidFileNameChars()));
            if (!string.IsNullOrEmpty(metadataVersion)) {
                key += '_' + string.Join("", metadataVersion.Split(Path.GetInvalidFileNameChars()));
            }
            return key.Replace('#', '_').Replace('.', '_').Replace('/','_').Replace(':', '_').Replace('"', '_').Replace('\'', '_');
        }

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

        private bool GetEntityData(MonitoredItemSampleModel sample,
            out string partitionLocation, out string partitionDelimitor){
            partitionLocation = null;
            partitionDelimitor = kCsvPartitionsDelimiter;
            var key = GetNormalizedEntityName(sample.EndpointId, sample.NodeId, null);
            var entityDeclaration = Manifest.Entities.Item(key);
            if (entityDeclaration == null) {
                return false;
            }
            var partition = entityDeclaration?.DataPartitions[0];
            if (partition == null) {
                return false;
            }
            var csvTrait = partition.ExhibitsTraits.Item("is.partition.format.CSV");
            partitionLocation = _cdmCorpus.Storage.CorpusPathToAdapterPath(partition.Location);
            partitionDelimitor = csvTrait?.Arguments?.FetchValue("delimiter") ?? kCsvPartitionsDelimiter;
            return true;
        }

        private bool CreateEntityData(MonitoredItemSampleModel sample,
            out string partitionLocation, out string partitionDelimitor) {
            string key = GetNormalizedEntityName(sample.EndpointId, sample.NodeId, null);

            // check if the enetity was aleready added
            var entity = Manifest.Entities.Item(key);
            if (entity != null) {
                return GetEntityData(sample, out partitionLocation, out partitionDelimitor);
            }

            // add a new entity for the Message
            var newSampleEntity = _cdmCorpus.MakeObject<CdmEntityDefinition>(
                CdmObjectType.EntityDef, key, false);

            var info = typeof(MonitoredItemSampleModel).GetProperties();
            foreach (var property in info) {
                // add the attributes required
                var attribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                    CdmObjectType.TypeAttributeDef, property.Name, false);
                attribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                    CdmObjectType.PurposeRef, "hasA", true);
                //  if we handle a value, lookup it's type property
                if (property.Name == "Value" &&
                    typeof(MonitoredItemSampleModel).
                        GetProperty("TypeId")?.GetValue(sample) is Type typeId){
                    attribute.DataFormat = DataTypeToCdmDataFormat(typeId);
                }
                else {
                    attribute.DataFormat = DataTypeToCdmDataFormat(property.PropertyType);
                }
                newSampleEntity.Attributes.Add(attribute);
            }

            newSampleEntity.DisplayName = kPublisherSampleEntityName;
            newSampleEntity.Version = "0.0.1";
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
            var newSampleEntityDef = Manifest.Entities.Add(newSampleEntity);

            // Define a partition and add it to the local declaration
            var newSampleEntityPartition = _cdmCorpus.MakeObject<CdmDataPartitionDefinition>(
                CdmObjectType.DataPartitionDef, newSampleEntity.EntityName);
            newSampleEntityDef.DataPartitions.Add(newSampleEntityPartition);
            newSampleEntityPartition.Location =
                $"adls:/{newSampleEntity.EntityName}/partition-data.csv";
            newSampleEntityPartition.Explanation = "Opc Ua monitored item sample messages storage";
            var csvTrait = newSampleEntityPartition.ExhibitsTraits.Add(
                "is.partition.format.CSV");
            csvTrait.Arguments.Add("columnHeaders", "true");
            csvTrait.Arguments.Add("delimiter", kCsvPartitionsDelimiter);

            partitionLocation = _cdmCorpus.Storage.CorpusPathToAdapterPath(newSampleEntityPartition.Location);
            partitionDelimitor = csvTrait?.Arguments?.FetchValue("delimiter") ?? kCsvPartitionsDelimiter;
            return true;
        }

        private bool GetEntityData(DataSetMessageModel dataSet,
            out string partitionLocation, out string partitionDelimitor) {
            partitionLocation = null;
            partitionDelimitor = kCsvPartitionsDelimiter;
            var key = GetNormalizedEntityName(dataSet.PublisherId,
                dataSet.DataSetWriterId, dataSet.MetaDataVersion);
            var entityDeclaration = Manifest.Entities.Item(key);
            if (entityDeclaration == null) {
                return false;
            }
            var partition = entityDeclaration?.DataPartitions[0];
            if (partition == null) {
                return false;
            }
            var csvTrait = partition.ExhibitsTraits.Item("is.partition.format.CSV");
            partitionLocation = _cdmCorpus.Storage.CorpusPathToAdapterPath(partition.Location);
            partitionDelimitor = csvTrait?.Arguments?.FetchValue("delimiter") ?? kCsvPartitionsDelimiter;
            return true;
        }

        private bool CreateEntityData(DataSetMessageModel dataSet,
            out string partitionLocation, out string partitionDelimitor) {
            var key = GetNormalizedEntityName(dataSet.PublisherId,
                dataSet.DataSetWriterId, dataSet.MetaDataVersion);
            // check if the enetity was aleready added
            var entity = Manifest.Entities.Item(key);
            if (entity != null) {
                return GetEntityData(dataSet, out partitionLocation, out partitionDelimitor);
            }

            // add a new entity for the Message
            var newSampleEntity = _cdmCorpus.MakeObject<CdmEntityDefinition>(
                CdmObjectType.EntityDef, key, false);

            var info = typeof(DataSetMessageModel).GetProperties();
            foreach (var property in info) {
                if (property.Name != nameof(DataSetMessageModel.Payload)) {
                    // add the attributes required
                    var attribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                    CdmObjectType.TypeAttributeDef, property.Name, false);
                    attribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                        CdmObjectType.PurposeRef, "hasA", true);
                    //  if we handle a value, lookup it's type property
                    if (property.Name == "Value" &&
                        typeof(DataSetMessageModel).
                            GetProperty("TypeId")?.GetValue(dataSet) is Type typeId) {
                        attribute.DataFormat = DataTypeToCdmDataFormat(typeId);
                    }
                    else {
                        attribute.DataFormat = DataTypeToCdmDataFormat(property.PropertyType);
                    }
                    newSampleEntity.Attributes.Add(attribute);
                }
                else{
                    //  Parse the message payload
                    foreach (var node in dataSet.Payload.OrderBy(i => i.Key)) {
                        // add the attributes for value, status and timestamp
                        var valueAttribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                            CdmObjectType.TypeAttributeDef, $"{node.Key}_value", false);
                        valueAttribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                            CdmObjectType.PurposeRef, "hasA", true);
                        valueAttribute.DataFormat = DataTypeToCdmDataFormat(node.Value.TypeId);
                        newSampleEntity.Attributes.Add(valueAttribute);

                        var typeIdAttribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                            CdmObjectType.TypeAttributeDef, $"{node.Key}_typeId", false);
                        typeIdAttribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                            CdmObjectType.PurposeRef, "hasA", true);
                        typeIdAttribute.DataFormat = DataTypeToCdmDataFormat(typeof(string));
                        newSampleEntity.Attributes.Add(typeIdAttribute);

                        var statusAttribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                            CdmObjectType.TypeAttributeDef, $"{node.Key}_status", false);
                        statusAttribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                            CdmObjectType.PurposeRef, "hasA", true);
                        statusAttribute.DataFormat = DataTypeToCdmDataFormat(typeof(string));
                        newSampleEntity.Attributes.Add(statusAttribute);

                        var timestampAttribute = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                            CdmObjectType.TypeAttributeDef, $"{node.Key}_timestamp", false);
                        timestampAttribute.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                            CdmObjectType.PurposeRef, "hasA", true);
                        timestampAttribute.DataFormat = DataTypeToCdmDataFormat(typeof(DateTime));
                        newSampleEntity.Attributes.Add(timestampAttribute);
                    }
                }
            }

            newSampleEntity.DisplayName = kPublisherDataSetEntityName;
            newSampleEntity.Version = "0.0.1";
            newSampleEntity.Description = "Opc Ua Pub/Sub Data Set";

            // Create a new document where the new entity's definition will be stored
            var newSampleEntityDoc = _cdmCorpus.MakeObject<CdmDocumentDefinition>(
                CdmObjectType.DocumentDef, $"{newSampleEntity.EntityName}.cdm.json", false);
            newSampleEntityDoc.Imports.Add($"{newSampleEntity.EntityName}.cdm.json");
            newSampleEntityDoc.Imports.Add(kFoundationJsonPath);
            newSampleEntityDoc.Definitions.Add(newSampleEntity);
            _cdmCorpus.Storage.FetchRootFolder("adls").Documents.Add(
                newSampleEntityDoc, newSampleEntityDoc.Name);
            var newSampleEntityDef = Manifest.Entities.Add(newSampleEntity);

            // Define a partition and add it to the local declaration
            var newSampleEntityPartition = _cdmCorpus.MakeObject<CdmDataPartitionDefinition>(
                CdmObjectType.DataPartitionDef, newSampleEntity.EntityName);
            newSampleEntityDef.DataPartitions.Add(newSampleEntityPartition);
            newSampleEntityPartition.Location =
                $"adls:/{newSampleEntity.EntityName}/partition-data.csv";
            newSampleEntityPartition.Explanation = "Opc Ua monitored item sample messages storage";
            var csvTrait = newSampleEntityPartition.ExhibitsTraits.Add(
                "is.partition.format.CSV");
            csvTrait.Arguments.Add("columnHeaders", "true");
            csvTrait.Arguments.Add("delimiter", kCsvPartitionsDelimiter);

            partitionLocation = _cdmCorpus.Storage.CorpusPathToAdapterPath(newSampleEntityPartition.Location);
            partitionDelimitor = csvTrait?.Arguments?.FetchValue("delimiter") ?? kCsvPartitionsDelimiter;
            return true;
        }

        /// <summary>
        /// Cache Timer Elapesed handler
        /// </summary>
        /// <param name="sender"></param>
        private async void CacheTimer_ElapesedAsync(object sender) {
            try {
                await _lock.WaitAsync();
                _cacheUploadTriggered = true;
                await PerformWriteCache();
            }
            finally {
                Try.Op(() => _cacheUploadTimer.Change(_cacheUploadInterval, Timeout.InfiniteTimeSpan));
                _cacheUploadTriggered = false;
                _lock.Release();
            }
        }

        private async Task ProcessCdmSampleAsync<T>(T payload) {
            try {
                await _lock.WaitAsync();
                if (payload is MonitoredItemSampleModel sample) {
                    var key = new Tuple<string, string>(sample.EndpointId, sample.NodeId);
                    if (!_samplesCache.TryGetValue(key, out var samplesList)) {
                        _samplesCache[key] = new List<MonitoredItemSampleModel>();
                    }
                    _samplesCache[key].Add(sample);
                }
                else if (payload is DataSetMessageModel dataSet) {
                    var key = new Tuple<string, string, string>(
                        dataSet.PublisherId, dataSet.DataSetWriterId, dataSet.MetaDataVersion);
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
        private readonly Dictionary<Tuple<string,string>, List<MonitoredItemSampleModel>> _samplesCache;
        private readonly Dictionary<Tuple<string, string, string>, List<DataSetMessageModel>> _dataSetsCache;

        private static readonly int kSamplesCacheMaxSize = 5000;
        private static readonly string kPublisherDataSetEntityName = "OpcUaPublisherDataSet";
        private static readonly string kPublisherSampleEntityName = "OpcUaPublisherSample";
        private static readonly string kFoundationJsonPath = "cdm:/foundations.cdm.json";
        private static readonly string kCsvPartitionsDelimiter = ",";
    }
}
