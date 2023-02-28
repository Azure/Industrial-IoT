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
    /// Configure discovery
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
        /// <returns>Discoverer registration</returns>
        [HttpGet("{discovererId}")]
        public async Task<DiscovererModel> GetDiscovererAsync(string discovererId)
        {
            return await _discoverers.GetDiscovererAsync(discovererId).ConfigureAwait(false);
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
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPatch("{discovererId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task UpdateDiscovererAsync(string discovererId,
            [FromBody][Required] DiscovererUpdateModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            await _discoverers.UpdateDiscovererAsync(discovererId,
                request).ConfigureAwait(false);
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
        /// <returns>
        /// List of discoverers and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<DiscovererListModel> GetListOfDiscoverersAsync(
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
            return await _discoverers.ListDiscoverersAsync(
                continuationToken, pageSize).ConfigureAwait(false);
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
        /// <returns>Discoverers</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        [HttpPost("query")]
        public async Task<DiscovererListModel> QueryDiscoverersAsync(
            [FromBody][Required] DiscovererQueryModel query,
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
            return await _discoverers.QueryDiscoverersAsync(
                query, pageSize).ConfigureAwait(false);
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
        /// <returns>Discoverers</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        [HttpGet("query")]
        public async Task<DiscovererListModel> GetFilteredListOfDiscoverersAsync(
            [FromQuery][Required] DiscovererQueryModel query,
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
            return await _discoverers.QueryDiscoverersAsync(
                query, pageSize).ConfigureAwait(false);
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
        [HttpPost("{discovererId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task SetDiscoveryModeAsync(string discovererId,
            [FromQuery][Required] DiscoveryMode mode,
            [FromBody] DiscoveryConfigModel config)
        {
            var request = new DiscovererUpdateModel
            {
                Discovery = mode,
                DiscoveryConfig = config
            };
            await _discoverers.UpdateDiscovererAsync(discovererId,
                request).ConfigureAwait(false);
        }

        private readonly IDiscovererRegistry _discoverers;
    }
}
