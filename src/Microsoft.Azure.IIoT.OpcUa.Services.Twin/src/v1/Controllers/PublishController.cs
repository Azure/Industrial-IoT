// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Browse controller
    /// </summary>
    [Route(VersionInfo.PATH + "/publish")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanPublish)]
    public class PublishController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        public PublishController(IPublishServices<string> twin) {
            _twin = twin;
        }

        /// <summary>
        /// Start publishing node values on the specified server twin endpoint.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The publish request</param>
        /// <returns>The publish response</returns>
        [HttpPost("{id}/start")]
        public async Task<PublishStartResponseApiModel> PublishAsync(string id,
            [FromBody] PublishStartRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _twin.NodePublishStartAsync(
                id, request.ToServiceModel());
            return new PublishStartResponseApiModel(result);
        }

        /// <summary>
        /// Stop publishing node values on the specified server twin endpoint.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The unpublish request</param>
        /// <returns>The unpublish response</returns>
        [HttpPost("{id}/stop")]
        public async Task<PublishStopResponseApiModel> UnpublishAsync(string id,
            [FromBody] PublishStopRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _twin.NodePublishStopAsync(
                id, request.ToServiceModel());
            return new PublishStopResponseApiModel(result);
        }

        /// <summary>
        /// Returns currently published node ids.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <returns>The list of published nodes</returns>
        [HttpGet("{id}")]
        public async Task<PublishedNodeListResponseApiModel> ListAsync(
            string id) {
            string continuationToken = null;
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken].FirstOrDefault();
            }
            var result = await _twin.NodePublishListAsync(id,
                new PublishedNodeListRequestApiModel {
                    ContinuationToken = continuationToken
                }.ToServiceModel());
            return new PublishedNodeListResponseApiModel(result);
        }

        private readonly IPublishServices<string> _twin;
    }
}
