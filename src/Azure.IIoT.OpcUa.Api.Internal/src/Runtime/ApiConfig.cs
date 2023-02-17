// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Runtime {
    using Azure.IIoT.OpcUa.Api.Events.Runtime;
    using Azure.IIoT.OpcUa.Api.Publisher.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Complete api configuration
    /// </summary>
    public class ApiConfig : DiagnosticsConfig, IServiceApiConfig,
        IEventsConfig, ISignalRClientConfig {

        /// <inheritdoc/>
        public string ServiceUrl => _publisher.ServiceUrl;

        /// <inheritdoc/>
        public string OpcUaEventsServiceUrl => _events.OpcUaEventsServiceUrl;

        /// <inheritdoc/>
        public bool UseMessagePackProtocol => _events.UseMessagePackProtocol;

        /// <inheritdoc/>
        public ApiConfig(IConfiguration configuration) :
            base(configuration) {
            _publisher = new PublisherConfig(configuration);
            _events = new EventsConfig(configuration);
        }

        private readonly PublisherConfig _publisher;
        private readonly EventsConfig _events;
    }
}
