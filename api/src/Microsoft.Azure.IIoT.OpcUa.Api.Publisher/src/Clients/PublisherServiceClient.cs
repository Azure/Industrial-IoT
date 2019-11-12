// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Implementation of twin service api.
    /// </summary>
    public sealed class PublisherServiceClient : IPublisherServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        public PublisherServiceClient(IHttpClient httpClient, IPublisherConfig config) :
            this(httpClient, config.OpcUaPublisherServiceUrl, config.OpcUaPublisherServiceResourceId) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        public PublisherServiceClient(IHttpClient httpClient, string serviceUri, string resourceId) {
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
        public async Task NodePublishSubscribeByEndpointAsync(string endpointId, string userId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/monitor/{endpointId}/samples", _resourceId);
            request.SetContent<string>(userId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task NodePublishUnsubscribeByEndpointAsync(string endpointId, string userId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/monitor/{endpointId}/samples/{userId}", _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly string _resourceId;
    }
}
