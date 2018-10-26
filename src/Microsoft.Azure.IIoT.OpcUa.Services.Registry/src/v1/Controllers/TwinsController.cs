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
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Twins controller
    /// </summary>
    [Route(VersionInfo.PATH + "/twins")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanQuery)]
    public class TwinsController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twins"></param>
        public TwinsController(ITwinRegistry twins) {
            _twins = twins;
        }

        /// <summary>
        /// Activates the twin registration with the specified identifier.
        /// </summary>
        /// <param name="id">twin identifier</param>
        [HttpPost("activate/{id}")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task ActivateAsync(string id) {
            await _twins.ActivateTwinAsync(id);
        }

        /// <summary>
        /// Update existing twin. Note that Id field in request
        /// must not be null and twin registration must exist.
        /// </summary>
        /// <param name="request">Twin update request</param>
        [HttpPatch]
        [Authorize(Policy = Policies.CanChange)]
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

            return new TwinInfoApiModel(result);
        }

        /// <summary>
        /// Get all registered twins in paged form.
        /// </summary>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the twin if
        /// available</param>
        /// <returns>
        /// List of twins and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<TwinInfoListApiModel> ListAsync(
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
            var result = await _twins.ListTwinsAsync(continuationToken,
                onlyServerState ?? false, pageSize);

            // TODO: Redact username/token based on policy/permission

            return new TwinInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the twins that match the query.
        /// </summary>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the twin if
        /// available</param>
        /// <param name="model">Query to match</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Twin model list</returns>
        [HttpPost("query")]
        public async Task<TwinInfoListApiModel> FindAsync(
            [FromBody] TwinRegistrationQueryApiModel model,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _twins.QueryTwinsAsync(model.ToServiceModel(),
                onlyServerState ?? false, pageSize);

            return new TwinInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the twins using query from uri query
        /// </summary>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the twin if
        /// available</param>
        /// <param name="model">Query to match</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Twin model list</returns>
        [HttpGet("query")]
        public async Task<TwinInfoListApiModel> QueryAsync(
            [FromQuery] TwinRegistrationQueryApiModel model,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _twins.QueryTwinsAsync(model.ToServiceModel(),
                onlyServerState ?? false, pageSize);

            return new TwinInfoListApiModel(result);
        }

        /// <summary>
        /// Deactivates the twin registration with the specified identifier.
        /// </summary>
        /// <param name="id">twin identifier</param>
        [HttpPost("deactivate/{id}")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task DeactivateAsync(string id) {
            await _twins.DeactivateTwinAsync(id);
        }

        private readonly ITwinRegistry _twins;
    }
}
