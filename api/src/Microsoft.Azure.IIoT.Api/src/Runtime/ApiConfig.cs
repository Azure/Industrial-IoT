// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.History;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Events.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Events;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Complete api configuration
    /// </summary>
    public class ApiConfig : DiagnosticsConfig, ITwinConfig, IRegistryConfig,
        IHistoryConfig, IPublisherConfig, IEventsConfig, ISignalRClientConfig {

        /// <inheritdoc/>
        public string OpcUaTwinServiceUrl => _twin.OpcUaTwinServiceUrl;

        /// <inheritdoc/>
        public string OpcUaRegistryServiceUrl => _registry.OpcUaRegistryServiceUrl;

        /// <inheritdoc/>
        public string OpcUaHistoryServiceUrl => _history.OpcUaHistoryServiceUrl;

        /// <inheritdoc/>
        public string OpcUaPublisherServiceUrl => _publisher.OpcUaPublisherServiceUrl;

        /// <inheritdoc/>
        public string OpcUaEventsServiceUrl => _events.OpcUaEventsServiceUrl;

        /// <inheritdoc/>
        public bool UseMessagePackProtocol => _events.UseMessagePackProtocol;

        /// <inheritdoc/>
        public ApiConfig(IConfiguration configuration) :
            base(configuration) {
            _twin = new TwinConfig(configuration);
            _registry = new RegistryConfig(configuration);
            _history = new HistoryConfig(configuration);
            _publisher = new PublisherConfig(configuration);
            _events = new EventsConfig(configuration);
        }

        private readonly TwinConfig _twin;
        private readonly RegistryConfig _registry;
        private readonly HistoryConfig _history;
        private readonly PublisherConfig _publisher;
        private readonly EventsConfig _events;
    }
}
