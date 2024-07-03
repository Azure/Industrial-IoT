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
    using System.Collections.Generic;
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
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet("reset")]
        public async Task ResetAllClientsAsync(CancellationToken ct = default)
        {
            await _diagnostics.ResetAllClientsAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetConnectionDiagnostic
        /// </summary>
        /// <remarks>
        /// Get connection diagnostic information for all connections.
        /// The first set of diagnostics are the diagnostics active for
        /// all connections, continue reading to get updates.
        /// </remarks>
        /// <param name="ct"></param>
        /// <response code="200">The operation was successful.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet("diagnostics/connections")]
        public IAsyncEnumerable<ConnectionDiagnosticModel> GetConnectionDiagnosticAsync(
            CancellationToken ct = default)
        {
            return _diagnostics.GetConnectionDiagnosticAsync(ct);
        }

        private readonly IClientDiagnostics _diagnostics;
    }
}
