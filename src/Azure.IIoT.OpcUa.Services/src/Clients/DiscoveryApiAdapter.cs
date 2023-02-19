// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Services.Adapter {
    using Azure.IIoT.OpcUa.Api;
    using Azure.IIoT.OpcUa.Api.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements discovery services as adapter on top of discovery api.
    /// </summary>
    public sealed class DiscoveryApiAdapter : IDiscoveryServices,
        IServerDiscovery {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public DiscoveryApiAdapter(IDiscoveryApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request,
            CancellationToken ct = default) {
            await _client.RegisterAsync(request, ct);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct = default) {
            await _client.DiscoverAsync(request, ct);
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelModel request,
            CancellationToken ct = default) {
            await _client.CancelAsync(request, ct);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query, CancellationToken ct = default) {
            var result = await _client.FindServerAsync(query, ct);
            return result;
        }

        private readonly IDiscoveryApi _client;
    }
}
