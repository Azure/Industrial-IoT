// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
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
    [Produces(ContentMimeType.Json)]
    [Authorize(Policy = Policies.CanQuery)]
    [ApiController]
    public class DiscoverersController : ControllerBase {

        /// <summary>
        /// Create controller for discovery services
        /// </summary>
        /// <param name="discoverers"></param>
        /// <param name="events"></param>
        public DiscoverersController(IDiscovererRegistry discoverers,
            IGroupRegistration events) {
            _discoverers = discoverers;
            _events = events;
        }

        /// <summary>
        /// Get discoverer registration information
        /// </summary>
        /// <remarks>
        /// Returns a discoverer's registration and connectivity information.
        /// A discoverer id corresponds to the twin modules module identity.
        /// </remarks>
        /// <param name="discovererId">Discoverer identifier</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <returns>Discoverer registration</returns>
        [HttpGet("{discovererId}")]
        public async Task<DiscovererApiModel> GetDiscovererAsync(string discovererId,
            [FromQuery] bool? onlyServerState) {
            var result = await _discoverers.GetDiscovererAsync(discovererId,
                onlyServerState ?? false);
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
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if available</param>
        /// <param name="continuationToken">Optional Continuation token</param>
        /// <param name="pageSize">Optional number of results to return</param>
        /// <returns>
        /// List of discoverers and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<DiscovererListApiModel> GetListOfDiscoverersAsync(
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
            var result = await _discoverers.ListDiscoverersAsync(
                continuationToken, onlyServerState ?? false, pageSize);
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
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <returns>Discoverers</returns>
        [HttpPost("query")]
        public async Task<DiscovererListApiModel> QueryDiscoverersAsync(
            [FromBody] [Required] DiscovererQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _discoverers.QueryDiscoverersAsync(
                query.ToServiceModel(), onlyServerState ?? false, pageSize);

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
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <returns>Discoverers</returns>
        [HttpGet("query")]
        public async Task<DiscovererListApiModel> GetFilteredListOfDiscoverersAsync(
            [FromQuery] [Required] DiscovererQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {

            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _discoverers.QueryDiscoverersAsync(
                query.ToServiceModel(), onlyServerState ?? false, pageSize);

            return result.ToApiModel();
        }

        /// <summary>
        /// Subscribe to discoverer registry events
        /// </summary>
        /// <remarks>
        /// Register a user to receive discoverer events through SignalR.
        /// </remarks>
        /// <param name="userId">The user id that will receive discoverer
        /// events.</param>
        /// <returns></returns>
        [HttpPut("events")]
        public async Task SubscribeAsync([FromBody]string userId) {
            await _events.SubscribeAsync("discovery", userId);
        }

        /// <summary>
        /// Unsubscribe registry events
        /// </summary>
        /// <remarks>
        /// Unregister a user and stop it from receiving discoverer events.
        /// </remarks>
        /// <param name="userId">The user id that will not receive
        /// any more discoverer events</param>
        /// <returns></returns>
        [HttpDelete("events/{userId}")]
        public async Task UnsubscribeAsync(string userId) {
            await _events.UnsubscribeAsync("discovery", userId);
        }

        /// <summary>
        /// Subscribe to discovery progress from discoverer
        /// </summary>
        /// <remarks>
        /// Register a client to receive discovery progress events
        /// through SignalR from a particular discoverer.
        /// </remarks>
        /// <param name="discovererId">The discoverer to subscribe to</param>
        /// <param name="userId">The user id that will receive discovery
        /// events.</param>
        /// <returns></returns>
        [HttpPut("{discovererId}/events")]
        public async Task SubscribeByDiscovererIdAsync(string discovererId,
            [FromBody] string userId) {
            await _events.SubscribeAsync(discovererId, userId);
        }

        /// <summary>
        /// Subscribe to discovery progress for a request
        /// </summary>
        /// <remarks>
        /// Register a client to receive discovery progress events
        /// through SignalR for a particular request.
        /// </remarks>
        /// <param name="requestId">The request to monitor</param>
        /// <param name="userId">The user id that will receive discovery
        /// events.</param>
        /// <returns></returns>
        [HttpPut("requests/{requestId}/events")]
        public async Task SubscribeByRequestIdAsync(string requestId,
            [FromBody] string userId) {
            await _events.SubscribeAsync(requestId, userId);
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

        /// <summary>
        /// Unsubscribe from discovery progress for a request.
        /// </summary>
        /// <remarks>
        /// Unregister a client and stop it from receiving discovery
        /// events for a particular request.
        /// </remarks>
        /// <param name="requestId">The request to unsubscribe from
        /// </param>
        /// <param name="userId">The user id that will not receive
        /// any more discovery progress</param>
        /// <returns></returns>
        [HttpDelete("requests/{requestId}/events/{userId}")]
        public async Task UnsubscribeByRequestIdAsync(string requestId,
            string userId) {
            await _events.UnsubscribeAsync(requestId, userId);
        }

        /// <summary>
        /// Unsubscribe from discovery progress from discoverer.
        /// </summary>
        /// <remarks>
        /// Unregister a client and stop it from receiving discovery events.
        /// </remarks>
        /// <param name="discovererId">The discoverer to unsubscribe from
        /// </param>
        /// <param name="userId">The user id that will not receive
        /// any more discovery progress</param>
        /// <returns></returns>
        [HttpDelete("{discovererId}/events/{userId}")]
        public async Task UnsubscribeByDiscovererIdAsync(string discovererId,
            string userId) {
            await _events.UnsubscribeAsync(discovererId, userId);
        }

        private readonly IDiscovererRegistry _discoverers;
        private readonly IGroupRegistration _events;
    }
}
