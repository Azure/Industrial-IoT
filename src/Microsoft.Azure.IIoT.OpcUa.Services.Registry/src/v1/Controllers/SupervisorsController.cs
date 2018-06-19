// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Supervisors controller
    /// </summary>
    [Route(VersionInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.Query)]
    public class SupervisorsController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="supervisors"></param>
        public SupervisorsController(IOpcUaSupervisorRegistry supervisors) {
            _supervisors = supervisors;
        }

        /// <summary>
        /// Returns the supervisor registration with the specified identifier.
        /// </summary>
        /// <param name="id">supervisor identifier</param>
        /// <returns>Supervisor registration</returns>
        [HttpGet("{id}")]
        public async Task<SupervisorApiModel> GetAsync(string id) {
            var result = await _supervisors.GetSupervisorAsync(id);
            return new SupervisorApiModel(result);
        }

        /// <summary>
        /// Update existing supervisor. Note that Id field in request
        /// must not be null and sueprvisor must exist.
        /// </summary>
        /// <param name="request">Patch request</param>
        [HttpPatch]
        [Authorize(Policy = Policy.Change)]
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
        /// <returns>
        /// List of supervisors and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<SupervisorListApiModel> ListAsync(
            [FromQuery] string continuationToken,
            [FromQuery] int? pageSize) {
            if (Request.Headers.ContainsKey(kContinuationTokenHeaderKey)) {
                continuationToken = Request.Headers[kContinuationTokenHeaderKey]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(kPageSizeHeaderKey)) {
                pageSize = int.Parse(Request.Headers[kPageSizeHeaderKey]
                    .FirstOrDefault());
            }
            var result = await _supervisors.ListSupervisorsAsync(
                continuationToken, pageSize);
            return new SupervisorListApiModel(result);
        }

        /// <summary>
        /// Returns the supervisors for the information in the
        /// specified supervisors query info model.
        /// </summary>
        /// <param name="query">Supervisors query</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Supervisors</returns>
        [HttpPost("query")]
        public async Task<SupervisorListApiModel> FindAsync(
            [FromBody] SupervisorQueryApiModel query,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(kPageSizeHeaderKey)) {
                pageSize = int.Parse(Request.Headers[kPageSizeHeaderKey]
                    .FirstOrDefault());
            }
            var result = await _supervisors.QuerySupervisorsAsync(
                query.ToServiceModel(), pageSize);

            // TODO: Filter results based on RBAC

            return new SupervisorListApiModel(result);
        }

        /// <summary>
        /// Query using Uri query specification.
        /// </summary>
        /// <param name="query">Supervisors Query</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Supervisors</returns>
        [HttpGet("query")]
        public async Task<SupervisorListApiModel> QueryAsync(
            [FromQuery] SupervisorQueryApiModel query,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(kPageSizeHeaderKey)) {
                pageSize = int.Parse(Request.Headers[kPageSizeHeaderKey]
                    .FirstOrDefault());
            }
            var result = await _supervisors.QuerySupervisorsAsync(
                query.ToServiceModel(), pageSize);

            // TODO: Filter results based on RBAC

            return new SupervisorListApiModel(result);
        }

        private const string kContinuationTokenHeaderKey = "x-ms-continuation";
        private const string kPageSizeHeaderKey = "x-ms-max-item-count";
        private readonly IOpcUaSupervisorRegistry _supervisors;
    }
}