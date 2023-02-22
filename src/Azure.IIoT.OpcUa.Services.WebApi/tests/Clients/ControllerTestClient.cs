// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests.Clients {
    using Azure.IIoT.OpcUa.Services.Sdk;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin service api with extra controller methods.
    /// </summary>
    public sealed class ControllerTestClient : ITwinServiceApi {

        /// <summary>
        /// Create test client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        public ControllerTestClient(IHttpClient httpClient, IServiceApiConfig config,
            ISerializer serializer) {
            _serviceUri = (config?.ServiceUrl ??
                throw new ArgumentNullException(nameof(config))).TrimEnd('/') + "/twin";
            _httpClient = httpClient ??
                throw new ArgumentNullException(nameof(httpClient));
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> NodeBrowseFirstAsync(string endpointId,
            BrowseFirstRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var path = new UriBuilder($"{_serviceUri}/v2/browse/{endpointId}");
            if (!string.IsNullOrEmpty(content.NodeId)) {
                path.Query = $"nodeId={content.NodeId.UrlEncode()}";
            }
            var request = _httpClient.NewRequest(path.ToString());
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<BrowseFirstResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.ContinuationToken == null) {
                throw new ArgumentNullException(nameof(content.ContinuationToken));
            }
            var path = new UriBuilder($"{_serviceUri}/v2/browse/{endpointId}/next") {
                Query = $"continuationToken={content.ContinuationToken}"
            };
            var request = _httpClient.NewRequest(path.ToString());
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<BrowseNextResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.NodeId)) {
                throw new ArgumentNullException(nameof(content.NodeId));
            }
            var path = new UriBuilder($"{_serviceUri}/v2/read/{endpointId}") {
                Query = $"nodeId={content.NodeId.UrlEncode()}"
            };
            var request = _httpClient.NewRequest(path.ToString());
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ValueReadResponseModel>(response);
        }

        /// <inheritdoc/>
        public Task<ReadResponseModel> NodeReadAsync(string endpointId,
            ReadRequestModel content, CancellationToken ct) {
            return Task.FromException<ReadResponseModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<WriteResponseModel> NodeWriteAsync(string endpointId,
            WriteRequestModel content, CancellationToken ct) {
            return Task.FromException<WriteResponseModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<ValueWriteResponseModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestModel content, CancellationToken ct) {
            return Task.FromException<ValueWriteResponseModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestModel content, CancellationToken ct) {
            return Task.FromException<MethodMetadataResponseModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<MethodCallResponseModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestModel content, CancellationToken ct) {
            return Task.FromException<MethodCallResponseModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<string> GetServiceStatusAsync(CancellationToken ct) {
            return Task.FromException<string>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<BrowsePathResponseModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestModel content, CancellationToken ct) {
            return Task.FromException<BrowsePathResponseModel>(new NotImplementedException());
        }

        public Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(string endpointId,
            CancellationToken ct) {
            return Task.FromException<ServerCapabilitiesModel>(new NotImplementedException());
        }

        public Task<NodeMetadataResponseModel> GetMetadataAsync(string endpointId,
            NodeMetadataRequestModel request, CancellationToken ct) {
            return Task.FromException<NodeMetadataResponseModel>(new NotImplementedException());
        }

        public Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(string endpointId,
            CancellationToken ct) {
            return Task.FromException<HistoryServerCapabilitiesModel>(new NotImplementedException());
        }

        public Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(string endpointId,
            HistoryConfigurationRequestModel request, CancellationToken ct) {
            return Task.FromException<HistoryConfigurationResponseModel>(new NotImplementedException());
        }

        public Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(string endpointId,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct) {
            return Task.FromException<HistoryReadResponseModel<VariantValue>>(new NotImplementedException());
        }

        public Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(string endpointId,
            HistoryReadNextRequestModel request, CancellationToken ct) {
            return Task.FromException<HistoryReadNextResponseModel<VariantValue>>(new NotImplementedException());
        }

        public Task<HistoryUpdateResponseModel> HistoryUpdateAsync(string endpointId,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct) {
            return Task.FromException<HistoryUpdateResponseModel>(new NotImplementedException());
        }

        private readonly IHttpClient _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
