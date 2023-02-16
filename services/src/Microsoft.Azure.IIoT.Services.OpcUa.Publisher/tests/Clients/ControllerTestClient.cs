// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Tests.Clients {
    using Microsoft.Azure.IIoT.Api;
    using Microsoft.Azure.IIoT.Api.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Serializers;
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
        public ControllerTestClient(IHttpClient httpClient, IPublisherConfig config,
            ISerializer serializer) {
            _serviceUri = (config?.OpcUaPublisherServiceUrl ??
                throw new ArgumentNullException(nameof(config))).TrimEnd('/') + "/twin";
            _httpClient = httpClient ??
                throw new ArgumentNullException(nameof(httpClient));
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseModel> NodeBrowseFirstAsync(string endpointId,
            BrowseRequestModel content, CancellationToken ct) {
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
            return _serializer.DeserializeResponse<BrowseResponseModel>(response);
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

        private readonly IHttpClient _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
