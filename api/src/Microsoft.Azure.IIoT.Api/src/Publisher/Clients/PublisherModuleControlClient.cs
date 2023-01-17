// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node and publish services through command control against
    /// the OPC Publihser module receiving service requests via device method calls.
    /// </summary>
    public sealed class PublisherModuleControlClient : IPublisherControlApi {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public PublisherModuleControlClient(IMethodClient client, IJsonSerializer serializer,
            ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handler for PublishNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> PublishNodesAsync(
            string deviceId, string moduleId, PublishNodesEndpointApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<PublishNodesEndpointApiModel, PublishedNodesResponseApiModel>(
                "PublishNodes", deviceId, moduleId, request);
            return result;
        }

        /// <summary>
        /// Handler for UnpublishNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> UnpublishNodesAsync(
            string deviceId, string moduleId, PublishNodesEndpointApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<PublishNodesEndpointApiModel, PublishedNodesResponseApiModel>(
                "UnpublishNodes", deviceId, moduleId, request);
            return result;
        }

        /// <summary>
        /// Handler for UnpublishAllNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> UnpublishAllNodesAsync(
            string deviceId, string moduleId, PublishNodesEndpointApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<PublishNodesEndpointApiModel, PublishedNodesResponseApiModel>(
                "UnpublishAllNodes", deviceId, moduleId, request);
            return result;
        }

        /// <summary>
        /// Handler for AddOrUpdateEndpoints direct method
        /// </summary>
        public async Task<PublishedNodesResponseApiModel> AddOrUpdateEndpointsAsync(
            string deviceId, string moduleId, List<PublishNodesEndpointApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<List<PublishNodesEndpointApiModel>, PublishedNodesResponseApiModel>(
                "AddOrUpdateEndpoints", deviceId, moduleId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredEndpointsResponseApiModel> GetConfiguredEndpointsAsync(
            string deviceId, string moduleId) {
            var result = await CallServiceOnPublisherAsync<object, GetConfiguredEndpointsResponseApiModel>(
                "GetConfiguredEndpoints", deviceId, moduleId, null);
            return result;
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredNodesOnEndpointResponseApiModel> GetConfiguredNodesOnEndpointAsync(
            string deviceId, string moduleId, PublishNodesEndpointApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<PublishNodesEndpointApiModel, GetConfiguredNodesOnEndpointResponseApiModel>(
                "GetConfiguredNodesOnEndpoint", deviceId, moduleId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<List<DiagnosticInfoApiModel>> GetDiagnosticInfoAsync(string deviceId, string moduleId) {
            var result = await CallServiceOnPublisherAsync<object, List<DiagnosticInfoApiModel>>(
                "GetDiagnosticInfo", deviceId, moduleId, null);
            return result;
        }

        /// <summary>
        /// helper to invoke service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<R> CallServiceOnPublisherAsync<T, R>(string service,
            string deviceId, string moduleId, T request) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            var sw = Stopwatch.StartNew();
            var result = await _client.CallMethodAsync(deviceId, moduleId, service,
                _serializer.SerializeToString(request));
            _logger.Debug("Publisher call '{service}' took {elapsed} ms)!",
                service, sw.ElapsedMilliseconds);
            return _serializer.Deserialize<R>(result);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
