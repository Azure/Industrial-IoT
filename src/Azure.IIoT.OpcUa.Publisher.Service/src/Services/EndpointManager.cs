﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Exceptions;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Registers endpoints with the endpoint and application
    /// registry
    /// </summary>
    public sealed class EndpointManager : IEndpointManager
    {
        /// <summary>
        /// Create endpoint manager
        /// </summary>
        /// <param name="discovery"></param>
        /// <param name="registry"></param>
        public EndpointManager(IServerDiscovery discovery, IApplicationBulkProcessor registry)
        {
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <inheritdoc/>
        public async Task<string> RegisterEndpointAsync(ServerEndpointQueryModel query,
            CancellationToken ct = default)
        {
            var application = await _discovery.FindServerAsync(query, ct).ConfigureAwait(false);
            if (application == null)
            {
                throw new ResourceNotFoundException("Could not find any endpoint");
            }
            var registered = await _registry.AddDiscoveredApplicationAsync(application,
                ct).ConfigureAwait(false);
            if (registered.Endpoints == null || registered.Endpoints.Count == 0)
            {
                throw new ResourceNotFoundException("No endpoint registered.");
            }
            var id = registered.Endpoints[0].Id;
            if (id == null)
            {
                throw new ResourceInvalidStateException("Failed to register endpoint.");
            }
            return id;
        }

        private readonly IServerDiscovery _discovery;
        private readonly IApplicationBulkProcessor _registry;
    }
}
