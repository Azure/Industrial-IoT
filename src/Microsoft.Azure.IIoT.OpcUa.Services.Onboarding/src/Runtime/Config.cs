// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Onboarding.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Hub.Processor.Runtime;
    using Microsoft.Azure.IIoT.Hub.Runtime;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Tasks.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Onboarding service configuration
    /// </summary>
    public class Config : LogConfig, IEventProcessorConfig, IIoTHubConfig,
        ITaskProcessorConfig {
        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;
        /// <inheritdoc/>
        public int ReceiveBatchSize => _ep.ReceiveBatchSize;
        /// <inheritdoc/>
        public TimeSpan ReceiveTimeout => _ep.ReceiveTimeout;
        /// <inheritdoc/>
        public string EventHubConnString => _ep.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _ep.EventHubPath;
        /// <inheritdoc/>
        public string ConsumerGroup => _ep.ConsumerGroup;
        /// <inheritdoc/>
        public string BlobStorageConnString => _ep.BlobStorageConnString;
        /// <inheritdoc/>
        public string LeaseContainerName => _ep.LeaseContainerName;
        /// <inheritdoc/>
        public bool UseWebsockets => _ep.UseWebsockets;
        /// <inheritdoc/>
        public int MaxInstances => _tasks.MaxInstances;
        /// <inheritdoc/>
        public int MaxQueueSize => _tasks.MaxQueueSize;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="serviceId"></param>
        /// <param name="configuration"></param>
        public Config(string processId, string serviceId,
            IConfigurationRoot configuration) :
            base(processId, configuration) {

            _tasks = new TaskProcessorConfig(configuration);
            _ep = new EventProcessorConfig(configuration, serviceId);
            _hub = new IoTHubConfig(configuration, serviceId);
        }

        private readonly TaskProcessorConfig _tasks;
        private readonly EventProcessorConfig _ep;
        private readonly IoTHubConfig _hub;
    }
}
