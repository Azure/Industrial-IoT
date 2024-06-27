// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Asp.Versioning;
    using Furly;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// This section lists the diagnostics APi provided by OPC Publisher providing
    /// connection related diagnostics API methods.
    /// </para>
    /// <para>
    /// The method name for all transports other than HTTP (which uses the shown
    /// HTTP methods and resource uris) is the name of the subsection header.
    /// To use the version specific method append "_V1" or "_V2" to the method
    /// name.
    /// </para>
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    [ControllerExceptionFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}")]
    [ApiController]
    [Authorize]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class DiagnosticsController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="diagnostics"></param>
        public DiagnosticsController(IClientDiagnostics diagnostics)
        {
            _diagnostics = diagnostics ??
                throw new ArgumentNullException(nameof(diagnostics));
        }

        /// <summary>
        /// ResetAllClients
        /// </summary>
        /// <remarks>
        /// Can be used to reset all established connections causing a full
        /// reconnect and recreate of all subscriptions.
        /// </remarks>
        /// <param name="ct"></param>
        /// <response code="200">The operation was successful.</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("reset")]
        public async Task ResetAllClientsAsync(CancellationToken ct = default)
        {
            await _diagnostics.ResetAllClients(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// SetTraceMode
        /// </summary>
        /// <remarks>
        /// Can be used to set trace mode for all established connections.
        /// Call within a minute to keep trace mode up or else trace mode
        /// will be disabled again after 1 minute. Enabling and resetting
        /// tracemode will cause a reconnect of the client.
        /// </remarks>
        /// <param name="ct"></param>
        /// <response code="200">The operation was successful.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("tracemode")]
        public async Task SetTraceModeAsync(CancellationToken ct = default)
        {
            await _diagnostics.SetTraceModeAsync(ct).ConfigureAwait(false);
        }

        private readonly IClientDiagnostics _diagnostics;
    }
}
