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

    /// <summary>
    /// Read, Update and Query Gateway resources
    /// </summary>
    [ApiVersion("2")]
    [Route("registry/v{version:apiVersion}/gateways")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class GatewaysController : ControllerBase
    {
        /// <summary>
        /// Create controller for Gateway services
        /// </summary>
        /// <param name="gateways"></param>
        public GatewaysController(IGatewayRegistry gateways)
        {
            _gateways = gateways;
        }

        /// <summary>
        /// Get Gateway registration information
        /// </summary>
        /// <remarks>
        /// Returns a Gateway's registration and connectivity information.
        /// A Gateway id corresponds to the twin modules module identity.
        /// </remarks>
        /// <param name="GatewayId">Gateway identifier</param>
        /// <returns>Gateway registration</returns>
        [HttpGet("{GatewayId}")]
        public async Task<GatewayInfoModel> GetGatewayAsync(string GatewayId)
        {
            return await _gateways.GetGatewayAsync(GatewayId).ConfigureAwait(false);
        }

        /// <summary>
        /// Update Gateway configuration
        /// </summary>
        /// <remarks>
        /// Allows a caller to configure operations on the Gateway module
        /// identified by the Gateway id.
        /// </remarks>
        /// <param name="GatewayId">Gateway identifier</param>
        /// <param name="request">Patch request</param>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPatch("{GatewayId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task UpdateGatewayAsync(string GatewayId,
            [FromBody][Required] GatewayUpdateModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            await _gateways.UpdateGatewayAsync(GatewayId,
                request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get list of Gateways
        /// </summary>
        /// <remarks>
        /// Get all registered Gateways and therefore twin modules in paged form.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call this operation again using the token to retrieve more results.
        /// </remarks>
        /// <param name="continuationToken">Optional Continuation token</param>
        /// <param name="pageSize">Optional number of results to return</param>
        /// <returns>
        /// List of Gateways and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<GatewayListModel> GetListOfGatewayAsync(
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
            return await _gateways.ListGatewaysAsync(
                continuationToken, pageSize).ConfigureAwait(false);
        }

        /// <summary>
        /// Query Gateways
        /// </summary>
        /// <remarks>
        /// Get all Gateways that match a specified query.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfGateway operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Gateway query model</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <returns>Gateway</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        [HttpPost("query")]
        public async Task<GatewayListModel> QueryGatewayAsync(
            [FromBody][Required] GatewayQueryModel query,
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
            return await _gateways.QueryGatewaysAsync(
                query, pageSize).ConfigureAwait(false);
        }

        /// <summary>
        /// Get filtered list of Gateways
        /// </summary>
        /// <remarks>
        /// Get a list of Gateways filtered using the specified query parameters.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfGateway operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Gateway Query model</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <returns>Gateway</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        [HttpGet("query")]
        public async Task<GatewayListModel> GetFilteredListOfGatewayAsync(
            [FromQuery][Required] GatewayQueryModel query,
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
            return await _gateways.QueryGatewaysAsync(
                query, pageSize).ConfigureAwait(false);
        }

        private readonly IGatewayRegistry _gateways;
    }
}
