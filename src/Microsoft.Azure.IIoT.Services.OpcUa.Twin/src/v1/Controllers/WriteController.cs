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
    /// Node writing services
    /// </summary>
    [Route(VersionInfo.PATH + "/write")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanControl)]
    public class WriteController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="nodes"></param>
        public WriteController(INodeServices<string> nodes) {
            _nodes = nodes;
        }

        /// <summary>
        /// Write variable value
        /// </summary>
        /// <remarks>
        /// Write variable node's value.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The write value request</param>
        /// <returns>The write value response</returns>
        [HttpPost("{endpointId}")]
        public async Task<ValueWriteResponseApiModel> WriteValueAsync(
            string endpointId, [FromBody] [Required] ValueWriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _nodes.NodeValueWriteAsync(
                endpointId, request.ToServiceModel());
            return new ValueWriteResponseApiModel(writeResult);
        }

        /// <summary>
        /// Write node attributes
        /// </summary>
        /// <remarks>
        /// Write any attribute of a node.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The batch write request</param>
        /// <returns>The batch write response</returns>
        [HttpPost("{endpointId}/attributes")]
        public async Task<WriteResponseApiModel> WriteAttributesAsync(
            string endpointId, [FromBody] [Required] WriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _nodes.NodeWriteAsync(
                endpointId, request.ToServiceModel());
            return new WriteResponseApiModel(writeResult);
        }

        /// <summary>
        /// Update node history
        /// </summary>
        /// <remarks>
        /// Update node history using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <returns>The history read response</returns>
        [HttpPost("{endpointId}/history")]
        public async Task<HistoryUpdateResponseApiModel> WriteHistoryAsync(
            string endpointId, [FromBody] [Required] HistoryUpdateRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _nodes.NodeHistoryUpdateAsync(
                endpointId, request.ToServiceModel());
            return new HistoryUpdateResponseApiModel(writeResult);
        }

        private readonly INodeServices<string> _nodes;
    }
}
