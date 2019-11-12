// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements historian services as adapter on top of api.
    /// </summary>
    public sealed class HistoryAdapter : IHistorianServices<string> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        public HistoryAdapter(IHistoryServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryReplaceEventsAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceEventsDetailsModel> request) {
            var result = await _client.HistoryReplaceEventsAsync(endpoint,
                request.Map<HistoryUpdateRequestApiModel<ReplaceEventsDetailsApiModel>>());
            return result.Map<HistoryUpdateResultModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryInsertEventsAsync(
            string endpoint, HistoryUpdateRequestModel<InsertEventsDetailsModel> request) {
            var result = await _client.HistoryInsertEventsAsync(endpoint,
                request.Map<HistoryUpdateRequestApiModel<InsertEventsDetailsApiModel>>());
            return result.Map<HistoryUpdateResultModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteEventsAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteEventsDetailsModel> request) {
            var result = await _client.HistoryDeleteEventsAsync(endpoint,
                request.Map<HistoryUpdateRequestApiModel<DeleteEventsDetailsApiModel>>());
            return result.Map<HistoryUpdateResultModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteValuesAtTimesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAtTimesAsync(endpoint,
                request.Map<HistoryUpdateRequestApiModel<DeleteValuesAtTimesDetailsApiModel>>());
            return result.Map<HistoryUpdateResultModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteModifiedValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteModifiedValuesAsync(endpoint,
                request.Map<HistoryUpdateRequestApiModel<DeleteModifiedValuesDetailsApiModel>>());
            return result.Map<HistoryUpdateResultModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAsync(endpoint,
                request.Map<HistoryUpdateRequestApiModel<DeleteValuesDetailsApiModel>>());
            return result.Map<HistoryUpdateResultModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryReplaceValuesAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceValuesDetailsModel> request) {
            var result = await _client.HistoryReplaceValuesAsync(endpoint,
                request.Map<HistoryUpdateRequestApiModel<ReplaceValuesDetailsApiModel>>());
            return result.Map<HistoryUpdateResultModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryInsertValuesAsync(
            string endpoint, HistoryUpdateRequestModel<InsertValuesDetailsModel> request) {
            var result = await _client.HistoryInsertValuesAsync(endpoint,
                request.Map<HistoryUpdateRequestApiModel<InsertValuesDetailsApiModel>>());
            return result.Map<HistoryUpdateResultModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request) {
            var result = await _client.HistoryReadEventsAsync(endpoint,
                request.Map<HistoryReadRequestApiModel<ReadEventsDetailsApiModel>>());
            return result.Map<HistoryReadResultModel<HistoricEventModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadEventsNextAsync(endpoint,
                request.Map<HistoryReadNextRequestApiModel>());
            return result.Map<HistoryReadNextResultModel<HistoricEventModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAsync(endpoint,
                request.Map<HistoryReadRequestApiModel<ReadValuesDetailsApiModel>>());
            return result.Map<HistoryReadResultModel<HistoricValueModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAtTimesAsync(endpoint,
                request.Map<HistoryReadRequestApiModel<ReadValuesAtTimesDetailsApiModel>>());
            return result.Map<HistoryReadResultModel<HistoricValueModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request) {
            var result = await _client.HistoryReadProcessedValuesAsync(endpoint,
                request.Map<HistoryReadRequestApiModel<ReadProcessedValuesDetailsApiModel>>());
            return result.Map<HistoryReadResultModel<HistoricValueModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryReadModifiedValuesAsync(endpoint,
                request.Map<HistoryReadRequestApiModel<ReadModifiedValuesDetailsApiModel>>());
            return result.Map<HistoryReadResultModel<HistoricValueModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadValuesNextAsync(endpoint,
                request.Map<HistoryReadNextRequestApiModel>());
            return result.Map<HistoryReadNextResultModel<HistoricValueModel[]>>();
        }

        private readonly IHistoryServiceApi _client;
    }
}
