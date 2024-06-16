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
        /// <param name="ct"></param>
        /// <returns>Gateway registration</returns>
        [HttpGet("{GatewayId}")]
        public async Task<GatewayInfoModel> GetGatewayAsync(string GatewayId,
            CancellationToken ct)
        {
            return await _gateways.GetGatewayAsync(GatewayId, ct: ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPatch("{GatewayId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task UpdateGatewayAsync(string GatewayId,
            [FromBody][Required] GatewayUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _gateways.UpdateGatewayAsync(GatewayId, request, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns>
        /// List of Gateways and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<GatewayListModel> GetListOfGatewayAsync(
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
            return await _gateways.ListGatewaysAsync(continuationToken,
                pageSize, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns>Gateway</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/>
        /// is <c>null</c>.</exception>
        [HttpPost("query")]
        public async Task<GatewayListModel> QueryGatewayAsync(
            [FromBody][Required] GatewayQueryModel query,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(
value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _gateways.QueryGatewaysAsync(query, pageSize,
                ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns>Gateway</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/>
        /// is <c>null</c>.</exception>
        [HttpGet("query")]
        public async Task<GatewayListModel> GetFilteredListOfGatewayAsync(
            [FromQuery][Required] GatewayQueryModel query,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(
value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _gateways.QueryGatewaysAsync(query, pageSize,
                ct).ConfigureAwait(false);
        }

        private readonly IGatewayRegistry _gateways;
    }
}
