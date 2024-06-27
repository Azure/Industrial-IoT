// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>OPC UA and network discovery related API.</para>
    /// <para>
    /// The method name for all transports other than HTTP (which uses the shown
    /// HTTP methods and resource uris) is the name of the subsection header.
    /// To use the version specific method append "_V1" or "_V2" to the method
    /// </para>
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    [ControllerExceptionFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/discovery")]
    [ApiController]
    [Authorize]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class DiscoveryController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="discover"></param>
        /// <param name="servers"></param>
        public DiscoveryController(INetworkDiscovery discover,
            IServerDiscovery servers)
        {
            _discover = discover ?? throw new ArgumentNullException(nameof(discover));
            _servers = servers ?? throw new ArgumentNullException(nameof(servers));
        }

        /// <summary>
        /// FindServer
        /// </summary>
        /// <remarks>
        /// Find servers matching the specified endpoint query spec.
        /// </remarks>
        /// <param name="endpoint">The endpoint query specifying the
        /// matching criteria for the discovered endpoints.</param>
        /// <param name="ct"></param>
        /// <returns>The application information of a found server.
        /// Endpoints are only included in the response if they match
        /// the query specification. If no server is found the call
        /// returns 404 NotFound error.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="endpoint"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("findserver")]
        public async Task<ApplicationRegistrationModel> FindServerAsync(
            [FromBody][Required] ServerEndpointQueryModel endpoint, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            return await _servers.FindServerAsync(endpoint, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Register
        /// </summary>
        /// <remarks>
        /// Start server registration. The results of the registration
        /// are published as events to the default event transport.
        /// </remarks>
        /// <param name="request">Contains all information to perform
        /// the registration request including discovery url to use.</param>
        /// <param name="ct"></param>
        /// <returns>Returns true if the registration request was
        /// processed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("register")]
        public async Task<bool> RegisterAsync(
            [FromBody][Required] ServerRegistrationRequestModel request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _discover.RegisterAsync(request, ct).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Discover
        /// </summary>
        /// <remarks>
        /// Start network discovery using the provided discovery request
        /// configuration. The discovery results are published to the
        /// configured default event transport.
        /// </remarks>
        /// <param name="request">The discovery configuration to use
        /// during the discovery run.</param>
        /// <param name="ct"></param>
        /// <returns>Returns true if the discovery operation started.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<bool> DiscoverAsync(
            [FromBody][Required] DiscoveryRequestModel request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _discover.DiscoverAsync(request, ct).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Cancel
        /// </summary>
        /// <remarks>
        /// Cancel a discovery run that is ongoing using the discovery
        /// request token specified in the discover operation.
        /// </remarks>
        /// <param name="request">The information needed to cancel the
        /// discovery operation.</param>
        /// <param name="ct"></param>
        /// <returns>Returns true if the cancellation request was processed.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("cancel")]
        public async Task<bool> CancelAsync(
            [FromBody][Required] DiscoveryCancelRequestModel request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _discover.CancelAsync(request, ct).ConfigureAwait(false);
            return true;
        }

        private readonly INetworkDiscovery _discover;
        private readonly IServerDiscovery _servers;
    }
}
