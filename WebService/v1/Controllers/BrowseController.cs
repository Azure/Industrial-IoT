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
    /// Browse controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.BrowseOpcServer)]
    public class BrowseController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browseServices"></param>
        /// <param name="endpointServices"></param>
        public BrowseController(IOpcUaBrowseServices browseServices,
            IOpcUaEndpointServices endpointServices) {
            _browse = browseServices;
            _endpoints = endpointServices;
        }

        /// <summary>
        /// Browse a node on the endpoint as specified in the service request
        /// using the service request's browse configuration.
        /// </summary>
        /// <param name="request">The service request</param>
        /// <returns>The browse response</returns>
        [HttpPost]
        public async Task<BrowseResponseApiModel> BrowseByEndpointAsync(
            [FromBody] ServiceRequestApiModel<BrowseRequestApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: if token type is not "none", but user/token not, take from current claims

            var browseresult = await _browse.NodeBrowseAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new BrowseResponseApiModel(browseresult);
        }

        /// <summary>
        /// Browse a node on the endpoint specified by the passed in id
        /// using the specified browse configuration.
        /// </summary>
        /// <param name="id">The (twin) identifier of the endpoint.</param>
        /// <param name="request">The browse request</param>
        /// <returns>The browse response</returns>
        [HttpPost("{id}")]
        public async Task<BrowseResponseApiModel> BrowseByIdAsync(string id,
            [FromBody] BrowseRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var browseresult = await _browse.NodeBrowseAsync(
                endpoint, request.ToServiceModel());
            return new BrowseResponseApiModel(browseresult);
        }

        /// <summary>
        /// Browse node by node id on the endpoint specified by the passed in id.
        /// </summary>
        /// <param name="id">The (twin) identifier of the endpoint.</param>
        /// <param name="nodeId">The node to browse or omit to browse object root</param>
        /// <returns>The browse response</returns>
        [HttpGet("{id}")]
        public async Task<BrowseResponseApiModel> BrwoseByIdAsGetAsync(string id,
            [FromQuery] string nodeId) {
            if (string.IsNullOrEmpty(nodeId)) {
                nodeId = null;
            }
            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var request = new BrowseRequestApiModel { NodeId = nodeId };
            var browseresult = await _browse.NodeBrowseAsync(
                endpoint, request.ToServiceModel());
            return new BrowseResponseApiModel(browseresult);
        }

        private readonly IOpcUaBrowseServices _browse;
        private readonly IOpcUaEndpointServices _endpoints;
    }
}
