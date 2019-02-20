// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Onboarding.Runtime {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Hub.Processor.Runtime;
    using Microsoft.Azure.IIoT.Hub.Runtime;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Tasks.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Onboarding service configuration
    /// </summary>
    public class Config : ConfigBase, IEventProcessorConfig, IIoTHubConfig,
        ITaskProcessorConfig, IEventHubConfig {

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;
        /// <inheritdoc/>
        public string EventHubConnString => _eh.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _eh.EventHubPath;
        /// <inheritdoc/>
        public string ConsumerGroup => _eh.ConsumerGroup;
        /// <inheritdoc/>
        public bool UseWebsockets => _eh.UseWebsockets;
        /// <inheritdoc/>
        public int ReceiveBatchSize => _ep.ReceiveBatchSize;
        /// <inheritdoc/>
        public TimeSpan ReceiveTimeout => _ep.ReceiveTimeout;
        /// <inheritdoc/>
        public string BlobStorageConnString => _ep.BlobStorageConnString;
        /// <inheritdoc/>
        public string LeaseContainerName => _ep.LeaseContainerName;
        /// <inheritdoc/>
        public int MaxInstances => _tasks.MaxInstances;
        /// <inheritdoc/>
        public int MaxQueueSize => _tasks.MaxQueueSize;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="configuration"></param>
        public Config(string serviceId, IConfigurationRoot configuration) :
            base(configuration) {

            _tasks = new TaskProcessorConfig(configuration);
            _ep = new EventProcessorConfig(configuration, serviceId);
            _eh = new IoTHubEventConfig(configuration, serviceId);
            _hub = new IoTHubConfig(configuration, serviceId);
        }

        private readonly TaskProcessorConfig _tasks;
        private readonly EventProcessorConfig _ep;
        private readonly IoTHubEventConfig _eh;
        private readonly IoTHubConfig _hub;
    }
}
