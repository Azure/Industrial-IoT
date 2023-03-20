// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery methods controller
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    [ControllerExceptionFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/discovery")]
    [ApiController]
    public class DiscoveryMethodsController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="discover"></param>
        /// <param name="servers"></param>
        public DiscoveryMethodsController(INetworkDiscovery discover,
            IServerDiscovery servers)
        {
            _discover = discover ?? throw new ArgumentNullException(nameof(discover));
            _servers = servers ?? throw new ArgumentNullException(nameof(servers));
        }

        /// <summary>
        /// Find server with endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="endpoint"/>
        /// is <c>null</c>.</exception>
        [HttpPost("findserver")]
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel endpoint)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            return await _servers.FindServerAsync(endpoint).ConfigureAwait(false);
        }

        /// <summary>
        /// Start server registration
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("register")]
        public async Task<bool> RegisterAsync(ServerRegistrationRequestModel request)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _discover.RegisterAsync(request).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Discover application
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost]
        public async Task<bool> DiscoverAsync(DiscoveryRequestModel request)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _discover.DiscoverAsync(request).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Cancel discovery
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("cancel")]
        public async Task<bool> CancelAsync(DiscoveryCancelRequestModel request)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _discover.CancelAsync(request).ConfigureAwait(false);
            return true;
        }

        private readonly INetworkDiscovery _discover;
        private readonly IServerDiscovery _servers;
    }
}
