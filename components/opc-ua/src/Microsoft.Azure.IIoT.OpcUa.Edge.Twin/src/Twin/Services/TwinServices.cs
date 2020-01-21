// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Twin.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
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
        /// <param name="events"></param>
        public TwinServices(IEndpointServices client, IEventEmitter events) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc/>
        public Task SetEndpointAsync(EndpointModel endpoint) {
            if (!endpoint.IsSameAs(Endpoint)) {
                // Unregister old endpoint
                if (Endpoint != null) {
                    _callback?.Dispose();
                    _callback = null;
                    _session?.Dispose();
                    _session = null;
                }

                // Set new endpoint
                Endpoint = endpoint;

                // Register callback to report endpoint state property
                if (Endpoint != null) {
                    var connection = new ConnectionModel {
                        Endpoint = Endpoint
                    };
                    _session = _client.GetSessionHandle(connection);
                    _callback = _client.RegisterCallback(connection,
                        state => _events?.ReportAsync("State", state));
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _callback?.Dispose();
            _callback = null;
            _session?.Dispose();
            _session = null;
        }

        private ISessionHandle _session;
        private IDisposable _callback;
        private readonly IEndpointServices _client;
        private readonly IEventEmitter _events;
    }
}
