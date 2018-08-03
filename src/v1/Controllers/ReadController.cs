// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Read value controller
    /// </summary>
    [Route(VersionInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policies.CanBrowse)]
    public class ReadController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        public ReadController(IOpcUaNodeServices<string> twin) {
            _twin = twin;
        }

        /// <summary>
        /// Read node value as specified in the read value request on the
        /// server specified by the twin id.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The read value request</param>
        /// <returns>The read value response</returns>
        [HttpPost("{id}")]
        public async Task<ValueReadResponseApiModel> ReadByIdAsync(string id,
            [FromBody] ValueReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _twin.NodeValueReadAsync(
                id, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Get node value from the node id passed through query on the
        /// server specified by the twin id.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="nodeId">The node to read</param>
        /// <returns>The read value response</returns>
        [HttpGet("{id}")]
        public async Task<ValueReadResponseApiModel> ReadByIdAsGetAsync(string id,
            [FromQuery] string nodeId) {
            if (string.IsNullOrEmpty(nodeId)) {
                throw new ArgumentNullException(nameof(nodeId));
            }
            var request = new ValueReadRequestApiModel { NodeId = nodeId };
            var readresult = await _twin.NodeValueReadAsync(
                id, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        private readonly IOpcUaNodeServices<string> _twin;
    }
}
