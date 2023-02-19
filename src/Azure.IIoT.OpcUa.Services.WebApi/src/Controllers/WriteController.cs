// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers {
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    /// <summary>
    /// Node writing services
    /// </summary>
    [ApiVersion("2")][Route("twin/v{version:apiVersion}/write")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanWrite)]
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
        public async Task<ValueWriteResponseModel> WriteValueAsync(
            string endpointId, [FromBody] [Required] ValueWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _nodes.NodeValueWriteAsync(
                endpointId, request);
            return writeResult;
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
        public async Task<WriteResponseModel> WriteAttributesAsync(
            string endpointId, [FromBody] [Required] WriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _nodes.NodeWriteAsync(
                endpointId, request);
            return writeResult;
        }

        private readonly INodeServices<string> _nodes;
    }
}
