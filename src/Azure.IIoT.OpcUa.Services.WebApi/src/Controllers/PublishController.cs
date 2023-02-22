// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers {
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Value and Event publishing services
    /// </summary>
    [ApiVersion("2")]
    [Route("publisher/v{version:apiVersion}/publish")]
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
        public async Task<PublishStartResponseModel> StartPublishingValuesAsync(
            string endpointId, [FromBody][Required] PublishStartRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishStartAsync(
                endpointId, request);
            return result;
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
        public async Task<PublishBulkResponseModel> BulkPublishValuesAsync(
            string endpointId, [FromBody][Required] PublishBulkRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishBulkAsync(
                endpointId, request);
            return result;
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
        public async Task<PublishStopResponseModel> StopPublishingValuesAsync(
            string endpointId, [FromBody][Required] PublishStopRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishStopAsync(
                endpointId, request);
            return result;
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
        public async Task<PublishedItemListResponseModel> GetFirstListOfPublishedNodesAsync(
            string endpointId, [FromBody][Required] PublishedItemListRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishListAsync(
                endpointId, request);
            return result;
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
        public async Task<PublishedItemListResponseModel> GetNextListOfPublishedNodesAsync(
            string endpointId, [FromQuery][Required] string continuationToken) {
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken].FirstOrDefault();
            }
            var result = await _publisher.NodePublishListAsync(endpointId,
                new PublishedItemListRequestModel {
                    ContinuationToken = continuationToken
                });
            return result;
        }

        private readonly IPublishServices<string> _publisher;
    }
}
