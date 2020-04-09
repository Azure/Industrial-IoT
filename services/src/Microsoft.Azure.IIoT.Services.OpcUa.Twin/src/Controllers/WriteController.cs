// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Node writing services
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/write")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanControl)]
    [ApiController]
    public class WriteController : ControllerBase {

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
            return writeResult.ToApiModel();
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
            return writeResult.ToApiModel();
        }

        private readonly INodeServices<string> _nodes;
    }
}
