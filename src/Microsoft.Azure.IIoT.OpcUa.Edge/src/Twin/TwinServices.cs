// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages the endpoint identity information in the twin and reports
    /// the endpoint's status back to the hub.
    /// </summary>
    public class TwinServices : ITwinServices, IDisposable {

        /// <inheritdoc/>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// Create twin services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="services"></param>
        /// <param name="events"></param>
        /// <param name="logger"></param>
        public TwinServices(IClientHost client, IEndpointServices services,
            IEventEmitter events, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task SetEndpointAsync(EndpointModel endpoint) {
            if (endpoint.IsSameAs(Endpoint)) {
                return Task.CompletedTask;
            }

            // Unregister old endpoint
            if (Endpoint != null) {
                _client.Unregister(Endpoint);
            }

            // Set new endpoint
            Endpoint = endpoint;

            // Register callback to report endpoint state property
            if (Endpoint != null) {
                _client.Register(Endpoint,
                   state => _events?.SendAsync("State", state));
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (Endpoint != null) {
                _client.Unregister(Endpoint);
            }
        }

        private readonly IClientHost _client;
        private readonly IEndpointServices _services;
        private readonly IEventEmitter _events;
        private readonly ILogger _logger;
    }
}
