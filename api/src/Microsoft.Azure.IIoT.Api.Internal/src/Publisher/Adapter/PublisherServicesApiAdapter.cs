// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Publisher.Adapter {
#if ZOMBIE

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
        public async Task<PublishStartResponseModel> NodePublishStartAsync(
            string endpoint, PublishStartRequestModel request) {
            var result = await _client.NodePublishStartAsync(endpoint,
                request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> NodePublishStopAsync(
            string endpoint, PublishStopRequestModel request) {
            var result = await _client.NodePublishStopAsync(endpoint,
                request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> NodePublishBulkAsync(
            string endpoint, PublishBulkRequestModel request) {
            var result = await _client.NodePublishBulkAsync(endpoint,
                request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> NodePublishListAsync(
            string endpoint, PublishedItemListRequestModel request) {
            var result = await _client.NodePublishListAsync(endpoint,
                request);
            return result;
        }

        private readonly IPublisherServiceApi _client;
    }
#endif
}
