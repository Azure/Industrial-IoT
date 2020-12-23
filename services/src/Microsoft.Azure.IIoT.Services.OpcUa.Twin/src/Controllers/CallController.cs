// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    /// <summary>
    /// Call node method services
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/call")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanControl)]
    [ApiController]
    public class CallController : ControllerBase {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="nodes"></param>
        public CallController(INodeServices<string> nodes) {
            _nodes = nodes;
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <remarks>
        /// Return method meta data to support a user interface displaying forms to
        /// input and output arguments.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The method metadata request</param>
        /// <returns>The method metadata response</returns>
        [HttpPost("{endpointId}/metadata")]
        public async Task<MethodMetadataResponseApiModel> GetCallMetadataAsync(
            string endpointId, [FromBody] [Required] MethodMetadataRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var metadataresult = await _nodes.NodeMethodGetMetadataAsync(
                endpointId, request.ToServiceModel());
            return metadataresult.ToApiModel();
        }

        /// <summary>
        /// Call a method
        /// </summary>
        /// <remarks>
        /// Invoke method node with specified input arguments.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The method call request</param>
        /// <returns>The method call response</returns>
        [HttpPost("{endpointId}")]
        public async Task<MethodCallResponseApiModel> CallMethodAsync(
            string endpointId, [FromBody] [Required] MethodCallRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: Permissions

            var callresult = await _nodes.NodeMethodCallAsync(
                endpointId, request.ToServiceModel());
            return callresult.ToApiModel();
        }

        private readonly INodeServices<string> _nodes;
    }
}
