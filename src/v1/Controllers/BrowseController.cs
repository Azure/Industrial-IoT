// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.Azure.IIoT.OpcUa.Models;
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
    [Authorize(Policy = Policies.CanBrowse)]
    public class BrowseController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        public BrowseController(IBrowseServices<string> twin) {
            _twin = twin;
        }

        /// <summary>
        /// Browse a node on the twin specified by the passed in id using the
        /// specified browse configuration.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The browse request</param>
        /// <returns>The browse response</returns>
        [HttpPost("{id}")]
        public async Task<BrowseResponseApiModel> BrowseByIdAsync(string id,
            [FromBody] BrowseRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var browseresult = await _twin.NodeBrowseAsync(id,
                request.ToServiceModel());
            return new BrowseResponseApiModel(browseresult);
        }

        /// <summary>
        /// Browse next set of references on the twin specified by the passed in id.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <returns>The browse response</returns>
        /// <param name="request">Continuation token</param>
        [HttpPost("{id}/next")]
        public async Task<BrowseNextResponseApiModel> BrowseNextByIdAsync(string id,
            [FromBody] BrowseNextRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ContinuationToken == null) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var browseresult = await _twin.NodeBrowseNextAsync(id,
                request.ToServiceModel());
            return new BrowseNextResponseApiModel(browseresult);
        }

        /// <summary>
        /// Browse the set of unique hierarchically referenced target nodes on the
        /// twin specified by the passed in id.
        /// The root node id to browse from can be provided as part of the query
        /// parameters.
        /// If it is not provided, the ObjectRoot node is browsed. Note that this
        /// is the same as the POST method with the model containing the node id
        /// and the targetNodesOnly flag set to true.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="nodeId">The node to browse or omit to browse
        /// object root</param>
        /// <returns>The browse response</returns>
        [HttpGet("{id}")]
        public async Task<BrowseResponseApiModel> BrowseByIdAsGetAsync(string id,
            [FromQuery] string nodeId) {
            if (string.IsNullOrEmpty(nodeId)) {
                nodeId = null;
            }
            var request = new BrowseRequestModel {
                NodeId = nodeId,
                TargetNodesOnly = true
            };
            var browseresult = await _twin.NodeBrowseAsync(id, request);
            return new BrowseResponseApiModel(browseresult);
        }

        /// <summary>
        /// Browse next set of hierarchically referenced target nodes on the twin
        /// specified by the passed in id. Note that this is the same as the POST
        /// method with the model containing the continuation token and the
        /// targetNodesOnly flag set to true.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <returns>The browse response</returns>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        [HttpGet("{id}/next")]
        public async Task<BrowseNextResponseApiModel> BrowseNextByIdAsGetAsync(string id,
            [FromQuery] string continuationToken) {
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (string.IsNullOrEmpty(continuationToken)) {
                throw new ArgumentNullException(nameof(continuationToken));
            }
            var request = new BrowseNextRequestModel {
                ContinuationToken = continuationToken,
                TargetNodesOnly = true
            };
            var browseresult = await _twin.NodeBrowseNextAsync(id, request);
            return new BrowseNextResponseApiModel(browseresult);
        }

        private readonly IBrowseServices<string> _twin;
    }
}
