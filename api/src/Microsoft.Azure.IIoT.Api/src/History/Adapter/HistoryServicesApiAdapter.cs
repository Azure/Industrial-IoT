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

    /// <summary>
    /// Implements historian services as adapter on top of api.
    /// </summary>
    public sealed class HistoryServicesApiAdapter : IHistorianServices<string> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        public HistoryServicesApiAdapter(IHistoryServiceApi client, ISerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryReplaceEventsAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceEventsDetailsModel> request) {
            var result = await _client.HistoryReplaceEventsAsync(endpoint,
                _serializer.Map<HistoryUpdateRequestApiModel<ReplaceEventsDetailsApiModel>>(request));
            return _serializer.Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryInsertEventsAsync(
            string endpoint, HistoryUpdateRequestModel<InsertEventsDetailsModel> request) {
            var result = await _client.HistoryInsertEventsAsync(endpoint,
                _serializer.Map<HistoryUpdateRequestApiModel<InsertEventsDetailsApiModel>>(request));
            return _serializer.Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteEventsAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteEventsDetailsModel> request) {
            var result = await _client.HistoryDeleteEventsAsync(endpoint,
                _serializer.Map<HistoryUpdateRequestApiModel<DeleteEventsDetailsApiModel>>(request));
            return _serializer.Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteValuesAtTimesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAtTimesAsync(endpoint,
                _serializer.Map<HistoryUpdateRequestApiModel<DeleteValuesAtTimesDetailsApiModel>>(request));
            return _serializer.Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteModifiedValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteModifiedValuesAsync(endpoint,
                _serializer.Map<HistoryUpdateRequestApiModel<DeleteModifiedValuesDetailsApiModel>>(request));
            return _serializer.Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryDeleteValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            var result = await _client.HistoryDeleteValuesAsync(endpoint,
                _serializer.Map<HistoryUpdateRequestApiModel<DeleteValuesDetailsApiModel>>(request));
            return _serializer.Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryReplaceValuesAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceValuesDetailsModel> request) {
            var result = await _client.HistoryReplaceValuesAsync(endpoint,
                _serializer.Map<HistoryUpdateRequestApiModel<ReplaceValuesDetailsApiModel>>(request));
            return _serializer.Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryInsertValuesAsync(
            string endpoint, HistoryUpdateRequestModel<InsertValuesDetailsModel> request) {
            var result = await _client.HistoryInsertValuesAsync(endpoint,
                _serializer.Map<HistoryUpdateRequestApiModel<InsertValuesDetailsApiModel>>(request));
            return _serializer.Map<HistoryUpdateResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request) {
            var result = await _client.HistoryReadEventsAsync(endpoint,
                _serializer.Map<HistoryReadRequestApiModel<ReadEventsDetailsApiModel>>(request));
            return _serializer.Map<HistoryReadResultModel<HistoricEventModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadEventsNextAsync(endpoint,
                _serializer.Map<HistoryReadNextRequestApiModel>(request));
            return _serializer.Map<HistoryReadNextResultModel<HistoricEventModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAsync(endpoint,
                _serializer.Map<HistoryReadRequestApiModel<ReadValuesDetailsApiModel>>(request));
            return _serializer.Map<HistoryReadResultModel<HistoricValueModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request) {
            var result = await _client.HistoryReadValuesAtTimesAsync(endpoint,
                _serializer.Map<HistoryReadRequestApiModel<ReadValuesAtTimesDetailsApiModel>>(request));
            return _serializer.Map<HistoryReadResultModel<HistoricValueModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request) {
            var result = await _client.HistoryReadProcessedValuesAsync(endpoint,
                _serializer.Map<HistoryReadRequestApiModel<ReadProcessedValuesDetailsApiModel>>(request));
            return _serializer.Map<HistoryReadResultModel<HistoricValueModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request) {
            var result = await _client.HistoryReadModifiedValuesAsync(endpoint,
                _serializer.Map<HistoryReadRequestApiModel<ReadModifiedValuesDetailsApiModel>>(request));
            return _serializer.Map<HistoryReadResultModel<HistoricValueModel[]>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadValuesNextAsync(endpoint,
                _serializer.Map<HistoryReadNextRequestApiModel>(request));
            return _serializer.Map<HistoryReadNextResultModel<HistoricValueModel[]>>(result);
        }

        private readonly ISerializer _serializer;
        private readonly IHistoryServiceApi _client;
    }
}
