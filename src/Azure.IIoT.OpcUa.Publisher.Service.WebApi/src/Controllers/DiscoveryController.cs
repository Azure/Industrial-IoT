// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
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
    /// Discovery
    /// </summary>
    [ApiVersion("2")]
    [Route("registry/v{version:apiVersion}/discovery")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class DiscoveryController : ControllerBase
    {
        /// <summary>
        /// Create controller for discovery services
        /// </summary>
        /// <param name="discoverers"></param>
        public DiscoveryController(IDiscovererRegistry discoverers)
        {
            _discoverers = discoverers;
        }

        /// <summary>
        /// Get discoverer registration information
        /// </summary>
        /// <remarks>
        /// Returns a discoverer's registration and connectivity information.
        /// A discoverer id corresponds to the twin modules module identity.
        /// </remarks>
        /// <param name="discovererId">Discoverer identifier</param>
        /// <param name="ct"></param>
        /// <returns>Discoverer registration</returns>
        [HttpGet("{discovererId}")]
        public async Task<DiscovererModel> GetDiscovererAsync(string discovererId,
            CancellationToken ct)
        {
            return await _discoverers.GetDiscovererAsync(discovererId,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Update discoverer information
        /// </summary>
        /// <remarks>
        /// Allows a caller to configure recurring discovery runs on the twin module
        /// identified by the discoverer id or update site information.
        /// </remarks>
        /// <param name="discovererId">discoverer identifier</param>
        /// <param name="request">Patch request</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPatch("{discovererId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task UpdateDiscovererAsync(string discovererId,
            [FromBody][Required] DiscovererUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _discoverers.UpdateDiscovererAsync(discovererId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get list of discoverers
        /// </summary>
        /// <remarks>
        /// Get all registered discoverers and therefore twin modules in paged form.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call this operation again using the token to retrieve more results.
        /// </remarks>
        /// <param name="continuationToken">Optional Continuation token</param>
        /// <param name="pageSize">Optional number of results to return</param>
        /// <param name="ct"></param>
        /// <returns>
        /// List of discoverers and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<DiscovererListModel> GetListOfDiscoverersAsync(
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
            return await _discoverers.ListDiscoverersAsync(continuationToken,
                pageSize, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Query discoverers
        /// </summary>
        /// <remarks>
        /// Get all discoverers that match a specified query.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfDiscoverers operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Discoverers query model</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <param name="ct"></param>
        /// <returns>Discoverers</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/>
        /// is <c>null</c>.</exception>
        [HttpPost("query")]
        public async Task<DiscovererListModel> QueryDiscoverersAsync(
            [FromBody][Required] DiscovererQueryModel query,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(
value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _discoverers.QueryDiscoverersAsync(query,
                pageSize, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get filtered list of discoverers
        /// </summary>
        /// <remarks>
        /// Get a list of discoverers filtered using the specified query parameters.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfDiscoverers operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Discoverers Query model</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <param name="ct"></param>
        /// <returns>Discoverers</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/>
        /// is <c>null</c>.</exception>
        [HttpGet("query")]
        public async Task<DiscovererListModel> GetFilteredListOfDiscoverersAsync(
            [FromQuery][Required] DiscovererQueryModel query,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(
value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _discoverers.QueryDiscoverersAsync(query,
                pageSize, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Enable server discovery
        /// </summary>
        /// <remarks>
        /// Allows a caller to configure recurring discovery runs on the
        /// discovery module identified by the module id.
        /// </remarks>
        /// <param name="discovererId">discoverer identifier</param>
        /// <param name="mode">Discovery mode</param>
        /// <param name="config">Discovery configuration</param>
        /// <param name="ct"></param>
        [HttpPost("{discovererId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task SetDiscoveryModeAsync(string discovererId,
            [FromQuery][Required] DiscoveryMode mode,
            [FromBody] DiscoveryConfigModel config, CancellationToken ct)
        {
            var request = new DiscovererUpdateModel
            {
                Discovery = mode,
                DiscoveryConfig = config
            };
            await _discoverers.UpdateDiscovererAsync(discovererId,
                request, ct).ConfigureAwait(false);
        }

        private readonly IDiscovererRegistry _discoverers;
    }
}
