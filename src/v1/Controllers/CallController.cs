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
    /// Call controller
    /// </summary>
    [Route(VersionInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policies.CanControl)]
    public class CallController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        public CallController(INodeServices<string> twin) {
            _twin = twin;
        }

        /// <summary>
        /// Return method meta data as specified in the method metadata request
        /// on the server specified by the twin id.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The method metadata request</param>
        /// <returns>The method metadata response</returns>
        [HttpPost("{id}/$metadata")]
        public async Task<MethodMetadataResponseApiModel> GetCallMetadataByIdAsync(string id,
            [FromBody] MethodMetadataRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var metadataresult = await _twin.NodeMethodGetMetadataAsync(
                id, request.ToServiceModel());
            return new MethodMetadataResponseApiModel(metadataresult);
        }

        /// <summary>
        /// Invoke method node as specified in the method call request on the
        /// server specified by the twin id.
        /// The twin must be activated and connected and twin and server must trust
        /// each other.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The method call request</param>
        /// <returns>The method call response</returns>
        [HttpPost("{id}")]
        public async Task<MethodCallResponseApiModel> CallByIdAsync(string id,
            [FromBody] MethodCallRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: Permissions

            var callresult = await _twin.NodeMethodCallAsync(
                id, request.ToServiceModel());
            return new MethodCallResponseApiModel(callresult);
        }

        private readonly INodeServices<string> _twin;
    }
}
