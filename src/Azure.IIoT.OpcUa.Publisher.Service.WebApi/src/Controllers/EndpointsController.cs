// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly.Extensions.AspNetCore.OpenApi;
    using Furly.Extensions.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Furly;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Activate, Deactivate and Query endpoint resources
    /// </summary>
    [ApiVersion("2")]
    [Route("registry/v{version:apiVersion}/endpoints")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
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
            _connections = activation;
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
        /// <param name="ct"></param>
        /// <returns>Endpoint identifier</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut]
        public async Task<string> RegisterEndpointAsync(ServerEndpointQueryModel query,
            CancellationToken ct)
        {
            return await _manager.RegisterEndpointAsync(query, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns>Endpoint registration</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("{endpointId}")]
        public async Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            [FromQuery] bool? onlyServerState, CancellationToken ct)
        {
            return await _endpoints.GetEndpointAsync(endpointId,
                onlyServerState ?? false, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns>List of endpoints and continuation token to use for next request</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<EndpointInfoListModel> GetListOfEndpointsAsync(
            [FromQuery] bool? onlyServerState,
            [FromQuery] string? continuationToken,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            if (Request.Headers.TryGetValue(HttpHeader.ContinuationToken, out var value))
            {
                continuationToken = value.FirstOrDefault();
            }
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }

            // TODO: Redact username/token based on policy/permission

            return await _endpoints.ListEndpointsAsync(continuationToken,
                onlyServerState ?? false, pageSize, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns>List of endpoints and continuation token to use for next request</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("query")]
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            [FromBody][Required] EndpointRegistrationQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!, CultureInfo.InvariantCulture);
            }
            return await _endpoints.QueryEndpointsAsync(query,
                onlyServerState ?? false, pageSize, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns>List of endpoints and continuation token to use for next request</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("query")]
        public async Task<EndpointInfoListModel> GetFilteredListOfEndpointsAsync(
            [FromQuery][Required] EndpointRegistrationQueryModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!, CultureInfo.InvariantCulture);
            }
            return await _endpoints.QueryEndpointsAsync(query,
                onlyServerState ?? false, pageSize, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Test endpoint is accessible
        /// </summary>
        /// <remarks>
        /// Test an endpoint can be connected to. Returns error
        /// information if connecting fails.
        /// </remarks>
        /// <param name="endpointId">endpoint identifier</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("{endpointId}/test")]
        public async Task<TestConnectionResponseModel> TestConnectionAsync(
            string endpointId, [FromBody][Required] TestConnectionRequestModel request,
            CancellationToken ct)
        {
            return await _connections.TestConnectionAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get endpoint certificate chain
        /// </summary>
        /// <remarks>
        /// Gets current certificate of the endpoint.
        /// </remarks>
        /// <param name="endpointId">endpoint identifier</param>
        /// <param name="ct"></param>
        /// <returns>Endpoint registration</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("{endpointId}/certificate")]
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string endpointId, CancellationToken ct)
        {
            return await _certificates.GetEndpointCertificateAsync(endpointId,
                ct).ConfigureAwait(false);
        }

        private readonly IEndpointManager _manager;
        private readonly IEndpointRegistry _endpoints;
        private readonly IConnectionServices<string> _connections;
        private readonly ICertificateServices<string> _certificates;
    }
}
