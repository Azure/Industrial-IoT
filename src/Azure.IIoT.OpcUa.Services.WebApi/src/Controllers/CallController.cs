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
    /// Call node method services
    /// </summary>
    [ApiVersion("2")]
    [Route("twin/v{version:apiVersion}/call")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanWrite)]
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
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The method metadata request</param>
        /// <returns>The method metadata response</returns>
        [HttpPost("{endpointId}/metadata")]
        public async Task<MethodMetadataResponseModel> GetCallMetadataAsync(
            string endpointId, [FromBody][Required] MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await _nodes.GetMethodMetadataAsync(
                endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Call a method
        /// </summary>
        /// <remarks>
        /// Invoke method node with specified input arguments.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The method call request</param>
        /// <returns>The method call response</returns>
        [HttpPost("{endpointId}")]
        public async Task<MethodCallResponseModel> CallMethodAsync(
            string endpointId, [FromBody][Required] MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: Permissions

            return await _nodes.MethodCallAsync(endpointId, request).ConfigureAwait(false);
        }

        private readonly INodeServices<string> _nodes;
    }
}
