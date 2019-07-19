// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Processor.Runtime {
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Blob;
    using Microsoft.Azure.IIoT.Storage.Blob.Runtime;
    using Microsoft.Azure.IIoT.Storage.CosmosDb;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Tasks.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Graph model upload agent configuration
    /// </summary>
    public class Config : ConfigBase, IIoTHubConfig, ITaskProcessorConfig,
        ICosmosDbConfig, IStorageConfig, IItemContainerConfig {

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;
        /// <inheritdoc/>
        public int MaxInstances => _tasks.MaxInstances;
        /// <inheritdoc/>
        public int MaxQueueSize => _tasks.MaxQueueSize;
        /// <inheritdoc/>
        public string DbConnectionString => _db.DbConnectionString;
        /// <inheritdoc/>
        public string BlobStorageConnString => _storage.BlobStorageConnString;
        /// <inheritdoc/>
        public string ContainerName => null;
        /// <inheritdoc/>
        public string DatabaseName => null; // TODO

        /// <summary>
        /// Listen with file notification host instead of event
        /// processor for development.
        /// </summary>
        public bool UseFileNotificationHost =>
            GetBoolOrDefault("PCS_USE_FILE_NOTIFICATION", true);
        // TODO - set false

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) :
            base(configuration) {

            _tasks = new TaskProcessorConfig(configuration);
            _db = new CosmosDbConfig(configuration);
            _storage = new StorageConfig(configuration);
            _hub = new IoTHubConfig(configuration);
        }

        private readonly TaskProcessorConfig _tasks;
        private readonly CosmosDbConfig _db;
        private readonly StorageConfig _storage;
        private readonly IoTHubConfig _hub;
    }
}
