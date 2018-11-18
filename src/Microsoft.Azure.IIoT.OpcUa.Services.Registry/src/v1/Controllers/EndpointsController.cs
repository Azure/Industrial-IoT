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
    /// Endpoints controller
    /// </summary>
    [Route(VersionInfo.PATH + "/endpoints")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanQuery)]
    public class EndpointsController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="endpoints"></param>
        public EndpointsController(IEndpointRegistry endpoints) {
            _endpoints = endpoints;
        }

        /// <summary>
        /// Activates the endpoint registration with the specified identifier.
        /// </summary>
        /// <param name="id">endpoint identifier</param>
        [HttpPost("{id}/activate")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task ActivateAsync(string id) {
            await _endpoints.ActivateEndpointAsync(id);
        }

        /// <summary>
        /// Update existing endpoint. Note that Id field in request
        /// must not be null and endpoint registration must exist.
        /// </summary>
        /// <param name="request">Endpoint update request</param>
        [HttpPatch]
        [Authorize(Policy = Policies.CanChange)]
        public async Task PatchAsync(
            [FromBody] EndpointRegistrationUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _endpoints.UpdateEndpointAsync(request.ToServiceModel());
        }

        /// <summary>
        /// Returns the endpoint registration with the specified identifier.
        /// </summary>
        /// <param name="id">endpoint identifier</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <returns>Endpoint registration</returns>
        [HttpGet("{id}")]
        public async Task<EndpointInfoApiModel> GetAsync(string id,
            [FromQuery] bool? onlyServerState) {
            var result = await _endpoints.GetEndpointAsync(id, onlyServerState ?? false);

            // TODO: Redact username/token in endpoint based on policy/permission

            return new EndpointInfoApiModel(result);
        }

        /// <summary>
        /// Get all registered endpoints in paged form.
        /// </summary>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <returns>
        /// List of endpoints and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<EndpointInfoListApiModel> ListAsync(
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
            var result = await _endpoints.ListEndpointsAsync(continuationToken,
                onlyServerState ?? false, pageSize);

            // TODO: Redact username/token based on policy/permission

            return new EndpointInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the endpoints that match the query.
        /// </summary>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <param name="model">Query to match</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Endpoint model list</returns>
        [HttpPost("query")]
        public async Task<EndpointInfoListApiModel> FindAsync(
            [FromBody] EndpointRegistrationQueryApiModel model,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _endpoints.QueryEndpointsAsync(model.ToServiceModel(),
                onlyServerState ?? false, pageSize);

            return new EndpointInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the endpoints using query from uri query
        /// </summary>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <param name="model">Query to match</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Endpoint model list</returns>
        [HttpGet("query")]
        public async Task<EndpointInfoListApiModel> QueryAsync(
            [FromQuery] EndpointRegistrationQueryApiModel model,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _endpoints.QueryEndpointsAsync(model.ToServiceModel(),
                onlyServerState ?? false, pageSize);

            return new EndpointInfoListApiModel(result);
        }

        /// <summary>
        /// Deactivates the endpoint registration with the specified identifier.
        /// </summary>
        /// <param name="id">endpoint identifier</param>
        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task DeactivateAsync(string id) {
            await _endpoints.DeactivateEndpointAsync(id);
        }

        private readonly IEndpointRegistry _endpoints;
    }
}
