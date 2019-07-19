// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Newtonsoft.Json;
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
                Map<HistoryUpdateRequestApiModel<ReplaceEventsDetailsApiModel>>(request));
            return Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryInsertEventsAsync(
            string endpoint, HistoryUpdateRequestModel<InsertEventsDetailsModel> request) {
            var result = await _client.HistoryInsertEventsAsync(endpoint,
                Map<HistoryUpdateRequestApiModel<InsertEventsDetailsApiModel>>(request));
            return Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteEventsAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteEventsDetailsModel> request) {
            var result = await _client.HistoryDeleteEventsAsync(endpoint,
                Map<HistoryUpdateRequestApiModel<DeleteEventsDetailsApiModel>>(request));
            return Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteValuesAtTimesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAtTimesAsync(endpoint,
                Map<HistoryUpdateRequestApiModel<DeleteValuesAtTimesDetailsApiModel>>(request));
            return Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteModifiedValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteModifiedValuesAsync(endpoint,
                Map<HistoryUpdateRequestApiModel<DeleteModifiedValuesDetailsApiModel>>(request));
            return Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAsync(endpoint,
                Map<HistoryUpdateRequestApiModel<DeleteValuesDetailsApiModel>>(request));
            return Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryReplaceValuesAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceValuesDetailsModel> request) {
            var result = await _client.HistoryReplaceValuesAsync(endpoint,
                Map<HistoryUpdateRequestApiModel<ReplaceValuesDetailsApiModel>>(request));
            return Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryInsertValuesAsync(
            string endpoint, HistoryUpdateRequestModel<InsertValuesDetailsModel> request) {
            var result = await _client.HistoryInsertValuesAsync(endpoint,
                Map<HistoryUpdateRequestApiModel<InsertValuesDetailsApiModel>>(request));
            return Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request) {
            var result = await _client.HistoryReadEventsAsync(endpoint,
                Map<HistoryReadRequestApiModel<ReadEventsDetailsApiModel>>(request));
            return Map<HistoryReadResultModel<HistoricEventModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadEventsNextAsync(endpoint,
                Map<HistoryReadNextRequestApiModel>(request));
            return Map<HistoryReadNextResultModel<HistoricEventModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAsync(endpoint,
                Map<HistoryReadRequestApiModel<ReadValuesDetailsApiModel>>(request));
            return Map<HistoryReadResultModel<HistoricValueModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAtTimesAsync(endpoint,
                Map<HistoryReadRequestApiModel<ReadValuesAtTimesDetailsApiModel>>(request));
            return Map<HistoryReadResultModel<HistoricValueModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request) {
            var result = await _client.HistoryReadProcessedValuesAsync(endpoint,
                Map<HistoryReadRequestApiModel<ReadProcessedValuesDetailsApiModel>>(request));
            return Map<HistoryReadResultModel<HistoricValueModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryReadModifiedValuesAsync(endpoint,
                Map<HistoryReadRequestApiModel<ReadModifiedValuesDetailsApiModel>>(request));
            return Map<HistoryReadResultModel<HistoricValueModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadValuesNextAsync(endpoint,
                Map<HistoryReadNextRequestApiModel>(request));
            return Map<HistoryReadNextResultModel<HistoricValueModel[]>>(result);
        }

        /// <summary>
        /// Convert from to
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        private static T Map<T>(object model) {
            return JsonConvertEx.DeserializeObject<T>(
                JsonConvertEx.SerializeObject(model));
        }

        private readonly IHistoryServiceApi _client;
    }
}
