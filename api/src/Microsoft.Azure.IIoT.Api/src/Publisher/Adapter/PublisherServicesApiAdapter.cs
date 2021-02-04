// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Implements node and publish services as adapter on top of api.
    /// </summary>
    public sealed class PublisherServicesApiAdapter : IPublishServices<string> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public PublisherServicesApiAdapter(IPublisherServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            string endpoint,
            PublishStartRequestModel request,
            CancellationToken ct = default
        ) {
            var result = await _client.NodePublishStartAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(
            string endpoint,
            PublishStopRequestModel request,
            CancellationToken ct = default
        ) {
            var result = await _client.NodePublishStopAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResultModel> NodePublishBulkAsync(
            string endpoint,
            PublishBulkRequestModel request,
            CancellationToken ct = default
        ) {
            var result = await _client.NodePublishBulkAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResultModel> NodePublishListAsync(
            string endpoint,
            PublishedItemListRequestModel request,
            CancellationToken ct = default
        ) {
            var result = await _client.NodePublishListAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        private readonly IPublisherServiceApi _client;
    }
}
