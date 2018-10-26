// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Gateway.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Runtime;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Tasks.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Gateway service configuration
    /// </summary>
    public class Config : LogConfig, IIoTHubConfig,
        ITaskProcessorConfig {
        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;
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
            _hub = new IoTHubConfig(configuration, serviceId);
        }

        private readonly TaskProcessorConfig _tasks;
        private readonly IoTHubConfig _hub;
    }
}
