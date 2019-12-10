// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Cdm.Services {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Cdm;
    using Microsoft.Azure.IIoT.Cdm.Models;
    using Microsoft.CommonDataModel.ObjectModel.Cdm;
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.Storage;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using Serilog;

    /// <summary>
    /// Process messages by reading the stream and passing it to a handler.
    /// The processor can be registered with an event hub processor or
    /// with the IoT Hub blob upload host.  The latter is a simple setup,
    /// the former allows fan out of processing through event hub or
    /// service bus.  No matter what the processor simply processes the
    /// stream it is handed.
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
                        default:
                            _logger.Information("CDM message: {0}", msg);
                            break;
                    }
                }
            });

            _adapter = new ADLSAdapter($"{config.ADLSg2HostName}/{config.ADLSg2BlobName}", 
                $"/{config.RootFolder}", config.TenantId, config.AppId, config.AppSecret);
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
        public async Task ProcessAsync(SubscriberCdmSampleModel payload,
            IDictionary<string, string> properties = null, 
            string partitionKey = null) {
            await ProcessCdmSampleAsync(payload);
        }

        /// <summary>
        /// Open and load the cdm repository 
        /// </summary>
        /// <returns></returns>
        public async Task OpenAsync() {
            
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
                    _config.ADLSg2BlobName, _config.RootFolder);

                // create a new Manifest definition
                Manifest = _cdmCorpus.MakeObject<CdmManifestDefinition>(
                    CdmObjectType.ManifestDef, "IIoTOpcUaPubSub");
                Manifest.Name = "IIoTOpcUaPubSub";
                Manifest.ManifestName = "IIoT OPC UA Pub/Sub Manifest";
                adlsRoot.Documents.Add(Manifest, "IIoTOpcUaPubSub.manifest.cdm.json");

                var persist = AddPublisherSampleModelEntityToModel();

                for (int i = 0; i < _cdmCorpus.Documents.Count; i++) {
                    var item = _cdmCorpus.Documents[i];
                    var resOpt = new ResolveOptions() { 
                        WrtDoc = item,
                        Directives = kDirectives
                    };
                    await item.RefreshAsync(resOpt);
                }
                if (persist) {
                    // persist the file
                    await Manifest.SaveAsAsync("model.json", true);
                }
            }
        }

        /// <summary>
        /// closes the cdm model repository
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync() {
            if (Manifest != null) {
                await Manifest.SaveAsAsync("model.json", true);
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _adapter.Dispose();
        }

        private async Task ProcessCdmSampleAsync(SubscriberCdmSampleModel payload) {

            if (Manifest == null) {
                _logger.Warning("Manifest is not assigned yet. Retry ... ");
                await this.OpenAsync();
            }

            var entityDeclaration = Manifest.Entities.Item(kPublisherSampleEntityName);
            if (entityDeclaration == null) {
                // failed to load the cdm model
                _logger.Error("Failed to load the entity declaration");
                return;
            }

            var partition = entityDeclaration.DataPartitions[0];
            var csvTrait = partition.ExhibitsTraits.Item("is.partition.format.CSV");
            var partitionLocation = _cdmCorpus.Storage.CorpusPathToAdapterPath(partition.Location);

            await _storage.WriteInCsvPartition<SubscriberCdmSampleModel>(
                partitionLocation,
                new List<SubscriberCdmSampleModel>() { payload },
                csvTrait?.Arguments?.FetchValue("delimiter") ?? kCsvPartitionsDelimiter);
       }

        private bool AddPublisherSampleModelEntityToModel() {

            // check if the enetity was aleready added
            foreach (var entity in Manifest.Entities) {
                if (entity.EntityName == kPublisherSampleEntityName) {
                    return false;
                }
            }

            // add a new entity for the PublisherSample
            var publisherSampleEntity = _cdmCorpus.MakeObject<CdmEntityDefinition>(
                CdmObjectType.EntityDef, kPublisherSampleEntityName, false);

            //  add the attributes required
            var subscriptionId = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                CdmObjectType.TypeAttributeDef, "SubscriptionId", false);
            subscriptionId.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                CdmObjectType.PurposeRef, "hasA", true);
            subscriptionId.DataType = _cdmCorpus.MakeRef<CdmDataTypeReference>(
                CdmObjectType.DataTypeRef, "string", true);
            subscriptionId.DataFormat = CdmDataFormat.String;
            publisherSampleEntity.Attributes.Add(subscriptionId);

            var endpointId = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                CdmObjectType.TypeAttributeDef, "EndpointId", false);
            endpointId.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                CdmObjectType.PurposeRef, "hasA", true);
            endpointId.DataType = _cdmCorpus.MakeRef<CdmDataTypeReference>(
                CdmObjectType.DataTypeRef, "string", true);
            endpointId.DataFormat = CdmDataFormat.String;
            publisherSampleEntity.Attributes.Add(endpointId);

            var dataSetId = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                CdmObjectType.TypeAttributeDef, "DataSetId", false);
            dataSetId.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                CdmObjectType.PurposeRef, "hasA", true);
            dataSetId.DataType = _cdmCorpus.MakeRef<CdmDataTypeReference>(
                CdmObjectType.DataTypeRef, "string", true);
            dataSetId.DataFormat = CdmDataFormat.String;
            publisherSampleEntity.Attributes.Add(dataSetId);

            var nodeId = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                CdmObjectType.TypeAttributeDef, "NodeId", false);
            nodeId.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                CdmObjectType.PurposeRef, "hasA", true);
            nodeId.DataType = _cdmCorpus.MakeRef<CdmDataTypeReference>(
                CdmObjectType.DataTypeRef, "string", true);
            nodeId.DataFormat = CdmDataFormat.String;
            publisherSampleEntity.Attributes.Add(nodeId);

            var value = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                CdmObjectType.TypeAttributeDef, "Value", false);
            value.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                CdmObjectType.PurposeRef, "hasA", true);
            value.DataType = _cdmCorpus.MakeRef<CdmDataTypeReference>(
                CdmObjectType.DataTypeRef, "string", true);
            value.DataFormat = CdmDataFormat.String;
            publisherSampleEntity.Attributes.Add(value);

            var timestamp = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                CdmObjectType.TypeAttributeDef, "Timestamp", false);
            timestamp.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                CdmObjectType.PurposeRef, "hasA", true);
            timestamp.DataType = _cdmCorpus.MakeRef<CdmDataTypeReference>(
                CdmObjectType.DataTypeRef, "dateTime", true);
            timestamp.DataFormat = CdmDataFormat.DateTime;
            publisherSampleEntity.Attributes.Add(timestamp);

            var sourceTimestamp = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                CdmObjectType.TypeAttributeDef, "SourceTimestamp", false);
            sourceTimestamp.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                CdmObjectType.PurposeRef, "hasA", true);
            sourceTimestamp.DataType = _cdmCorpus.MakeRef<CdmDataTypeReference>(
                CdmObjectType.DataTypeRef, "dateTime", true);
            sourceTimestamp.DataFormat = CdmDataFormat.DateTime;
            publisherSampleEntity.Attributes.Add(sourceTimestamp);

            var serverTimestamp = _cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(
                CdmObjectType.TypeAttributeDef, "ServerTimestamp", false);
            serverTimestamp.Purpose = _cdmCorpus.MakeRef<CdmPurposeReference>(
                CdmObjectType.PurposeRef, "hasA", true);
            serverTimestamp.DataType = _cdmCorpus.MakeRef<CdmDataTypeReference>(
                CdmObjectType.DataTypeRef, "dateTime", true);
            serverTimestamp.DataFormat = CdmDataFormat.DateTime;
            publisherSampleEntity.Attributes.Add(serverTimestamp);

            publisherSampleEntity.DisplayName = kPublisherSampleEntityName;
            publisherSampleEntity.Version = "0.0.1";
            publisherSampleEntity.Description = "Publisher Sample Model";

            // Create a new document where the new entity's definition will be stored
            var publisherSampleEntityDoc = _cdmCorpus.MakeObject<CdmDocumentDefinition>(
                CdmObjectType.DocumentDef, $"{kPublisherSampleEntityName}.cdm.json", false);
            publisherSampleEntityDoc.Imports.Add($"{kPublisherSampleEntityName}.cdm.json");
            publisherSampleEntityDoc.Imports.Add("cdm:/foundations.cdm.json");
            publisherSampleEntityDoc.Definitions.Add(publisherSampleEntity);
            _cdmCorpus.Storage.FetchRootFolder("adls").Documents.Add(
                publisherSampleEntityDoc, publisherSampleEntityDoc.Name);
            var publisherSampleEntityDef = Manifest.Entities.Add(publisherSampleEntity);

            // Define a partition and add it to the local declaration
            var publisherSampleEntityPartition = _cdmCorpus.MakeObject<CdmDataPartitionDefinition>(
                CdmObjectType.DataPartitionDef, $"{kPublisherSampleEntityName}-data");
            publisherSampleEntityDef.DataPartitions.Add(publisherSampleEntityPartition);
            publisherSampleEntityPartition.Location = 
                $"adls:/{publisherSampleEntity.EntityName}/partition-data.csv";
            publisherSampleEntityPartition.Explanation = "OpcUaPublisher sample messages storage";
            var csvTrait = publisherSampleEntityPartition.ExhibitsTraits.Add(
                "is.partition.format.CSV");
            csvTrait.Arguments.Add("columnHeaders", "true");
            csvTrait.Arguments.Add("delimiter", kCsvPartitionsDelimiter);

            return true;
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

        private static readonly string kPublisherSampleEntityName = "PublisherSampleModel";
        private static readonly string kCsvPartitionsDelimiter = ",";
        private static readonly AttributeResolutionDirectiveSet kDirectives =
            new AttributeResolutionDirectiveSet(new HashSet<string>() {
                "normalized", "referenceOnly" });
    }
}
