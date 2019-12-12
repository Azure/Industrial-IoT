// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Twin.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Utils;
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
        /// <param name="events"></param>
        public TwinServices(IClientHost client, IEventEmitter events) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc/>
        public async Task SetEndpointAsync(EndpointModel endpoint) {
            if (endpoint.IsSameAs(Endpoint)) {
                return;
            }

            // Unregister old endpoint
            if (Endpoint != null) {
                await _client.UnregisterAsync(Endpoint);
            }

            // Set new endpoint
            Endpoint = endpoint;

            // Register callback to report endpoint state property
            if (Endpoint != null) {
                await _client.RegisterAsync(Endpoint,
                    state => _events?.ReportAsync("State", state));
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (Endpoint != null) {
                Try.Op(() => SetEndpointAsync(null).Wait());
            }
        }

        private readonly IClientHost _client;
        private readonly IEventEmitter _events;
    }
}
