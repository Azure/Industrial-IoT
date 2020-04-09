// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Browse nodes services
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/browse")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanBrowse)]
    [ApiController]
    public class BrowseController : ControllerBase {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browser"></param>
        public BrowseController(IBrowseServices<string> browser) {
            _browser = browser;
        }

        /// <summary>
        /// Browse node references
        /// </summary>
        /// <remarks>
        /// Browse a node on the specified endpoint.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The browse request</param>
        /// <returns>The browse response</returns>
        [HttpPost("{endpointId}")]
        public async Task<BrowseResponseApiModel> BrowseAsync(string endpointId,
            [FromBody] [Required] BrowseRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var browseresult = await _browser.NodeBrowseAsync(endpointId,
                request.ToServiceModel());
            return browseresult.ToApiModel();
        }

        /// <summary>
        /// Browse next set of references
        /// </summary>
        /// <remarks>
        /// Browse next set of references on the endpoint.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The request body with continuation token.</param>
        /// <returns>The browse response</returns>
        [HttpPost("{endpointId}/next")]
        public async Task<BrowseNextResponseApiModel> BrowseNextAsync(
            string endpointId, [FromBody] [Required] BrowseNextRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ContinuationToken == null) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var browseresult = await _browser.NodeBrowseNextAsync(endpointId,
                request.ToServiceModel());
            return browseresult.ToApiModel();
        }

        /// <summary>
        /// Browse using a browse path
        /// </summary>
        /// <remarks>
        /// Browse using a path from the specified node id.
        /// This call uses TranslateBrowsePathsToNodeIds service under the hood.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The browse path request</param>
        /// <returns>The browse path response</returns>
        [HttpPost("{endpointId}/path")]
        public async Task<BrowsePathResponseApiModel> BrowseUsingPathAsync(string endpointId,
            [FromBody] [Required] BrowsePathRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var browseresult = await _browser.NodeBrowsePathAsync(endpointId,
                request.ToServiceModel());
            return browseresult.ToApiModel();
        }

        /// <summary>
        /// Browse set of unique target nodes
        /// </summary>
        /// <remarks>
        /// Browse the set of unique hierarchically referenced target nodes on the endpoint.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// The root node id to browse from can be provided as part of the query
        /// parameters.
        /// If it is not provided, the RootFolder node is browsed. Note that this
        /// is the same as the POST method with the model containing the node id
        /// and the targetNodesOnly flag set to true.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="nodeId">The node to browse or omit to browse the root node (i=84)
        /// </param>
        /// <returns>The browse response</returns>
        [HttpGet("{endpointId}")]
        public async Task<BrowseResponseApiModel> GetSetOfUniqueNodesAsync(
            string endpointId, [FromQuery] string nodeId) {
            if (string.IsNullOrEmpty(nodeId)) {
                nodeId = null;
            }
            var request = new BrowseRequestModel {
                NodeId = nodeId,
                TargetNodesOnly = true,
                ReadVariableValues = true
            };
            var browseresult = await _browser.NodeBrowseAsync(endpointId, request);
            return browseresult.ToApiModel();
        }

        /// <summary>
        /// Browse next set of unique target nodes
        /// </summary>
        /// <remarks>
        /// Browse the next set of unique hierarchically referenced target nodes on the
        /// endpoint.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// Note that this is the same as the POST method with the model containing
        /// the continuation token and the targetNodesOnly flag set to true.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <returns>The browse response</returns>
        /// <param name="continuationToken">Continuation token from GetSetOfUniqueNodes operation
        /// </param>
        [HttpGet("{endpointId}/next")]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<BrowseNextResponseApiModel> GetNextSetOfUniqueNodesAsync(
            string endpointId, [FromQuery] [Required] string continuationToken) {
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (string.IsNullOrEmpty(continuationToken)) {
                throw new ArgumentNullException(nameof(continuationToken));
            }
            var request = new BrowseNextRequestModel {
                ContinuationToken = continuationToken,
                TargetNodesOnly = true,
                ReadVariableValues = true
            };
            var browseresult = await _browser.NodeBrowseNextAsync(endpointId, request);
            return browseresult.ToApiModel();
        }

        private readonly IBrowseServices<string> _browser;
    }
}
