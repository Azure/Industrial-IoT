// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controller
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher direct  method controller
    /// </summary>
    [Version("_V2")]
    [Version("_V1")]
    [Version("")]
    [ExceptionsFilter]
    public class PublisherMethodsController : IMethodController
    {
        /// <summary>
        /// ctor
        /// </summary>
        public PublisherMethodsController(IPublisherConfigurationServices configServices)
        {
            _configServices = configServices ?? throw new ArgumentNullException(nameof(configServices));
        }

        /// <summary>
        /// Start publishing values from a node
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishStartResponseModel> PublishStartAsync(
            ConnectionModel connection, PublishStartRequestModel request)
        {
            return await _configServices.NodePublishStartAsync(connection,
                request).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop publishing values from a node
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishStopResponseModel> PublishStopAsync(
            ConnectionModel connection, PublishStopRequestModel request)
        {
            return await _configServices.NodePublishStopAsync(connection,
                request).ConfigureAwait(false);
        }

        /// <summary>
        /// Configure node values to publish and unpublish in bulk
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishBulkResponseModel> PublishBulkAsync(
            ConnectionModel connection, PublishBulkRequestModel request)
        {
            return await _configServices.NodePublishBulkAsync(connection,
                request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all published nodes for a server endpoint.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishedItemListResponseModel> PublishListAsync(
            ConnectionModel connection,
            PublishedItemListRequestModel request)
        {
            return await _configServices.NodePublishListAsync(connection,
                request).ConfigureAwait(false);
        }

        /// <summary>
        /// Handler for PublishNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseModel> PublishNodesAsync(
            PublishedNodesEntryModel request)
        {
            await _configServices.PublishNodesAsync(request).ConfigureAwait(false);
            return new PublishedNodesResponseModel();
        }

        /// <summary>
        /// Handler for UnpublishNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseModel> UnpublishNodesAsync(
            PublishedNodesEntryModel request)
        {
            await _configServices.UnpublishNodesAsync(request).ConfigureAwait(false);
            return new PublishedNodesResponseModel();
        }

        /// <summary>
        /// Handler for UnpublishAllNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseModel> UnpublishAllNodesAsync(
            PublishedNodesEntryModel request)
        {
            await _configServices.UnpublishAllNodesAsync(request).ConfigureAwait(false);
            return new PublishedNodesResponseModel();
        }

        /// <summary>
        /// Handler for AddOrUpdateEndpoints direct method
        /// </summary>
        public async Task<PublishedNodesResponseModel> AddOrUpdateEndpointsAsync(
            List<PublishedNodesEntryModel> request)
        {
            var endpoints = request?.Select(e => e).ToList();
            await _configServices.AddOrUpdateEndpointsAsync(endpoints).ConfigureAwait(false);
            return new PublishedNodesResponseModel();
        }

        /// <summary>
        /// Handler for GetConfiguredEndpoints direct method
        /// </summary>
        public async Task<GetConfiguredEndpointsResponseModel> GetConfiguredEndpointsAsync()
        {
            var response = await _configServices.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            return new GetConfiguredEndpointsResponseModel
            {
                Endpoints = response,
            };
        }

        /// <summary>
        /// Handler for GetConfiguredNodesOnEndpoint direct method
        /// </summary>
        public async Task<GetConfiguredNodesOnEndpointResponseModel> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request)
        {
            var response = await _configServices.GetConfiguredNodesOnEndpointAsync(
                request).ConfigureAwait(false);
            return new GetConfiguredNodesOnEndpointResponseModel
            {
                OpcNodes = response,
            };
        }

        /// <summary>
        /// Handler for GetDiagnosticInfo direct method
        /// </summary>
        public async Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync()
        {
            return await _configServices.GetDiagnosticInfoAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Handler for GetInfo direct method
        /// </summary>
        public async Task GetInfoAsync()
        {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        /// <summary>
        /// Handler for GetDiagnosticLog direct method - Discontinued
        /// </summary>
        public async Task GetDiagnosticLogAsync()
        {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        /// <summary>
        /// Handler for GetDiagnosticStartupLog direct method - Discontinued
        /// </summary>
        public async Task GetDiagnosticStartupLogAsync()
        {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        /// <summary>
        /// Handler for ExitApplication direct method - Discontinued
        /// </summary>
        public async Task ExitApplicationAsync()
        {
            await Task.Delay(0).ConfigureAwait(false);
            throw new MethodCallStatusException((int)HttpStatusCode.NotFound, "Discontinued");
        }

        private readonly IPublisherConfigurationServices _configServices;
    }
}
