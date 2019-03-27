// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Implementation of twin service api.
    /// </summary>
    public sealed class TwinServiceClient : ITwinServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public TwinServiceClient(IHttpClient httpClient, ITwinConfig config,
            ILogger logger) :
            this(httpClient, config.OpcUaTwinServiceUrl,
                config.OpcUaTwinServiceResourceId, logger) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        /// <param name="logger"></param>
        public TwinServiceClient(IHttpClient httpClient, string serviceUri,
            string resourceId, ILogger logger) {
            if (serviceUri == null) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serviceUri = serviceUri;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceId = resourceId;
        }

        /// <inheritdoc/>
        public async Task<StatusResponseApiModel> GetServiceStatusAsync() {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/status",
                _resourceId);
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<StatusResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseApiModel> NodeBrowseAsync(string endpointId,
            BrowseRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/browse/{endpointId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<BrowseResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseApiModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestApiModel content) {
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
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<BrowseNextResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseApiModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestApiModel content) {
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
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<BrowsePathResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseApiModel> NodePublishStartAsync(string endpointId,
            PublishStartRequestApiModel content) {
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
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<PublishStartResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseApiModel> NodePublishListAsync(
            string endpointId, PublishedItemListRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/publish/{endpointId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<PublishedItemListResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseApiModel> NodePublishStopAsync(string endpointId,
            PublishStopRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/publish/{endpointId}/stop",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<PublishStopResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<ReadResponseApiModel> NodeReadAsync(string endpointId,
            ReadRequestApiModel content) {
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
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ReadResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<WriteResponseApiModel> NodeWriteAsync(string endpointId,
            WriteRequestApiModel content) {
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
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<WriteResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseApiModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/read/{endpointId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ValueReadResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestApiModel content) {
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
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ValueWriteResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/call/{endpointId}/metadata",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<MethodMetadataResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/call/{endpointId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<MethodCallResponseApiModel>();
        }

        private const string kContinuationTokenHeaderKey = "x-ms-continuation";
        private const string kPageSizeHeaderKey = "x-ms-max-item-count";
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _serviceUri;
        private readonly string _resourceId;
    }
}
