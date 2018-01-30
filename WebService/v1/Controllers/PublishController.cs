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
    [Authorize(Policy = Policy.ControlOpcServer)]
    public class PublishController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="publishServices"></param>
        /// <param name="endpointServices"></param>
        public PublishController(IOpcUaPublishServices publishServices,
            IOpcUaEndpointServices endpointServices) {
            _publish = publishServices;
            _endpoints = endpointServices;
        }

        /// <summary>
        /// Publish node value as specified in the publish value request on the
        /// server specified in the endpoint object of the service request.
        /// </summary>
        /// <param name="request">The service request</param>
        /// <returns>The publish response</returns>
        [HttpPost]
        public async Task<PublishResponseApiModel> PublishAsync(
            [FromBody] ServiceRequestApiModel<PublishRequestApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: if token type is not "none", but user/token not, take from current claims

            var publishresult = await _publish.NodePublishAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new PublishResponseApiModel(publishresult);
        }

        /// <summary>
        /// Publish node value as specified in the publish value request on the
        /// server specified by the endpoint id.
        /// </summary>
        /// <param name="id">The (twin) identifier of the endpoint.</param>
        /// <param name="request">The publish request</param>
        /// <returns>The publish response</returns>
        [HttpPost("{id}")]
        public async Task<PublishResponseApiModel> PublishAsync(string id,
            [FromBody] PublishRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetAsync(id);
            if (endpoint == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            var publishresult = await _publish.NodePublishAsync(
                endpoint, request.ToServiceModel());
            return new PublishResponseApiModel(publishresult);
        }

        private readonly IOpcUaPublishServices _publish;
        private readonly IOpcUaEndpointServices _endpoints;
    }
}
