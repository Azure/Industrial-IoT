// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.v1.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Node read services
    /// </summary>
    [Route(VersionInfo.PATH + "/read")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanBrowse)]
    public class ReadController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="nodes"></param>
        public ReadController(INodeServices<string> nodes) {
            _nodes = nodes;
        }

        /// <summary>
        /// Read variable value
        /// </summary>
        /// <remarks>
        /// Read a variable node's value.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The read value request</param>
        /// <returns>The read value response</returns>
        [HttpPost("{endpointId}")]
        public async Task<ValueReadResponseApiModel> ReadValueAsync(
            string endpointId, [FromBody] [Required] ValueReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _nodes.NodeValueReadAsync(
                endpointId, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Read node attributes
        /// </summary>
        /// <remarks>
        /// Read attributes of a node.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The read request</param>
        /// <returns>The read response</returns>
        [HttpPost("{endpointId}/attributes")]
        public async Task<ReadResponseApiModel> ReadAttributesAsync(
            string endpointId, [FromBody] [Required] ReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _nodes.NodeReadAsync(
                endpointId, request.ToServiceModel());
            return new ReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Read node history
        /// </summary>
        /// <remarks>
        /// Read node history if available using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <returns>The history read response</returns>
        [HttpPost("{endpointId}/history")]
        public async Task<HistoryReadResponseApiModel> ReadHistoryAsync(
            string endpointId, [FromBody] [Required] HistoryReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _nodes.NodeHistoryReadAsync(
                endpointId, request.ToServiceModel());
            return new HistoryReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Read next batch of node history
        /// </summary>
        /// <remarks>
        /// Read next batch of node history values using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read next request</param>
        /// <returns>The history read response</returns>
        [HttpPost("{endpointId}/history/next")]
        public async Task<HistoryReadNextResponseApiModel> ReadHistoryNextAsync(
            string endpointId, [FromBody] [Required] HistoryReadNextRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _nodes.NodeHistoryReadNextAsync(
                endpointId, request.ToServiceModel());
            return new HistoryReadNextResponseApiModel(readresult);
        }

        /// <summary>
        /// Get variable value
        /// </summary>
        /// <remarks>
        /// Get a variable node's value using its node id.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="nodeId">The node to read</param>
        /// <returns>The read value response</returns>
        [HttpGet("{endpointId}")]
        public async Task<ValueReadResponseApiModel> GetValueAsync(
            string endpointId, [FromQuery] [Required] string nodeId) {
            if (string.IsNullOrEmpty(nodeId)) {
                throw new ArgumentNullException(nameof(nodeId));
            }
            var request = new ValueReadRequestApiModel { NodeId = nodeId };
            var readresult = await _nodes.NodeValueReadAsync(
                endpointId, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        private readonly INodeServices<string> _nodes;
    }
}
