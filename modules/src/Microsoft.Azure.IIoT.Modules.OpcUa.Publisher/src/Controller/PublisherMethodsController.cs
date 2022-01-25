// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Net;

    /// <summary>
    /// Publisher direct  method controller
    /// </summary>
    [Version(1)]
    [ExceptionsFilter]
    public class PublisherMethodsController : IMethodController {

        /// <summary>
        /// ctor
        /// </summary>
        public PublisherMethodsController(IPublisherConfigServices configServices) {
            _configServices = configServices ?? throw new ArgumentNullException(nameof(configServices));
        }

        /// <summary>
        /// Handler for PublishNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> PublishNodesAsync(
            PublishNodesEndpointApiModel request) {
            var response = await _configServices.PublishNodesAsync(request.ToServiceModel()).ConfigureAwait(false);
            return response.ToApiModel();
        }

        /// <summary>
        /// Handler for UnpublishNodes DM
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> UnpublishNodesAsync(
            PublishNodesEndpointApiModel request) {
            var response = await _configServices.UnpublishNodesAsync(request.ToServiceModel()).ConfigureAwait(false);
            return response.ToApiModel();
        }

        /// <summary>
        /// Handler for UnpublishAllNodes DM
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> UnpublishAllNodesAsync(
            PublishNodesEndpointApiModel request) {

            if (request.OpcNodes != null) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, "OpcNodes is set.");
            }

            var response = await _configServices.UnpublishAllNodesAsync(request.ToServiceModel()).ConfigureAwait(false);
            return response.ToApiModel();
        }

        /// <summary>
        /// Handler for GetConfiguredEndpoints DM
        /// </summary>
        public async Task<List<PublishNodesEndpointApiModel>> GetConfiguredEndpointsAsync() {

            var response = await _configServices.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            return response.ToApiModel();
        }

        /// <summary>
        /// Handler for GetConfiguredNodesOnEndpoint DM
        /// </summary>
        public async Task GetConfiguredNodesOnEndpointAsync(PublishNodesEndpointApiModel request) {
            if (request.OpcNodes != null) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, "OpcNodes is set.");
            }
            await Task.Delay(0);
            throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
        }

        /// <summary>
        /// Handler for GetDiagnosticInfo DM
        /// </summary>
        public async Task GetDiagnosticInfoAsync(PublishNodesEndpointApiModel request) {
            await Task.Delay(0);
            throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
        }

        /// <summary>
        /// Handler for GetInfo DM
        /// </summary>
        public async Task GetInfoAsync() {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        /// <summary>
        /// Handler for GetDiagnosticLog DM - Discontinued
        /// </summary>
        public async Task GetDiagnosticLogAsync() {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        /// <summary>
        /// Handler for GetDiagnosticStartupLog DM - Discontinued
        /// </summary>
        public async Task GetDiagnosticStartupLogAsync() {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        /// <summary>
        /// Handler for ExitApplication DM - Discontinued
        /// </summary>
        public async Task ExitApplicationAsync() {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        private readonly IPublisherConfigServices _configServices;
    }
}
