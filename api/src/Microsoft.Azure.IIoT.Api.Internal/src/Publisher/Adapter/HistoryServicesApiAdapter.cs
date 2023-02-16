// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Publisher.Adapter {
#if ZOMBIE

    /// <summary>
    /// Implements historian services as adapter on top of api.
    /// </summary>
    public sealed class HistoryServicesApiAdapter : IHistorianServices<string> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        public HistoryServicesApiAdapter(IHistoryServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceEventsDetailsModel> request) {
            var result = await _client.HistoryReplaceEventsAsync(endpoint,
                request.ToModel(m => m));
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(
            string endpoint, HistoryUpdateRequestModel<InsertEventsDetailsModel> request) {
            var result = await _client.HistoryInsertEventsAsync(endpoint,
                request.ToModel(m => m));
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteEventsDetailsModel> request) {
            var result = await _client.HistoryDeleteEventsAsync(endpoint,
                request.ToModel(m => m));
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAtTimesAsync(endpoint,
                request.ToModel(m => m));
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteModifiedValuesAsync(endpoint,
                request.ToModel(m => m));
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAsync(endpoint,
                request.ToModel(m => m));
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceValuesDetailsModel> request) {
            var result = await _client.HistoryReplaceValuesAsync(endpoint,
                request.ToModel(m => m));
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(
            string endpoint, HistoryUpdateRequestModel<InsertValuesDetailsModel> request) {
            var result = await _client.HistoryInsertValuesAsync(endpoint,
                request.ToModel(m => m));
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request) {
            var result = await _client.HistoryReadEventsAsync(endpoint,
                request.ToModel(m => m));
            return result.ToServiceModel(m => m?.Select(x => x).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadEventsNextAsync(endpoint, request);
            return result.ToServiceModel(m => m?.Select(x => x).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAsync(endpoint,
                request.ToModel(m => m));
            return result.ToServiceModel(m => m?.Select(x => x).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAtTimesAsync(endpoint,
                request.ToModel(m => m));
            return result.ToServiceModel(m => m?.Select(x => x).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request) {
            var result = await _client.HistoryReadProcessedValuesAsync(endpoint,
                request.ToModel(m => m));
            return result.ToServiceModel(m => m?.Select(x => x).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryReadModifiedValuesAsync(endpoint,
                request.ToModel(m => m));
            return result.ToServiceModel(m => m?.Select(x => x).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadValuesNextAsync(endpoint,
                request);
            return result.ToServiceModel(m => m?.Select(x => x).ToArray());
        }

        private readonly IHistoryServiceApi _client;
    }
#endif
}
