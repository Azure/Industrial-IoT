// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers {
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Activate, Deactivate and Query endpoint resources
    /// </summary>
    [ApiVersion("2")][Route("registry/v{version:apiVersion}/endpoints")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class EndpointsController : ControllerBase {

        /// <summary>
        /// Create controller for endpoints services
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="activation"></param>
        /// <param name="certificates"></param>
        public EndpointsController(IEndpointRegistry endpoints,
            IConnectionServices<string> activation,
            ICertificateServices<string> certificates) {
            _activation = activation;
            _certificates = certificates;
            _endpoints = endpoints;
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
        public async Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            [FromQuery] bool? onlyServerState) {
            var result = await _endpoints.GetEndpointAsync(endpointId, onlyServerState ?? false);
            return result;
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
        public async Task<EndpointInfoListModel> GetListOfEndpointsAsync(
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

            return result;
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
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            [FromBody] [Required] EndpointRegistrationQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _endpoints.QueryEndpointsAsync(query,
                onlyServerState ?? false, pageSize);

            return result;
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
        public async Task<EndpointInfoListModel> GetFilteredListOfEndpointsAsync(
            [FromQuery] [Required] EndpointRegistrationQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _endpoints.QueryEndpointsAsync(query,
                onlyServerState ?? false, pageSize);

            return result;
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
        public async Task ConnectAsync(string endpointId) {
            await _activation.ConnectAsync(endpointId);
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
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string endpointId) {
            var result = await _certificates.GetEndpointCertificateAsync(endpointId);
            return result;
        }

        /// <summary>
        /// Deactivate endpoint
        /// </summary>
        /// <remarks>
        /// Deactivates the endpoint and disable access through twin service.
        /// </remarks>
        /// <param name="endpointId">endpoint identifier</param>
        [HttpPost("{endpointId}/deactivate")]
        public async Task DisconnectAsync(string endpointId) {
            await _activation.DisconnectAsync(endpointId);
        }

        private readonly IEndpointRegistry _endpoints;
        private readonly IConnectionServices<string> _activation;
        private readonly ICertificateServices<string> _certificates;
    }
}
