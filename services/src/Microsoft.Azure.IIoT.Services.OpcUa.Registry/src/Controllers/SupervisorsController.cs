// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;
    using System;

    /// <summary>
    /// Read, Update and Query supervisor resources
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/supervisors")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanQuery)]
    [ApiController]
    public class SupervisorsController : ControllerBase {

        /// <summary>
        /// Create controller for supervisor services
        /// </summary>
        /// <param name="supervisors"></param>
        /// <param name="diagnostics"></param>
        public SupervisorsController(ISupervisorRegistry supervisors,
            ISupervisorDiagnostics diagnostics) {
            _supervisors = supervisors;
            _diagnostics = diagnostics;
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
        public async Task<SupervisorApiModel> GetSupervisorAsync(string supervisorId,
            [FromQuery] bool? onlyServerState) {
            var result = await _supervisors.GetSupervisorAsync(supervisorId,
                onlyServerState ?? false);
            return result.ToApiModel();
        }

        /// <summary>
        /// Get runtime status of supervisor
        /// </summary>
        /// <remarks>
        /// Allows a caller to get runtime status for a supervisor.
        /// </remarks>
        /// <param name="supervisorId">supervisor identifier</param>
        /// <returns>Supervisor status</returns>
        [HttpGet("{supervisorId}/status")]
        public async Task<SupervisorStatusApiModel> GetSupervisorStatusAsync(
            string supervisorId) {
            var result = await _diagnostics.GetSupervisorStatusAsync(supervisorId);
            return result.ToApiModel();
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
        [Authorize(Policy = Policies.CanChange)]
        public async Task UpdateSupervisorAsync(string supervisorId,
            [FromBody] [Required] SupervisorUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _supervisors.UpdateSupervisorAsync(supervisorId,
                request.ToServiceModel());
        }

        /// <summary>
        /// Reset supervisor
        /// </summary>
        /// <remarks>
        /// Allows a caller to reset the twin module using its supervisor
        /// identity identifier.
        /// </remarks>
        /// <param name="supervisorId">supervisor identifier</param>
        /// <returns></returns>
        [HttpPost("{supervisorId}/reset")]
        [Authorize(Policy = Policies.CanManage)]
        public Task ResetSupervisorAsync(string supervisorId) {
            return _diagnostics.ResetSupervisorAsync(supervisorId);
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
        public async Task<SupervisorListApiModel> GetListOfSupervisorsAsync(
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
            var result = await _supervisors.ListSupervisorsAsync(
                continuationToken, onlyServerState ?? false, pageSize);
            return result.ToApiModel();
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
        public async Task<SupervisorListApiModel> QuerySupervisorsAsync(
            [FromBody] [Required] SupervisorQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _supervisors.QuerySupervisorsAsync(
                query.ToServiceModel(), onlyServerState ?? false, pageSize);

            // TODO: Filter results based on RBAC

            return result.ToApiModel();
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
        public async Task<SupervisorListApiModel> GetFilteredListOfSupervisorsAsync(
            [FromQuery] [Required] SupervisorQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {

            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _supervisors.QuerySupervisorsAsync(
                query.ToServiceModel(), onlyServerState ?? false, pageSize);

            // TODO: Filter results based on RBAC

            return result.ToApiModel();
        }

        private readonly ISupervisorRegistry _supervisors;
        private readonly ISupervisorDiagnostics _diagnostics;
    }
}
