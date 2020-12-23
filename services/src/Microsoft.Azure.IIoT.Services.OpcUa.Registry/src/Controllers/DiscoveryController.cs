// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;
    using System;
    using System.Linq;

    /// <summary>
    /// Configure discovery
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/discovery")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanQuery)]
    [ApiController]
    public class DiscoveryController : ControllerBase {

        /// <summary>
        /// Create controller for discovery services
        /// </summary>
        /// <param name="discoverers"></param>
        public DiscoveryController(IDiscovererRegistry discoverers) {
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
        public async Task<DiscovererApiModel> GetDiscovererAsync(string discovererId) {
            var result = await _discoverers.GetDiscovererAsync(discovererId);
            return result.ToApiModel();
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
        [HttpPatch("{discovererId}")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task UpdateDiscovererAsync(string discovererId,
            [FromBody] [Required] DiscovererUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _discoverers.UpdateDiscovererAsync(discovererId,
                request.ToServiceModel());
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
        public async Task<DiscovererListApiModel> GetListOfDiscoverersAsync(
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
            var result = await _discoverers.ListDiscoverersAsync(
                continuationToken, pageSize);
            return result.ToApiModel();
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
        [HttpPost("query")]
        public async Task<DiscovererListApiModel> QueryDiscoverersAsync(
            [FromBody] [Required] DiscovererQueryApiModel query,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _discoverers.QueryDiscoverersAsync(
                query.ToServiceModel(), pageSize);

            return result.ToApiModel();
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
        [HttpGet("query")]
        public async Task<DiscovererListApiModel> GetFilteredListOfDiscoverersAsync(
            [FromQuery] [Required] DiscovererQueryApiModel query,
            [FromQuery] int? pageSize) {

            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _discoverers.QueryDiscoverersAsync(
                query.ToServiceModel(), pageSize);

            return result.ToApiModel();
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
        [Authorize(Policy = Policies.CanChange)]
        public async Task SetDiscoveryModeAsync(string discovererId,
            [FromQuery] [Required] IIoT.OpcUa.Api.Registry.Models.DiscoveryMode mode,
            [FromBody] DiscoveryConfigApiModel config) {
            var request = new DiscovererUpdateApiModel {
                Discovery = mode,
                DiscoveryConfig = config
            };
            await _discoverers.UpdateDiscovererAsync(discovererId,
                request.ToServiceModel());
        }

        private readonly IDiscovererRegistry _discoverers;
    }
}
