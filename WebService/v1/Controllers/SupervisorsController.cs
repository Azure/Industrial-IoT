// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Controllers {
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Auth;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Filters;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Supervisors controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.RegisterTwins)]
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
        /// <returns>
        /// List of supervisors and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<SupervisorListApiModel> ListAsync() {
            string continuationToken = null;
            if (Request.Headers.ContainsKey(CONTINUATION_TOKEN_NAME)) {
                continuationToken = Request.Headers[CONTINUATION_TOKEN_NAME].FirstOrDefault();
            }
            var result = await _supervisors.ListSupervisorsAsync(continuationToken);
            return new SupervisorListApiModel(result);
        }

        private const string CONTINUATION_TOKEN_NAME = "x-ms-continuation";
        private readonly IOpcUaSupervisorRegistry _supervisors;
    }
}