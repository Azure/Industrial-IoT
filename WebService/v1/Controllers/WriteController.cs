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
    /// Nodes controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.ControlOpcServer)]
    public class WriteController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="nodeServices"></param>
        /// <param name="endpointServices"></param>
        public WriteController(IOpcUaNodeServices nodeServices,
            IOpcUaEndpointServices endpointServices) {
            _nodes = nodeServices;
            _endpoints = endpointServices;
        }

        /// <summary>
        /// Write node value as specified in the write value request on the
        /// server specified in the endpoint object of the service request.
        /// </summary>
        /// <param name="request">The service request</param>
        /// <returns>The write value response</returns>
        [HttpPost]
        public async Task<ValueWriteResponseApiModel> WriteByEndpointAsync(
            [FromBody] ServiceRequestApiModel<ValueWriteRequestApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: if token type is not "none", but user/token not, take from current claims

            var writeResult = await _nodes.NodeValueWriteAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new ValueWriteResponseApiModel(writeResult);
        }

        /// <summary>
        /// Write node value as specified in the write value request on the
        /// server specified by the endpoint id.
        /// </summary>
        /// <param name="id">The (twin) identifier of the endpoint.</param>
        /// <param name="request">The write value request</param>
        /// <returns>The write value response</returns>
        [HttpPost("{id}")]
        public async Task<ValueWriteResponseApiModel> WriteByIdAsync(string id,
            [FromBody] ValueWriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var writeResult = await _nodes.NodeValueWriteAsync(
                endpoint, request.ToServiceModel());
            return new ValueWriteResponseApiModel(writeResult);
        }

        private readonly IOpcUaNodeServices _nodes;
        private readonly IOpcUaEndpointServices _endpoints;
    }
}
