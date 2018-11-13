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
    /// Read value controller
    /// </summary>
    [Route(VersionInfo.PATH + "/read")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanBrowse)]
    public class ReadController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        public ReadController(INodeServices<string> twin) {
            _twin = twin;
        }

        /// <summary>
        /// Read node value as specified in the read value request on the server
        /// endpoint specified by the twin id.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The read value request</param>
        /// <returns>The read value response</returns>
        [HttpPost("{id}")]
        public async Task<ValueReadResponseApiModel> ReadAsync(string id,
            [FromBody] ValueReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _twin.NodeValueReadAsync(
                id, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Read any node attribute as batches. This allows reading attributes
        /// of any node in batch fashion.  However, note that batch requests
        /// and responses must fit into the device method  payload size limits.
        /// This is therefore considered an advanced api and should be used only
        /// when the request and response payload size is not dynamic and was
        /// well tested against a known endpoint.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The batch read request</param>
        /// <returns>The batch read response</returns>
        [HttpPost("{id}/batch")]
        public async Task<BatchReadResponseApiModel> ReadBatchAsync(string id,
            [FromBody] BatchReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _twin.NodeBatchReadAsync(
                id, request.ToServiceModel());
            return new BatchReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Get node value from the node id passed through query on the server
        /// endpoint specified by the twin id.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="nodeId">The node to read</param>
        /// <returns>The read value response</returns>
        [HttpGet("{id}")]
        public async Task<ValueReadResponseApiModel> ReadAsGetAsync(string id,
            [FromQuery] string nodeId) {
            if (string.IsNullOrEmpty(nodeId)) {
                throw new ArgumentNullException(nameof(nodeId));
            }
            var request = new ValueReadRequestApiModel { NodeId = nodeId };
            var readresult = await _twin.NodeValueReadAsync(
                id, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        private readonly INodeServices<string> _twin;
    }
}
