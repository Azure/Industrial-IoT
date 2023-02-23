// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Sdk.Clients {
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Abstractions.Serializers.Extensions;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin service api.
    /// </summary>
    public sealed class TwinServiceClient : ITwinServiceApi {
        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public TwinServiceClient(IHttpClient httpClient, IServiceApiConfig config,
            ISerializer serializer) :
            this(httpClient, config?.ServiceUrl, serializer) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public TwinServiceClient(IHttpClient httpClient, string serviceUri,
            ISerializer serializer = null) {
            if (string.IsNullOrWhiteSpace(serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serviceUri = serviceUri.TrimEnd('/');
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/healthz",
                Resource.Platform);
            try {
                var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
                response.Validate();
                return response.GetContentAsString();
            }
            catch (Exception ex) {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> NodeBrowseFirstAsync(string endpointId,
            BrowseFirstRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/twin/v2/browse/{endpointId}",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
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
            var request = _httpClient.NewRequest($"{_serviceUri}/twin/v2/browse/{endpointId}/next",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<BrowseNextResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.BrowsePaths == null || content.BrowsePaths.Count == 0 ||
                content.BrowsePaths.Any(p => p == null || p.Count == 0)) {
                throw new ArgumentNullException(nameof(content.BrowsePaths));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/twin/v2/browse/{endpointId}/path",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<BrowsePathResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> NodeReadAsync(string endpointId,
            ReadRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Attributes == null || content.Attributes.Count == 0) {
                throw new ArgumentException(nameof(content.Attributes));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/twin/v2/read/{endpointId}/attributes", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ReadResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> NodeWriteAsync(string endpointId,
            WriteRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Attributes == null || content.Attributes.Count == 0) {
                throw new ArgumentException(nameof(content.Attributes));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/twin/v2/write/{endpointId}/attributes", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<WriteResponseModel>(response);
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
            var request = _httpClient.NewRequest($"{_serviceUri}/twin/v2/read/{endpointId}",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ValueReadResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Value is null) {
                throw new ArgumentNullException(nameof(content.Value));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/twin/v2/write/{endpointId}",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ValueWriteResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/twin/v2/call/{endpointId}/metadata",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<MethodMetadataResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/twin/v2/call/{endpointId}",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<MethodCallResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(string endpointId,
            NodeMetadataRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/twin/v2/metadata/{endpointId}",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<NodeMetadataResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(string endpointId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/applications/v2/capabilities/{endpointId}",
                Resource.Platform);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ServerCapabilitiesModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(string endpointId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/applications/v2/capabilities/{endpointId}/history",
                Resource.Platform);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryServerCapabilitiesModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(string endpointId,
            HistoryConfigurationRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.NodeId == null) {
                throw new ArgumentNullException(nameof(content.NodeId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/history/read/{endpointId}/config", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryConfigurationResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            string endpointId, HistoryReadRequestModel<VariantValue> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Details == null) {
                throw new ArgumentNullException(nameof(content.Details));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/history/read/{endpointId}", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            string endpointId, HistoryReadNextRequestModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.ContinuationToken)) {
                throw new ArgumentNullException(nameof(content.ContinuationToken));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/history/read/{endpointId}/next", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadNextResponseModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            string endpointId, HistoryUpdateRequestModel<VariantValue> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Details == null) {
                throw new ArgumentNullException(nameof(content.Details));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/history/update/{endpointId}", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        private readonly IHttpClient _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
