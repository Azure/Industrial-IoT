// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Events.Runtime {
    using Microsoft.Azure.IIoT.Messaging.ServiceBus;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Runtime;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Eventing agent configuration
    /// </summary>
    public class Config : DiagnosticsConfig, ISignalRServiceConfig,
        IServiceBusConfig {

        /// <inheritdoc/>
        public string ServiceBusConnString => _sb.ServiceBusConnString;
        /// <inheritdoc/>
        public string SignalRHubName => _sr.SignalRHubName;
        /// <inheritdoc/>
        public string SignalRConnString => _sr.SignalRConnString;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _sb = new ServiceBusConfig(configuration);
            _sr = new SignalRServiceConfig(configuration);
        }

        private readonly IServiceBusConfig _sb;
        private readonly SignalRServiceConfig _sr;
    }
}
