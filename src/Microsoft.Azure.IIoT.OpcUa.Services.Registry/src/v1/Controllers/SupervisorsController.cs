// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Supervisors controller
    /// </summary>
    [Route(VersionInfo.PATH + "/supervisors")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanQuery)]
    public class SupervisorsController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="supervisors"></param>
        public SupervisorsController(ISupervisorRegistry supervisors) {
            _supervisors = supervisors;
        }

        /// <summary>
        /// Returns the supervisor registration with the specified identifier.
        /// </summary>
        /// <param name="id">supervisor identifier</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <returns>Supervisor registration</returns>
        [HttpGet("{id}")]
        public async Task<SupervisorApiModel> GetAsync(string id,
            [FromQuery] bool? onlyServerState) {
            var result = await _supervisors.GetSupervisorAsync(id,
                onlyServerState ?? false);
            return new SupervisorApiModel(result);
        }

        /// <summary>
        /// Update existing supervisor. Note that Id field in request
        /// must not be null and sueprvisor must exist.
        /// </summary>
        /// <param name="request">Patch request</param>
        [HttpPatch]
        [Authorize(Policy = Policies.CanChange)]
        public async Task PatchAsync(
            [FromBody] SupervisorUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _supervisors.UpdateSupervisorAsync(request.ToServiceModel());
        }

        /// <summary>
        /// Get all registered supervisors in paged form.
        /// </summary>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <returns>
        /// List of supervisors and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<SupervisorListApiModel> ListAsync(
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
            return new SupervisorListApiModel(result);
        }

        /// <summary>
        /// Returns the supervisors for the information in the
        /// specified supervisors query info model.
        /// </summary>
        /// <param name="query">Supervisors query</param>
        /// <param name="pageSize">Optional number of results to
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// return</param>
        /// <returns>Supervisors</returns>
        [HttpPost("query")]
        public async Task<SupervisorListApiModel> FindAsync(
            [FromBody] SupervisorQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _supervisors.QuerySupervisorsAsync(
                query.ToServiceModel(), onlyServerState ?? false, pageSize);

            // TODO: Filter results based on RBAC

            return new SupervisorListApiModel(result);
        }

        /// <summary>
        /// Query using Uri query specification.
        /// </summary>
        /// <param name="query">Supervisors Query</param>
        /// <param name="pageSize">Optional number of results to
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// return</param>
        /// <returns>Supervisors</returns>
        [HttpGet("query")]
        public async Task<SupervisorListApiModel> QueryAsync(
            [FromQuery] SupervisorQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _supervisors.QuerySupervisorsAsync(
                query.ToServiceModel(), onlyServerState ?? false, pageSize);

            // TODO: Filter results based on RBAC

            return new SupervisorListApiModel(result);
        }

        private readonly ISupervisorRegistry _supervisors;
    }
}
