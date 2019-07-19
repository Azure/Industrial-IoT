// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Hub.Router.Runtime {
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Graph model upload agent configuration
    /// </summary>
    public class Config : ConfigBase, IIoTHubConfig, IEventHubConfig {

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;
        /// <inheritdoc/>
        public string EventHubConnString => _eh.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _eh.EventHubPath;
        /// <inheritdoc/>
        public bool UseWebsockets => _eh.UseWebsockets;
        /// <inheritdoc/>
        public string ConsumerGroup => _eh.ConsumerGroup;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) :
            base(configuration) {

            _eh = new EventHubConfig(configuration);
            _hub = new IoTHubConfig(configuration);
        }

        private readonly EventHubConfig _eh;
        private readonly IoTHubConfig _hub;
    }
}
