// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Browse controller
    /// </summary>
    [Route(VersionInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.ControlTwins)]
    public class PublishController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="adhoc"></param>
        public PublishController(IOpcUaPublishServices<string> twin,
            IOpcUaPublishServices<EndpointModel> adhoc) {
            _adhoc = adhoc;
            _twin = twin;
        }

        /// <summary>
        /// Publish node value as specified in the publish value request on the
        /// server specified in the endpoint object of the service request.
        /// </summary>
        /// <param name="request">The service request</param>
        /// <returns>The publish response</returns>
        [HttpPost]
        public async Task<PublishResponseApiModel> PublishAsync(
            [FromBody] ServiceRequestApiModel<PublishRequestApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: if token type is not "none", but user/token not, take from current claims

            var result = await _adhoc.NodePublishAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new PublishResponseApiModel(result);
        }

        /// <summary>
        /// Publish node value as specified in the publish value request on the
        /// server specified by the endpoint id.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The publish request</param>
        /// <returns>The publish response</returns>
        [HttpPost("{id}")]
        public async Task<PublishResponseApiModel> PublishByIdAsync(string id,
            [FromBody] PublishRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _twin.NodePublishAsync(
                id, request.ToServiceModel());
            return new PublishResponseApiModel(result);
        }

        /// <summary>
        /// Returns currently published node ids.
        /// </summary>
        /// <param name="endpoint">The endpoint to get published nodes for.</param>
        /// <returns>The list of published nodes</returns>
        [HttpPost("state")]
        public async Task<PublishedNodeListApiModel> ListPublishedNodesAsync(
            [FromBody] EndpointApiModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }

            // TODO: if token type is not "none", but user/token not, take from current claims

            string continuationToken = null;
            if (Request.Headers.ContainsKey(CONTINUATION_TOKEN_NAME)) {
                continuationToken = Request.Headers[CONTINUATION_TOKEN_NAME].FirstOrDefault();
            }
            var result = await _adhoc.ListPublishedNodesAsync(endpoint.ToServiceModel(),
                continuationToken);
            return new PublishedNodeListApiModel(result);
        }

        /// <summary>
        /// Returns currently published node ids.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <returns>The list of published nodes</returns>
        [HttpGet("{id}/state")]
        public async Task<PublishedNodeListApiModel> ListPublishedNodesByIdAsync(string id) {
            string continuationToken = null;
            if (Request.Headers.ContainsKey(CONTINUATION_TOKEN_NAME)) {
                continuationToken = Request.Headers[CONTINUATION_TOKEN_NAME].FirstOrDefault();
            }
            var result = await _twin.ListPublishedNodesAsync(id, continuationToken);
            return new PublishedNodeListApiModel(result);
        }

        private const string CONTINUATION_TOKEN_NAME = "x-ms-continuation";
        private readonly IOpcUaPublishServices<string> _twin;
        private readonly IOpcUaPublishServices<EndpointModel> _adhoc;
    }
}
