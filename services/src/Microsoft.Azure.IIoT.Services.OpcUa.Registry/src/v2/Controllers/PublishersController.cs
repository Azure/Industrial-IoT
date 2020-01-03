// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;
    using System;
    using Swashbuckle.AspNetCore.Swagger;

    /// <summary>
    /// Read, Update and Query publisher resources
    /// </summary>
    [Route(VersionInfo.PATH + "/publishers")]
    [ExceptionsFilter]
    [Produces(ContentMimeType.Json)]
    [Authorize(Policy = Policies.CanQuery)]
    [ApiController]
    public class PublisherController : ControllerBase {

        /// <summary>
        /// Create controller for publisher services
        /// </summary>
        /// <param name="publishers"></param>
        /// <param name="events"></param>
        public PublisherController(IPublisherRegistry publishers,
            IGroupRegistration events) {
            _publishers = publishers;
            _events = events;
        }

        /// <summary>
        /// Get publisher registration information
        /// </summary>
        /// <remarks>
        /// Returns a publisher's registration and connectivity information.
        /// A publisher id corresponds to the twin modules module identity.
        /// </remarks>
        /// <param name="publisherId">Publisher identifier</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <returns>Publisher registration</returns>
        [HttpGet("{publisherId}")]
        public async Task<PublisherApiModel> GetPublisherAsync(string publisherId,
            [FromQuery] bool? onlyServerState) {
            var result = await _publishers.GetPublisherAsync(publisherId,
                onlyServerState ?? false);
            return new PublisherApiModel(result);
        }

        /// <summary>
        /// Update publisher configuration
        /// </summary>
        /// <remarks>
        /// Allows a caller to configure operations on the publisher module
        /// identified by the publisher id.
        /// </remarks>
        /// <param name="publisherId">Publisher identifier</param>
        /// <param name="request">Patch request</param>
        [HttpPatch("{publisherId}")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task UpdatePublisherAsync(string publisherId,
            [FromBody] [Required] PublisherUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _publishers.UpdatePublisherAsync(publisherId,
                request.ToServiceModel());
        }

        /// <summary>
        /// Get list of publishers
        /// </summary>
        /// <remarks>
        /// Get all registered publishers and therefore twin modules in paged form.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call this operation again using the token to retrieve more results.
        /// </remarks>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if available</param>
        /// <param name="continuationToken">Optional Continuation token</param>
        /// <param name="pageSize">Optional number of results to return</param>
        /// <returns>
        /// List of publishers and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<PublisherListApiModel> GetListOfPublisherAsync(
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
            var result = await _publishers.ListPublishersAsync(
                continuationToken, onlyServerState ?? false, pageSize);
            return new PublisherListApiModel(result);
        }

        /// <summary>
        /// Query publishers
        /// </summary>
        /// <remarks>
        /// Get all publishers that match a specified query.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfPublisher operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Publisher query model</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <returns>Publisher</returns>
        [HttpPost("query")]
        public async Task<PublisherListApiModel> QueryPublisherAsync(
            [FromBody] [Required] PublisherQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _publishers.QueryPublishersAsync(
                query.ToServiceModel(), onlyServerState ?? false, pageSize);

            // TODO: Filter results based on RBAC

            return new PublisherListApiModel(result);
        }

        /// <summary>
        /// Get filtered list of publishers
        /// </summary>
        /// <remarks>
        /// Get a list of publishers filtered using the specified query parameters.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfPublisher operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Publisher Query model</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the endpoint if
        /// available</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <returns>Publisher</returns>
        [HttpGet("query")]
        public async Task<PublisherListApiModel> GetFilteredListOfPublisherAsync(
            [FromQuery] [Required] PublisherQueryApiModel query,
            [FromQuery] bool? onlyServerState,
            [FromQuery] int? pageSize) {

            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _publishers.QueryPublishersAsync(
                query.ToServiceModel(), onlyServerState ?? false, pageSize);

            // TODO: Filter results based on RBAC

            return new PublisherListApiModel(result);
        }

        /// <summary>
        /// Subscribe to publisher registry events
        /// </summary>
        /// <remarks>
        /// Register a user to receive publisher events through SignalR.
        /// </remarks>
        /// <param name="userId">The user id that will receive publisher
        /// events.</param>
        /// <returns></returns>
        [HttpPut("events")]
        public async Task SubscribeAsync([FromBody]string userId) {
            await _events.SubscribeAsync("publishers", userId);
        }

        /// <summary>
        /// Unsubscribe registry events
        /// </summary>
        /// <remarks>
        /// Unregister a user and stop it from receiving publisher events.
        /// </remarks>
        /// <param name="userId">The user id that will not receive
        /// any more publisher events</param>
        /// <returns></returns>
        [HttpDelete("events/{userId}")]
        public async Task UnsubscribeAsync(string userId) {
            await _events.UnsubscribeAsync("publishers", userId);
        }

        private readonly IPublisherRegistry _publishers;
        private readonly IGroupRegistration _events;
    }
}
