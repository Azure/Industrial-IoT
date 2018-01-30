// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Controllers {
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Auth;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Filters;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Call controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.ControlOpcServer)]
    public class CallController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="nodeServices"></param>
        /// <param name="endpointServices"></param>
        public CallController(IOpcUaNodeServices nodeServices,
            IOpcUaEndpointServices endpointServices) {
            _nodes = nodeServices;
            _endpoints = endpointServices;
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

            var metadataresult = await _nodes.NodeMethodGetMetadataAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new MethodMetadataResponseApiModel(metadataresult);
        }

        /// <summary>
        /// Return method meta data as specified in the method metadata request 
        /// on the server specified by the endpoint id.
        /// </summary>
        /// <param name="id">The (twin) identifier of the endpoint.</param>
        /// <param name="request">The method metadata request</param>
        /// <returns>The method metadata response</returns>
        [HttpPost("{id}/$metadata")]
        public async Task<MethodMetadataResponseApiModel> GetCallMetadataByIdAsync(string id,
            [FromBody] MethodMetadataRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var metadataresult = await _nodes.NodeMethodGetMetadataAsync(
                endpoint, request.ToServiceModel());
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

            var callresult = await _nodes.NodeMethodCallAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new MethodCallResponseApiModel(callresult);
        }

        /// <summary>
        /// Invoke method node as specified in the method call request on the
        /// server specified by the endpoint id.
        /// </summary>
        /// <param name="id">The (twin) identifier of the endpoint.</param>
        /// <param name="request">The method call request</param>
        /// <returns>The method call response</returns>
        [HttpPost("{id}")]
        public async Task<MethodCallResponseApiModel> CallByIdAsync(string id,
            [FromBody] MethodCallRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: Permissions

            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var callresult = await _nodes.NodeMethodCallAsync(
                endpoint, request.ToServiceModel());
            return new MethodCallResponseApiModel(callresult);
        }

        private readonly IOpcUaNodeServices _nodes;
        private readonly IOpcUaEndpointServices _endpoints;
    }
}
