// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Value and Event publishing services
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/publish")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class PublishController : ControllerBase {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="publisher"></param>
        public PublishController(IPublishServices<string> publisher) {
            _publisher = publisher;
        }

        /// <summary>
        /// Start publishing node values
        /// </summary>
        /// <remarks>
        /// Start publishing variable node values to IoT Hub.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The publish request</param>
        /// <returns>The publish response</returns>
        [HttpPost("{endpointId}/start")]
        public async Task<PublishStartResponseApiModel> StartPublishingValuesAsync(
            string endpointId, [FromBody] [Required] PublishStartRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishStartAsync(
                endpointId, request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Bulk publish node values
        /// </summary>
        /// <remarks>
        /// Adds or removes in bulk values that should be published from a particular
        /// endpoint.
        /// </remarks>
        /// <param name="endpointId">The identifier of an activated endpoint.</param>
        /// <param name="request">The bulk publish request</param>
        /// <returns>The bulk publish response</returns>
        [HttpPost("{endpointId}/bulk")]
        public async Task<PublishBulkResponseApiModel> BulkPublishValuesAsync(
            string endpointId, [FromBody] [Required] PublishBulkRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishBulkAsync(
                endpointId, request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Stop publishing node values
        /// </summary>
        /// <remarks>
        /// Stop publishing variable node values to IoT Hub.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The unpublish request</param>
        /// <returns>The unpublish response</returns>
        [HttpPost("{endpointId}/stop")]
        public async Task<PublishStopResponseApiModel> StopPublishingValuesAsync(
            string endpointId, [FromBody] [Required] PublishStopRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishStopAsync(
                endpointId, request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Get currently published nodes
        /// </summary>
        /// <remarks>
        /// Returns currently published node ids for an endpoint.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The list request</param>
        /// <returns>The list of published nodes</returns>
        [HttpPost("{endpointId}")]
        public async Task<PublishedItemListResponseApiModel> GetFirstListOfPublishedNodesAsync(
            string endpointId, [FromBody] [Required] PublishedItemListRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishListAsync(
                endpointId, request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Get next set of published nodes
        /// </summary>
        /// <remarks>
        /// Returns next set of currently published node ids for an endpoint.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="continuationToken">The continuation token to continue with</param>
        /// <returns>The list of published nodes</returns>
        [HttpGet("{endpointId}")]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<PublishedItemListResponseApiModel> GetNextListOfPublishedNodesAsync(
            string endpointId, [FromQuery] [Required] string continuationToken) {
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken].FirstOrDefault();
            }
            var result = await _publisher.NodePublishListAsync(endpointId,
                new PublishedItemListRequestApiModel {
                    ContinuationToken = continuationToken
                }.ToServiceModel());
            return result.ToApiModel();
        }

        private readonly IPublishServices<string> _publisher;
    }
}
