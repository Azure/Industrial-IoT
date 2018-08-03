// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.Azure.IIoT.Http;
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
    [Authorize(Policy = Policies.CanPublish)]
    public class PublishController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        public PublishController(IOpcUaPublishServices<string> twin) {
            _twin = twin;
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
        /// <param name="id">The identifier of the twin.</param>
        /// <returns>The list of published nodes</returns>
        [HttpGet("{id}/state")]
        public async Task<PublishedNodeListApiModel> ListPublishedNodesByIdAsync(string id) {
            string continuationToken = null;
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken].FirstOrDefault();
            }
            var result = await _twin.ListPublishedNodesAsync(id, continuationToken);
            return new PublishedNodeListApiModel(result);
        }

        private readonly IOpcUaPublishServices<string> _twin;
    }
}
