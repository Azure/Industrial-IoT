// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Call controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.ControlTwins)]
    public class CallController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="adhoc"></param>
        public CallController(IOpcUaTwinNodeServices twin,
            IOpcUaNodeServices<EndpointModel> adhoc) {
            _twin = twin;
            _adhoc = adhoc;
        }

        /// <summary>
        /// Return method meta data as specified in the method metadata request
        /// on the server specified in the endpoint object of the service request.
        /// </summary>
        /// <param name="request">The service request</param>
        /// <returns>The method metadata response</returns>
        [HttpPost("$metadata")]
        public async Task<MethodMetadataResponseApiModel> GetCallMetadataEndpointAsync(
            [FromBody] ServiceRequestApiModel<MethodMetadataRequestApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: if token type is not "none", but user/token not, take from current claims

            var metadataresult = await _adhoc.NodeMethodGetMetadataAsync(
                request.Endpoint.ToServiceModel(), request.Content.ToServiceModel());
            return new MethodMetadataResponseApiModel(metadataresult);
        }

        /// <summary>
        /// Return method meta data as specified in the method metadata request
        /// on the server specified by the twin id.
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
        /// server specified in the endpoint object of the service request.
        /// </summary>
        /// <param name="request">The service request</param>
        /// <returns>The method call response</returns>
        [HttpPost]
        public async Task<MethodCallResponseApiModel> CallByEndpointAsync(
            [FromBody] ServiceRequestApiModel<MethodCallRequestApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: Permissions
            // TODO: if token type is not "none", but user/token not, take from current claims

            var callresult = await _adhoc.NodeMethodCallAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new MethodCallResponseApiModel(callresult);
        }

        /// <summary>
        /// Invoke method node as specified in the method call request on the
        /// server specified by the twin id.
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

        private readonly IOpcUaTwinNodeServices _twin;
        private readonly IOpcUaNodeServices<EndpointModel> _adhoc;
    }
}
