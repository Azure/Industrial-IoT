// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Controllers {
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Auth;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Filters;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Read value controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.BrowseTwins)]
    public class ReadController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="adhoc"></param>
        public ReadController(IOpcUaTwinNodeServices twin, IOpcUaAdhocNodeServices adhoc) {
            _twin = twin;
            _adhoc = adhoc;
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

            var readresult = await _adhoc.NodeValueReadAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Read node value as specified in the read value request on the
        /// server specified by the twin id.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The read value request</param>
        /// <returns>The read value response</returns>
        [HttpPost("{id}")]
        public async Task<ValueReadResponseApiModel> ReadByIdAsync(string id,
            [FromBody] ValueReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _twin.NodeValueReadAsync(
                id, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        /// <summary>
        /// Get node value from the node id passed through query on the
        /// server specified by the twin id.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="nodeId">The node to read</param>
        /// <returns>The read value response</returns>
        [HttpGet("{id}")]
        public async Task<ValueReadResponseApiModel> ReadByIdAsGetAsync(string id,
            [FromQuery] string nodeId) {
            if (string.IsNullOrEmpty(nodeId)) {
                throw new ArgumentNullException(nameof(nodeId));
            }
            var request = new ValueReadRequestApiModel { NodeId = nodeId };
            var readresult = await _twin.NodeValueReadAsync(
                id, request.ToServiceModel());
            return new ValueReadResponseApiModel(readresult);
        }

        private readonly IOpcUaTwinNodeServices _twin;
        private readonly IOpcUaAdhocNodeServices _adhoc;
    }
}
