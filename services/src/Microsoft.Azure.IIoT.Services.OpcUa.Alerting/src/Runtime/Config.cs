// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Alerting.Runtime {
    using Microsoft.Azure.IIoT.Messaging.ServiceBus;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Runtime;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Alerting agent configuration
    /// </summary>
    public class Config : ConfigBase, IIoTHubConfig, IServiceBusConfig {

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;

        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;

        /// <inheritdoc/>
        public string ServiceBusConnString => _sb.ServiceBusConnString;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) :
            base(configuration) {

            _sb = new ServiceBusConfig(configuration);
            _hub = new IoTHubConfig(configuration);
        }

        private readonly IServiceBusConfig _sb;
        private readonly IIoTHubConfig _hub;
    }
}
