// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Controllers.Test {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Implementation of twin service api with extra controller methods.
    /// </summary>
    public sealed class ControllerTestClient : ITwinServiceApi {

        /// <summary>
        /// Create test client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        public ControllerTestClient(IHttpClient httpClient, ITwinConfig config) {
            _serviceUri = config?.OpcUaTwinServiceUrl ??
                throw new ArgumentNullException(nameof(config));
            _httpClient = httpClient ??
                throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseApiModel> NodeBrowseFirstAsync(string endpointId,
            BrowseRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var path = new UriBuilder($"{_serviceUri}/v2/browse/{endpointId}");
            if (!string.IsNullOrEmpty(content.NodeId)) {
                path.Query = $"nodeId={content.NodeId.UrlEncode()}";
            }
            var request = _httpClient.NewRequest(path.ToString());
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<BrowseResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseApiModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestApiModel content, CancellationToken ct) {
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
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<BrowseNextResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseApiModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestApiModel content, CancellationToken ct) {
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
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ValueReadResponseApiModel>();
        }

        /// <inheritdoc/>
        public Task<ReadResponseApiModel> NodeReadAsync(string endpointId,
            ReadRequestApiModel content, CancellationToken ct) {
            return Task.FromException<ReadResponseApiModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<WriteResponseApiModel> NodeWriteAsync(string endpointId,
            WriteRequestApiModel content, CancellationToken ct) {
            return Task.FromException<WriteResponseApiModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestApiModel content, CancellationToken ct) {
            return Task.FromException<ValueWriteResponseApiModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestApiModel content, CancellationToken ct) {
            return Task.FromException<MethodMetadataResponseApiModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestApiModel content, CancellationToken ct) {
            return Task.FromException<MethodCallResponseApiModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<StatusResponseApiModel> GetServiceStatusAsync(CancellationToken ct) {
            return Task.FromException<StatusResponseApiModel>(new NotImplementedException());
        }

        /// <inheritdoc/>
        public Task<BrowsePathResponseApiModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestApiModel content, CancellationToken ct) {
            return Task.FromException<BrowsePathResponseApiModel>(new NotImplementedException());
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
    }
}
