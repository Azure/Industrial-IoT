// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using Furly.Extensions.Http;

    /// <summary>
    /// Activate, Deactivate and Query endpoint resources
    /// </summary>
    [ApiVersion("2")]
    [Route("registry/v{version:apiVersion}/endpoints")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class EndpointsController : ControllerBase
    {
        /// <summary>
        /// Create controller for endpoints services
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="manager"></param>
        /// <param name="activation"></param>
        /// <param name="certificates"></param>
        public EndpointsController(IEndpointRegistry endpoints,
            IEndpointManager manager, IConnectionServices<string> activation,
            ICertificateServices<string> certificates)
        {
            _manager = manager;
            _activation = activation;
            _certificates = certificates;
            _endpoints = endpoints;
        }

        /// <summary>
        /// Register endpoint
        /// </summary>
        /// <remarks>
        /// Adds an endpoint. This will onboard the endpoint and the associated
        /// application but no other endpoints. This call is synchronous and will
        /// return successful if endpoint is found. Otherwise the call will fail
        /// with error not found.
        /// </remarks>
        /// <param name="query">Query for the endpoint to register. This must
        /// have at least the discovery url. If more information is specified it
        /// is used to validate that the application has such endpoint and if
        /// not the call will fail.</param>
        /// <returns>Endpoint identifier</returns>
        [HttpPut]
        public async Task<string> RegisterEndpointAsync(ServerEndpointQueryModel query)
        {
            return await _manager.RegisterEndpointAsync(query).ConfigureAwait(false);
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
            [FromQuery] bool? onlyServerState)
        {
            return await _endpoints.GetEndpointAsync(endpointId, onlyServerState ?? false).ConfigureAwait(false);
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
            [FromQuery] int? pageSize)
        {
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken))
            {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount))
            {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }

            // TODO: Redact username/token based on policy/permission

            return await _endpoints.ListEndpointsAsync(continuationToken,
                onlyServerState ?? false, pageSize).ConfigureAwait(false);
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
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        [HttpPost("query")]
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            [FromBody][Required] EndpointRegistrationQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount))
            {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            return await _endpoints.QueryEndpointsAsync(query,
                onlyServerState ?? false, pageSize).ConfigureAwait(false);
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
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        [HttpGet("query")]
        public async Task<EndpointInfoListModel> GetFilteredListOfEndpointsAsync(
            [FromQuery][Required] EndpointRegistrationQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount))
            {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            return await _endpoints.QueryEndpointsAsync(query,
                onlyServerState ?? false, pageSize).ConfigureAwait(false);
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
        public async Task ConnectAsync(string endpointId)
        {
            await _activation.ConnectAsync(endpointId).ConfigureAwait(false);
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
            string endpointId)
        {
            return await _certificates.GetEndpointCertificateAsync(endpointId).ConfigureAwait(false);
        }

        /// <summary>
        /// Deactivate endpoint
        /// </summary>
        /// <remarks>
        /// Deactivates the endpoint and disable access through twin service.
        /// </remarks>
        /// <param name="endpointId">endpoint identifier</param>
        [HttpPost("{endpointId}/deactivate")]
        public async Task DisconnectAsync(string endpointId)
        {
            await _activation.DisconnectAsync(endpointId).ConfigureAwait(false);
        }

        private readonly IEndpointManager _manager;
        private readonly IEndpointRegistry _endpoints;
        private readonly IConnectionServices<string> _activation;
        private readonly ICertificateServices<string> _certificates;
    }
}
