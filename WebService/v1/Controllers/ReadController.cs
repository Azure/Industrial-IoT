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
    /// Read value controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.BrowseOpcServer)]
    public class ReadController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="nodeServices"></param>
        /// <param name="endpointServices"></param>
        public ReadController(IOpcUaNodeServices nodeServices,
            IOpcUaEndpointServices endpointServices) {
            _nodes = nodeServices;
            _endpoints = endpointServices;
        }

        /// <summary>
        /// Read node value as specified in the read value request on the
        /// server specified in the endpoint object of the service request.
        /// </summary>
        /// <param name="request">The service request</param>
        /// <returns>The read value response</returns>
        [HttpPost]
        public async Task<ValueReadResponseApiModel> ReadByEndpointAsync(
            [FromBody] ServiceRequestApiModel<ValueReadRequestApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: if token type is not "none", but user/token not, take from current claims

            var readresult = await _nodes.NodeValueReadAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Read node value as specified in the read value request on the
        /// server specified by the endpoint id.
        /// </summary>
        /// <param name="id">The (twin) identifier of the endpoint.</param>
        /// <param name="request">The read value request</param>
        /// <returns>The read value response</returns>
        [HttpPost("{id}")]
        public async Task<ValueReadResponseApiModel> ReadByIdAsync(string id,
            [FromBody] ValueReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var readresult = await _nodes.NodeValueReadAsync(
                endpoint, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Get node value from the node id passed through query on the 
        /// server specified by the endpoint id.
        /// </summary>
        /// <param name="id">The (twin) identifier of the endpoint.</param>
        /// <param name="nodeId">The node to read</param>
        /// <returns>The read value response</returns>
        [HttpGet("{id}")]
        public async Task<ValueReadResponseApiModel> ReadByIdAsGetAsync(string id,
            [FromQuery] string nodeId) {
            if (string.IsNullOrEmpty(nodeId)) {
                throw new ArgumentNullException(nameof(nodeId));
            }
            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var request = new ValueReadRequestApiModel { NodeId = nodeId };
            var readresult = await _nodes.NodeValueReadAsync(
                endpoint, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        private readonly IOpcUaNodeServices _nodes;
        private readonly IOpcUaEndpointServices _endpoints;
    }
}
