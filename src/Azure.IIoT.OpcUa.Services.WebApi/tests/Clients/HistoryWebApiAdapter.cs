// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Services.Adapter {
    using Azure.IIoT.OpcUa.Api.Models;
    using Azure.IIoT.OpcUa.Services.Sdk;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements historian services as adapter on top of api.
    /// </summary>
    public sealed class HistoryWebApiAdapter : IHistorianServices<string> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        public HistoryWebApiAdapter(IHistoryServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceEventsDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryReplaceEventsAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(
            string endpoint, HistoryUpdateRequestModel<InsertEventsDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryInsertEventsAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryDeleteEventsAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryDeleteValuesAtTimesAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryDeleteModifiedValuesAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryDeleteValuesAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(
            string endpoint, HistoryUpdateRequestModel<ReplaceValuesDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryReplaceValuesAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(
            string endpoint, HistoryUpdateRequestModel<InsertValuesDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryInsertValuesAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryReadEventsAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct) {
            var result = await _client.HistoryReadEventsNextAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryReadValuesAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryReadValuesAtTimesAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryReadProcessedValuesAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request, CancellationToken ct) {
            var result = await _client.HistoryReadModifiedValuesAsync(endpoint, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct) {
            var result = await _client.HistoryReadValuesNextAsync(endpoint, request);
            return result;
        }

        private readonly IHistoryServiceApi _client;
    }
}
