// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Activate, Deactivate and Query endpoint resources
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/endpoints")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanQuery)]
    [ApiController]
    public class EndpointsController : ControllerBase {

        /// <summary>
        /// Create controller for endpoints services
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="activation"></param>
        public EndpointsController(IEndpointRegistry endpoints, IEndpointActivation activation) {
            _activation = activation;
            _endpoints = endpoints;
        }

        /// <summary>
        /// Get endpoint certificate chain
        /// </summary>
        /// <remarks>
        /// Gets current certificate of the endpoint.
        /// </remarks>
        /// <param name="endpointId">endpoint identifier</param>
        /// <returns>Endpoint registration</returns>
        [HttpGet("{endpointId}/certificate")]
        public async Task<X509CertificateChainApiModel> GetEndpointCertificateAsync(
            string endpointId) {
            var result = await _endpoints.GetEndpointCertificateAsync(endpointId);
            return result.ToApiModel();
        }

        /// <summary>
        /// Activate endpoint
        /// </summary>
        /// <remarks>
        /// Activates an endpoint for subsequent use in twin service.
        /// All endpoints must be activated using this API or through a
        /// activation filter during application registration or discovery.
        /// </remarks>
        /// <param name="endpointId">endpoint identifier</param>
        [HttpPost("{endpointId}/activate")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task ActivateEndpointAsync(string endpointId) {
            await _activation.ActivateEndpointAsync(endpointId);
        }

        /// <summary>
        /// Get endpoint information
        /// </summary>
        /// <remarks>
        /// Gets information about an endpoint.
        /// </remarks>
        /// <param name="endpointId">endpoint identifier</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <returns>Endpoint registration</returns>
        [HttpGet("{endpointId}")]
        public async Task<EndpointInfoApiModel> GetEndpointAsync(string endpointId,
            [FromQuery] bool? onlyServerState) {
            var result = await _endpoints.GetEndpointAsync(endpointId, onlyServerState ?? false);
            return result.ToApiModel();
        }

        /// <summary>
        /// Get list of endpoints
        /// </summary>
        /// <remarks>
        /// Get all registered endpoints in paged form.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call this operation again using the token to retrieve more results.
        /// </remarks>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if available</param>
        /// <param name="continuationToken">Optional Continuation token</param>
        /// <param name="pageSize">Optional number of results to return</param>
        /// <returns>List of endpoints and continuation token to use for next request</returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<EndpointInfoListApiModel> GetListOfEndpointsAsync(
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

            return result.ToApiModel();
        }

        /// <summary>
        /// Query endpoints
        /// </summary>
        /// <remarks>
        /// Return endpoints that match the specified query.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfEndpoints operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Query to match</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if available</param>
        /// <param name="pageSize">Optional number of results to return</param>
        /// <returns>List of endpoints and continuation token to use for next request</returns>
        [HttpPost("query")]
        public async Task<EndpointInfoListApiModel> QueryEndpointsAsync(
            [FromBody] [Required] EndpointRegistrationQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _endpoints.QueryEndpointsAsync(query.ToServiceModel(),
                onlyServerState ?? false, pageSize);

            return result.ToApiModel();
        }

        /// <summary>
        /// Get filtered list of endpoints
        /// </summary>
        /// <remarks>
        /// Get a list of endpoints filtered using the specified query parameters.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfEndpoints operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Query to match</param>
        /// <param name="onlyServerState">Whether to include only server state, or display
        /// current client state of the endpoint if available</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>List of endpoints and continuation token to use for next request</returns>
        [HttpGet("query")]
        public async Task<EndpointInfoListApiModel> GetFilteredListOfEndpointsAsync(
            [FromQuery] [Required] EndpointRegistrationQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _endpoints.QueryEndpointsAsync(query.ToServiceModel(),
                onlyServerState ?? false, pageSize);

            return result.ToApiModel();
        }

        /// <summary>
        /// Deactivate endpoint
        /// </summary>
        /// <remarks>
        /// Deactivates the endpoint and disable access through twin service.
        /// </remarks>
        /// <param name="endpointId">endpoint identifier</param>
        [HttpPost("{endpointId}/deactivate")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task DeactivateEndpointAsync(string endpointId) {
            await _activation.DeactivateEndpointAsync(endpointId);
        }

        private readonly IEndpointRegistry _endpoints;
        private readonly IEndpointActivation _activation;
    }
}
