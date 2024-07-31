// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Clients.Adapters
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements discovery services as adapter on top of discovery api.
    /// </summary>
    public sealed class DiscoveryApiAdapter : INetworkDiscovery<object>, IServerDiscovery<object>
    {
        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public DiscoveryApiAdapter(IDiscoveryApi client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request,
            object? context = null, CancellationToken ct = default)
        {
            await _client.RegisterAsync(request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request,
            object? context = null, CancellationToken ct = default)
        {
            await _client.DiscoverAsync(request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelRequestModel request,
            object? context = null, CancellationToken ct = default)
        {
            await _client.CancelAsync(request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query, object? context = null,
            CancellationToken ct = default)
        {
            return await _client.FindServerAsync(query, ct).ConfigureAwait(false);
        }

        private readonly IDiscoveryApi _client;
    }
}
