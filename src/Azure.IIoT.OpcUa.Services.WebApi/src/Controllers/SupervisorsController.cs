// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers {
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Read, Update and Query publisher resources
    /// </summary>
    [ApiVersion("2")]
    [Route("registry/v{version:apiVersion}/supervisors")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class SupervisorsController : ControllerBase {
        /// <summary>
        /// Create controller for supervisor services
        /// </summary>
        /// <param name="supervisors"></param>
        public SupervisorsController(ISupervisorRegistry supervisors) {
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
        /// <returns>Supervisor registration</returns>
        [HttpGet("{supervisorId}")]
        public async Task<SupervisorModel> GetSupervisorAsync(string supervisorId,
            [FromQuery] bool? onlyServerState) {
            return await _supervisors.GetSupervisorAsync(supervisorId,
                onlyServerState ?? false).ConfigureAwait(false);
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
        /// <returns></returns>
        [HttpPatch("{supervisorId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task UpdateSupervisorAsync(string supervisorId,
            [FromBody][Required] SupervisorUpdateModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _supervisors.UpdateSupervisorAsync(supervisorId,
                request).ConfigureAwait(false);
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
        /// <returns>
        /// List of supervisors and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<SupervisorListModel> GetListOfSupervisorsAsync(
            [FromQuery] bool? onlyServerState,
            [FromQuery] string continuationToken,
            [FromQuery] int? pageSize) {
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            return await _supervisors.ListSupervisorsAsync(
                continuationToken, onlyServerState ?? false, pageSize).ConfigureAwait(false);
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
        /// <returns>Supervisors</returns>
        [HttpPost("query")]
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            [FromBody][Required] SupervisorQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            

            // TODO: Filter results based on RBAC

            return await _supervisors.QuerySupervisorsAsync(
                query, onlyServerState ?? false, pageSize).ConfigureAwait(false);
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
        /// <returns>Supervisors</returns>
        [HttpGet("query")]
        public async Task<SupervisorListModel> GetFilteredListOfSupervisorsAsync(
            [FromQuery][Required] SupervisorQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            

            // TODO: Filter results based on RBAC

            return await _supervisors.QuerySupervisorsAsync(
                query, onlyServerState ?? false, pageSize).ConfigureAwait(false);
        }

        private readonly ISupervisorRegistry _supervisors;
    }
}
