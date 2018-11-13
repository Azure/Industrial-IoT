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
        /// <param name="twin"></param>
        public WriteController(INodeServices<string> twin) {
            _twin = twin;
        }

        /// <summary>
        /// Write node value as specified in the write value request on the server
        /// endpoint specified by the twin id.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The write value request</param>
        /// <returns>The write value response</returns>
        [HttpPost("{id}")]
        public async Task<ValueWriteResponseApiModel> WriteAsync(string id,
            [FromBody] ValueWriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _twin.NodeValueWriteAsync(
                id, request.ToServiceModel());
            return new ValueWriteResponseApiModel(writeResult);
        }

        /// <summary>
        /// Write any node attribute as batches. This allows updating attributes
        /// of any node in batch fashion.  However, note that batch requests
        /// and responses must fit into the device method  payload size limits.
        /// This is therefore considered an advanced api and should be used only
        /// when the request and response payload size is not dynamic and was
        /// well tested against a known endpoint.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The batch write request</param>
        /// <returns>The batch write response</returns>
        [HttpPost("{id}/batch")]
        public async Task<BatchWriteResponseApiModel> WriteBatchAsync(string id,
            [FromBody] BatchWriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _twin.NodeBatchWriteAsync(
                id, request.ToServiceModel());
            return new BatchWriteResponseApiModel(writeResult);
        }

        private readonly INodeServices<string> _twin;
    }
}
