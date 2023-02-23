// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controller {
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery method controller
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [ExceptionsFilter]
    public class DiscoveryMethodsController : IMethodController {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="discover"></param>
        /// <param name="servers"></param>
        public DiscoveryMethodsController(IDiscoveryServices discover,
            IServerDiscovery servers) {
            _discover = discover ?? throw new ArgumentNullException(nameof(discover));
            _servers = servers ?? throw new ArgumentNullException(nameof(servers));
        }

        /// <summary>
        /// Find server with endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            return await _servers.FindServerAsync(endpoint).ConfigureAwait(false);
        }

        /// <summary>
        /// Start server registration
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> RegisterAsync(ServerRegistrationRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _discover.RegisterAsync(request).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Discover application
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> DiscoverAsync(DiscoveryRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _discover.DiscoverAsync(request).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Cancel discovery
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> CancelAsync(DiscoveryCancelRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _discover.CancelAsync(request).ConfigureAwait(false);
            return true;
        }

        private readonly IDiscoveryServices _discover;
        private readonly IServerDiscovery _servers;
    }
}
