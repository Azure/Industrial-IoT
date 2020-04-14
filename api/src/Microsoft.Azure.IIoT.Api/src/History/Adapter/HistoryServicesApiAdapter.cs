// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading.Tasks;
    using System.Linq;

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
        public async Task<HistoryUpdateResultModel> HistoryReplaceEventsAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceEventsDetailsModel> request) {
            var result = await _client.HistoryReplaceEventsAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryInsertEventsAsync(
            string endpoint, HistoryUpdateRequestModel<InsertEventsDetailsModel> request) {
            var result = await _client.HistoryInsertEventsAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteEventsAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteEventsDetailsModel> request) {
            var result = await _client.HistoryDeleteEventsAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteValuesAtTimesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAtTimesAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteModifiedValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteModifiedValuesAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryReplaceValuesAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceValuesDetailsModel> request) {
            var result = await _client.HistoryReplaceValuesAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryInsertValuesAsync(
            string endpoint, HistoryUpdateRequestModel<InsertValuesDetailsModel> request) {
            var result = await _client.HistoryInsertValuesAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request) {
            var result = await _client.HistoryReadEventsAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel(m => m?.Select(x => x.ToServiceModel()).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadEventsNextAsync(endpoint, request.ToApiModel());
            return result.ToServiceModel(m => m?.Select(x => x.ToServiceModel()).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel(m => m?.Select(x => x.ToServiceModel()).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAtTimesAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel(m => m?.Select(x => x.ToServiceModel()).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request) {
            var result = await _client.HistoryReadProcessedValuesAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel(m => m?.Select(x => x.ToServiceModel()).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryReadModifiedValuesAsync(endpoint,
                request.ToApiModel(m => m.ToApiModel()));
            return result.ToServiceModel(m => m?.Select(x => x.ToServiceModel()).ToArray());
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadValuesNextAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel(m => m?.Select(x => x.ToServiceModel()).ToArray());
        }

        private readonly IHistoryServiceApi _client;
    }
}
