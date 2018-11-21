// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Nodes controller
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
        /// Write node value as specified in the write value request on the server
        /// endpoint specified by the twin id.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the endpoint.</param>
        /// <param name="request">The write value request</param>
        /// <returns>The write value response</returns>
        [HttpPost("{id}")]
        public async Task<ValueWriteResponseApiModel> WriteAsync(string id,
            [FromBody] ValueWriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _nodes.NodeValueWriteAsync(
                id, request.ToServiceModel());
            return new ValueWriteResponseApiModel(writeResult);
        }

        /// <summary>
        /// Write any node attribute.
        /// </summary>
        /// <param name="id">The identifier of the endpoint.</param>
        /// <param name="request">The batch write request</param>
        /// <returns>The batch write response</returns>
        [HttpPost("{id}/attributes")]
        public async Task<WriteResponseApiModel> WriteAttributesAsync(string id,
            [FromBody] WriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _nodes.NodeWriteAsync(
                id, request.ToServiceModel());
            return new WriteResponseApiModel(writeResult);
        }

        /// <summary>
        /// Update node history through historic access.
        /// </summary>
        /// <param name="id">The identifier of the endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <returns>The history read response</returns>
        [HttpPost("{id}/history")]
        public async Task<HistoryUpdateResponseApiModel> WriteHistoryAsync(
            string id, [FromBody] HistoryUpdateRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _nodes.NodeHistoryUpdateAsync(
                id, request.ToServiceModel());
            return new HistoryUpdateResponseApiModel(writeResult);
        }

        private readonly INodeServices<string> _nodes;
    }
}
