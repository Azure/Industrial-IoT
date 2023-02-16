// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Clients {
    using Microsoft.Azure.IIoT.Api;
    using Microsoft.Azure.IIoT.Api.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node and publish services through command control against
    /// the OPC Publihser module receiving service requests via device method calls.
    /// </summary>
    public sealed class PublisherApiClient : IPublisherModuleApi {

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="serializer"></param>
        public PublisherApiClient(IMethodClient methodClient, string deviceId,
            string moduleId = null, IJsonSerializer serializer = null) {
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _client = methodClient ?? throw new ArgumentNullException(nameof(methodClient));
            _moduleId = moduleId;
            _deviceId = deviceId;
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public PublisherApiClient(IMethodClient methodClient, IPublisherModuleConfig config = null,
            IJsonSerializer serializer = null) :
            this(methodClient, config?.DeviceId, config?.ModuleId, serializer) {
        }

        /// <summary>
        /// Handler for PublishNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseModel> PublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<PublishedNodesEntryModel, PublishedNodesResponseModel>(
                "PublishNodes", request);
            return result;
        }

        /// <summary>
        /// Handler for UnpublishNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseModel> UnpublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<PublishedNodesEntryModel, PublishedNodesResponseModel>(
                "UnpublishNodes", request);
            return result;
        }

        /// <summary>
        /// Handler for UnpublishAllNodes direct method
        /// </summary>
        public async Task<PublishedNodesResponseModel> UnpublishAllNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<PublishedNodesEntryModel, PublishedNodesResponseModel>(
                "UnpublishAllNodes", request);
            return result;
        }

        /// <summary>
        /// Handler for AddOrUpdateEndpoints direct method
        /// </summary>
        public async Task<PublishedNodesResponseModel> AddOrUpdateEndpointsAsync(
            List<PublishedNodesEntryModel> request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<List<PublishedNodesEntryModel>, PublishedNodesResponseModel>(
                "AddOrUpdateEndpoints", request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredEndpointsResponseModel> GetConfiguredEndpointsAsync(
            CancellationToken ct) {
            var result = await CallServiceOnPublisherAsync<object, GetConfiguredEndpointsResponseModel>(
                "GetConfiguredEndpoints", null);
            return result;
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredNodesOnEndpointResponseModel> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnPublisherAsync<PublishedNodesEntryModel, GetConfiguredNodesOnEndpointResponseModel>(
                "GetConfiguredNodesOnEndpoint", request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(
            CancellationToken ct) {
            var result = await CallServiceOnPublisherAsync<object, List<PublishDiagnosticInfoModel>>(
                "GetDiagnosticInfo", null);
            return result;
        }

        /// <inheritdoc/>
        public async Task<byte[]> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            var result = await CallServiceOnPublisherAsync<EndpointModel, byte[]>(
                "GetEndpointCertificate", endpoint);
            return result;
        }

        /// <summary>
        /// helper to invoke service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<R> CallServiceOnPublisherAsync<T, R>(string service,
            T request) {
            var result = await _client.CallMethodAsync(_deviceId, _moduleId, service,
                _serializer.SerializeToString(request));
            return _serializer.Deserialize<R>(result);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _client;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
