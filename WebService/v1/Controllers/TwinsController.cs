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
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Twins controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.BrowseTwins)]
    public class TwinsController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twins"></param>
        public TwinsController(IOpcUaTwinRegistry twins) {
            _twins = twins;
        }

        /// <summary>
        /// Update existing twin. Note that Id field in request
        /// must not be null and twin registration must exist.
        /// </summary>
        /// <param name="request">Twin update request</param>
        [HttpPatch]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task PatchAsync(
            [FromBody] TwinRegistrationUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _twins.UpdateTwinAsync(request.ToServiceModel());
        }

        /// <summary>
        /// Returns the twin registration with the specified identifier.
        /// </summary>
        /// <param name="id">twin identifier</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the twin if
        /// available</param>
        /// <returns>Twin registration</returns>
        [HttpGet("{id}")]
        public async Task<TwinInfoApiModel> GetAsync(string id,
            [FromQuery] bool? onlyServerState) {
            var result = await _twins.GetTwinAsync(id, onlyServerState ?? false);

            // TODO: Redact username/token in twin based on policy/permission
            // TODO: Filter twins based on RBAC

            return new TwinInfoApiModel(result);
        }

        /// <summary>
        /// Get all registered twins in paged form.
        /// </summary>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the twin if
        /// available</param>
        /// <returns>
        /// List of twins and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<TwinInfoListApiModel> ListAsync(
            [FromQuery] bool? onlyServerState) {
            string continuationToken = null;
            if (Request.Headers.ContainsKey(CONTINUATION_TOKEN_NAME)) {
                continuationToken = Request.Headers[CONTINUATION_TOKEN_NAME]
                    .FirstOrDefault();
            }
            var result = await _twins.ListTwinsAsync(continuationToken,
                onlyServerState ?? false);

            // TODO: Redact username/token based on policy/permission
            // TODO: Filter twins based on RBAC

            return new TwinInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the twins that match the query.
        /// </summary>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the twin if
        /// available</param>
        /// <param name="model">Query to match</param>
        /// <returns>Twin model list</returns>
        [HttpPost("query")]
        public async Task<TwinInfoListApiModel> FindAsync(
            [FromBody] TwinRegistrationQueryApiModel model,
            [FromQuery] bool? onlyServerState) {
            var result = await _twins.QueryTwinsAsync(model.ToServiceModel(),
                onlyServerState ?? false);

            // TODO: Filter twins based on RBAC

            return new TwinInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the twins using query from uri query
        /// </summary>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the twin if
        /// available</param>
        /// <param name="model">Query to match</param>
        /// <returns>Twin model list</returns>
        [HttpGet("query")]
        public async Task<TwinInfoListApiModel> QueryAsync(
            [FromQuery] TwinRegistrationQueryApiModel model,
            [FromQuery] bool? onlyServerState) {
            var result = await _twins.QueryTwinsAsync(model.ToServiceModel(),
                onlyServerState ?? false);

            // TODO: Filter twins based on RBAC

            return new TwinInfoListApiModel(result);
        }

        private const string CONTINUATION_TOKEN_NAME = "x-ms-continuation";
        private readonly IOpcUaTwinRegistry _twins;
    }
}