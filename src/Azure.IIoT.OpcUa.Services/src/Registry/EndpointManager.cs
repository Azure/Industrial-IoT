// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Registry {
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Registers endpoints with the endpoint and application
    /// registry
    /// </summary>
    public sealed class EndpointManager : IEndpointManager {

        /// <summary>
        /// Create endpoint manager
        /// </summary>
        /// <param name="discovery"></param>
        /// <param name="registry"></param>
        public EndpointManager(IServerDiscovery discovery, IApplicationBulkProcessor registry) {
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary/>
        public async Task<string> RegisterEndpointAsync(ServerEndpointQueryModel query,
            CancellationToken ct = default) {
            var application = await _discovery.FindServerAsync(query, ct);
            if (application == null) {
                throw new ResourceNotFoundException("Could not find any endpoint");
            }
            var registered = await _registry.AddDiscoveredApplicationAsync(application, ct);
            return registered.Endpoints.Single().Id;
        }

        private readonly IServerDiscovery _discovery;
        private readonly IApplicationBulkProcessor _registry;
    }
}
