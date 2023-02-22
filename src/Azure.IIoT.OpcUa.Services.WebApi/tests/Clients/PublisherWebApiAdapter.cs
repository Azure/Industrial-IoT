// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter {
    using Azure.IIoT.OpcUa.Services.Sdk;
    using Azure.IIoT.OpcUa.Shared.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node and publish services as adapter on top of api.
    /// </summary>
    public sealed class PublisherWebApiAdapter : IPublishServices<string> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public PublisherWebApiAdapter(IPublisherServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseModel> NodePublishStartAsync(
            string endpoint, PublishStartRequestModel request, CancellationToken ct) {
            var result = await _client.NodePublishStartAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> NodePublishStopAsync(
            string endpoint, PublishStopRequestModel request, CancellationToken ct) {
            var result = await _client.NodePublishStopAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> NodePublishBulkAsync(
            string endpoint, PublishBulkRequestModel request, CancellationToken ct) {
            var result = await _client.NodePublishBulkAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> NodePublishListAsync(
            string endpoint, PublishedItemListRequestModel request, CancellationToken ct) {
            var result = await _client.NodePublishListAsync(endpoint, request, ct);
            return result;
        }

        private readonly IPublisherServiceApi _client;
    }
}
