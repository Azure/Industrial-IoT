// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Implementation of twin service api.
    /// </summary>
    public sealed class TwinServiceClient : ITwinServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        public TwinServiceClient(IHttpClient httpClient, ITwinConfig config) :
            this(httpClient, config.OpcUaTwinServiceUrl, config.OpcUaTwinServiceResourceId) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        public TwinServiceClient(IHttpClient httpClient, string serviceUri, string resourceId) {
            _serviceUri = serviceUri ?? throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _resourceId = resourceId;
        }

        /// <inheritdoc/>
        public async Task<StatusResponseApiModel> GetServiceStatusAsync(CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/status",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<StatusResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseApiModel> NodeBrowseFirstAsync(string endpointId,
            BrowseRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/browse/{endpointId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
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
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/browse/{endpointId}/next",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<BrowseNextResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseApiModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.BrowsePaths == null || content.BrowsePaths.Count == 0 ||
                content.BrowsePaths.Any(p => p == null || p.Length == 0)) {
                throw new ArgumentNullException(nameof(content.BrowsePaths));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/browse/{endpointId}/path",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<BrowsePathResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseApiModel> NodePublishStartAsync(string endpointId,
            PublishStartRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Item == null) {
                throw new ArgumentNullException(nameof(content.Item));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/publish/{endpointId}/start",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<PublishStartResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseApiModel> NodePublishListAsync(
            string endpointId, PublishedItemListRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/publish/{endpointId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<PublishedItemListResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseApiModel> NodePublishStopAsync(string endpointId,
            PublishStopRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/publish/{endpointId}/stop",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<PublishStopResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<ReadResponseApiModel> NodeReadAsync(string endpointId,
            ReadRequestApiModel content, CancellationToken ct) {
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
                $"{_serviceUri}/v2/read/{endpointId}/attributes", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ReadResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<WriteResponseApiModel> NodeWriteAsync(string endpointId,
            WriteRequestApiModel content, CancellationToken ct) {
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
                $"{_serviceUri}/v2/write/{endpointId}/attributes", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<WriteResponseApiModel>();
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
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/read/{endpointId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ValueReadResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Value == null) {
                throw new ArgumentNullException(nameof(content.Value));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/write/{endpointId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ValueWriteResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/call/{endpointId}/metadata",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<MethodMetadataResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/call/{endpointId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<MethodCallResponseApiModel>();
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly string _resourceId;
    }
}
