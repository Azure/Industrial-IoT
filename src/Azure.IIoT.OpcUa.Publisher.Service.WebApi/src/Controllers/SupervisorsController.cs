// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly.Extensions.AspNetCore.OpenApi;
    using Furly.Extensions.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Furly;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Read, Update and Query publisher resources
    /// </summary>
    [ApiVersion("2")]
    [Route("registry/v{version:apiVersion}/supervisors")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class SupervisorsController : ControllerBase
    {
        /// <summary>
        /// Create controller for supervisor services
        /// </summary>
        /// <param name="supervisors"></param>
        public SupervisorsController(ISupervisorRegistry supervisors)
        {
            _supervisors = supervisors;
        }

        /// <summary>
        /// Get supervisor registration information
        /// </summary>
        /// <remarks>
        /// Returns a supervisor's registration and connectivity information.
        /// A supervisor id corresponds to the twin modules module identity.
        /// </remarks>
        /// <param name="supervisorId">Supervisor identifier</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <param name="ct"></param>
        /// <returns>Supervisor registration</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("{supervisorId}")]
        public async Task<SupervisorModel> GetSupervisorAsync(string supervisorId,
            [FromQuery] bool? onlyServerState, CancellationToken ct)
        {
            return await _supervisors.GetSupervisorAsync(supervisorId,
                onlyServerState ?? false, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Update supervisor information
        /// </summary>
        /// <remarks>
        /// Allows a caller to configure recurring discovery runs on the twin module
        /// identified by the supervisor id or update site information.
        /// </remarks>
        /// <param name="supervisorId">supervisor identifier</param>
        /// <param name="request">Patch request</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPatch("{supervisorId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task UpdateSupervisorAsync(string supervisorId,
            [FromBody][Required] SupervisorUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _supervisors.UpdateSupervisorAsync(supervisorId, request,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get list of supervisors
        /// </summary>
        /// <remarks>
        /// Get all registered supervisors and therefore twin modules in paged form.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call this operation again using the token to retrieve more results.
        /// </remarks>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if available</param>
        /// <param name="continuationToken">Optional Continuation token</param>
        /// <param name="pageSize">Optional number of results to return</param>
        /// <param name="ct"></param>
        /// <returns>
        /// List of supervisors and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<SupervisorListModel> GetListOfSupervisorsAsync(
            [FromQuery] bool? onlyServerState,
            [FromQuery] string? continuationToken,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            if (Request.Headers.TryGetValue(HttpHeader.ContinuationToken, out var value))
            {
                continuationToken = value.FirstOrDefault();
            }
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _supervisors.ListSupervisorsAsync(continuationToken,
                onlyServerState ?? false, pageSize, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Query supervisors
        /// </summary>
        /// <remarks>
        /// Get all supervisors that match a specified query.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfSupervisors operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Supervisors query model</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <param name="ct"></param>
        /// <returns>Supervisors</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("query")]
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            [FromBody][Required] SupervisorQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }

            // TODO: Filter results based on RBAC

            return await _supervisors.QuerySupervisorsAsync(query,
                onlyServerState ?? false, pageSize, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get filtered list of supervisors
        /// </summary>
        /// <remarks>
        /// Get a list of supervisors filtered using the specified query parameters.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfSupervisors operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Supervisors Query model</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <param name="ct"></param>
        /// <returns>Supervisors</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("query")]
        public async Task<SupervisorListModel> GetFilteredListOfSupervisorsAsync(
            [FromQuery][Required] SupervisorQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }

            // TODO: Filter results based on RBAC

            return await _supervisors.QuerySupervisorsAsync(query,
                onlyServerState ?? false, pageSize, ct).ConfigureAwait(false);
        }

        private readonly ISupervisorRegistry _supervisors;
    }
}
