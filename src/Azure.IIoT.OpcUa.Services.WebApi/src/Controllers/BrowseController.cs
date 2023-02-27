// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Browse nodes services
    /// </summary>
    [ApiVersion("2")]
    [Route("twin/v{version:apiVersion}/browse")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class BrowseController : ControllerBase
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browser"></param>
        public BrowseController(INodeServices<string> browser)
        {
            _browser = browser;
        }

        /// <summary>
        /// Browse node references
        /// </summary>
        /// <remarks>
        /// Browse a node on the specified endpoint.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The browse request</param>
        /// <returns>The browse response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("{endpointId}")]
        public async Task<BrowseFirstResponseModel> BrowseAsync(string endpointId,
            [FromBody][Required] BrowseFirstRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _browser.BrowseAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next set of references
        /// </summary>
        /// <remarks>
        /// Browse next set of references on the endpoint.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The request body with continuation token.</param>
        /// <returns>The browse response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"></exception>
        [HttpPost("{endpointId}/next")]
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            string endpointId, [FromBody][Required] BrowseNextRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            return await _browser.BrowseNextAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse using a browse path
        /// </summary>
        /// <remarks>
        /// Browse using a path from the specified node id.
        /// This call uses TranslateBrowsePathsToNodeIds service under the hood.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The browse path request</param>
        /// <returns>The browse path response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("{endpointId}/path")]
        public async Task<BrowsePathResponseModel> BrowseUsingPathAsync(string endpointId,
            [FromBody][Required] BrowsePathRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _browser.BrowsePathAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse set of unique target nodes
        /// </summary>
        /// <remarks>
        /// Browse the set of unique hierarchically referenced target nodes on the endpoint.
        /// The endpoint must be in the registry and the server accessible.
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
        public async Task<BrowseFirstResponseModel> GetSetOfUniqueNodesAsync(
            string endpointId, [FromQuery] string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                nodeId = null;
            }
            var request = new BrowseFirstRequestModel
            {
                NodeId = nodeId,
                TargetNodesOnly = true,
                ReadVariableValues = true
            };
            return await _browser.BrowseAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next set of unique target nodes
        /// </summary>
        /// <remarks>
        /// Browse the next set of unique hierarchically referenced target nodes on the
        /// endpoint.
        /// The endpoint must be in the registry and the server accessible.
        /// Note that this is the same as the POST method with the model containing
        /// the continuation token and the targetNodesOnly flag set to true.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <returns>The browse response</returns>
        /// <param name="continuationToken">Continuation token from GetSetOfUniqueNodes operation
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        [HttpGet("{endpointId}/next")]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<BrowseNextResponseModel> GetNextSetOfUniqueNodesAsync(
            string endpointId, [FromQuery][Required] string continuationToken)
        {
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken))
            {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (string.IsNullOrEmpty(continuationToken))
            {
                throw new ArgumentNullException(nameof(continuationToken));
            }
            var request = new BrowseNextRequestModel
            {
                ContinuationToken = continuationToken,
                TargetNodesOnly = true,
                ReadVariableValues = true
            };
            return await _browser.BrowseNextAsync(endpointId, request).ConfigureAwait(false);
        }

        private readonly INodeServices<string> _browser;
    }
}
