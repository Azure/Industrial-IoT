// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Auth;
    using Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Filters;
    using Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Models;
    using Microsoft.Azure.IIoT.OpcTwin.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Nodes controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.ControlTwins)]
    public class WriteController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="adhoc"></param>
        public WriteController(IOpcUaTwinNodeServices twin, IOpcUaAdhocNodeServices adhoc) {
            _twin = twin;
            _adhoc = adhoc;
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

            var writeResult = await _adhoc.NodeValueWriteAsync(
                request.Endpoint.ToServiceModel(),
                request.Content.ToServiceModel());
            return new ValueWriteResponseApiModel(writeResult);
        }

        /// <summary>
        /// Write node value as specified in the write value request on the
        /// server specified by the twin id.
        /// </summary>
        /// <param name="id">The identifier of the twin.</param>
        /// <param name="request">The write value request</param>
        /// <returns>The write value response</returns>
        [HttpPost("{id}")]
        public async Task<ValueWriteResponseApiModel> WriteByIdAsync(string id,
            [FromBody] ValueWriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _twin.NodeValueWriteAsync(
                id, request.ToServiceModel());
            return new ValueWriteResponseApiModel(writeResult);
        }

        private readonly IOpcUaTwinNodeServices _twin;
        private readonly IOpcUaAdhocNodeServices _adhoc;
    }
}
