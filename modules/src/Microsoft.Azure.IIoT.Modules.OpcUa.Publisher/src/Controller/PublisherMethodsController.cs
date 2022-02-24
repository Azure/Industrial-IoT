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
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher direct  method controller
    /// </summary>
    [Version("_V1")]
    [Version("")]
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
            await _configServices.PublishNodesAsync(request.ToServiceModel()).ConfigureAwait(false);
            return new PublishedNodesResponseApiModel();
        }

        /// <summary>
        /// Handler for UnpublishNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> UnpublishNodesAsync(
            PublishNodesEndpointApiModel request) {
            await _configServices.UnpublishNodesAsync(request.ToServiceModel()).ConfigureAwait(false);
            return new PublishedNodesResponseApiModel();
        }

        /// <summary>
        /// Handler for UnpublishAllNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> UnpublishAllNodesAsync(
            PublishNodesEndpointApiModel request) {
            if (request.OpcNodes != null && request.OpcNodes.Count > 0) {
                throw new MethodCallStatusException((int)HttpStatusCode.BadRequest, "OpcNodes is set.");
            }
            await _configServices.UnpublishAllNodesAsync(request.ToServiceModel()).ConfigureAwait(false);
            return new PublishedNodesResponseApiModel();
        }

        /// <summary>
        /// Handler for AddOrUpdateEndpoints direct method
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> AddOrUpdateEndpointsAsync(
            List<PublishNodesEndpointApiModel> request) {
            var endpoints = request?.Select(e => e.ToServiceModel()).ToList();
            await _configServices.AddOrUpdateEndpointsAsync(endpoints).ConfigureAwait(false);
            return new PublishedNodesResponseApiModel();
        }

        /// <summary>
        /// Handler for GetConfiguredEndpoints direct method
        /// </summary>
        public async Task<GetConfiguredEndpointsResponseApiModel> GetConfiguredEndpointsAsync() {
            var response = await _configServices.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            return new GetConfiguredEndpointsResponseApiModel() {
                Endpoints = response.ToApiModel(),
            };
        }

        /// <summary>
        /// Handler for GetConfiguredNodesOnEndpoint direct method
        /// </summary>
        public async Task<GetConfiguredNodesOnEndpointResponseApiModel> GetConfiguredNodesOnEndpointAsync(PublishNodesEndpointApiModel request) {
            var response = await _configServices.GetConfiguredNodesOnEndpointAsync(request.ToServiceModel()).ConfigureAwait(false);
            return new GetConfiguredNodesOnEndpointResponseApiModel() {
                OpcNodes = response.ToApiModel(),
            };
        }

        /// <summary>
        /// Handler for GetDiagnosticInfo direct method
        /// </summary>
        public async Task<List<DiagnosticInfoApiModel>> GetDiagnosticInfoAsync() {
            var response = await _configServices.GetDiagnosticInfoAsync().ConfigureAwait(false);
            return response.ToApiModel();
        }

        /// <summary>
        /// Handler for GetInfo direct method
        /// </summary>
        public async Task GetInfoAsync() {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        /// <summary>
        /// Handler for GetDiagnosticLog direct method - Discontinued
        /// </summary>
        public async Task GetDiagnosticLogAsync() {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        /// <summary>
        /// Handler for GetDiagnosticStartupLog direct method - Discontinued
        /// </summary>
        public async Task GetDiagnosticStartupLogAsync() {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        /// <summary>
        /// Handler for ExitApplication direct method - Discontinued
        /// </summary>
        public async Task ExitApplicationAsync() {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        private readonly IPublisherConfigServices _configServices;
    }
}
