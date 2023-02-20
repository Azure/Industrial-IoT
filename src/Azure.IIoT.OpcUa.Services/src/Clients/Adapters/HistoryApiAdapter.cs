// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter {
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Shared.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapts historian services to historic access services
    /// </summary>
    public sealed class HistoryApiAdapter : IHistoryServices<ConnectionModel> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        public HistoryApiAdapter(IHistoryApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct) {
            return _client.HistoryDeleteEventsAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct) {
            return _client.HistoryDeleteValuesAtTimesAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct) {
            return _client.HistoryDeleteModifiedValuesAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct) {
            return _client.HistoryDeleteValuesAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct) {
            return _client.HistoryReplaceEventsAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct) {
            return _client.HistoryReplaceValuesAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct) {
            return _client.HistoryInsertEventsAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct) {
            return _client.HistoryInsertValuesAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct) {
            return _client.HistoryUpsertEventsAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct) {
            return _client.HistoryUpsertValuesAsync(connection, request, ct);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct) {
            var results = await _client.HistoryReadEventsAsync(connection, request, ct);
            return results;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request, CancellationToken ct) {
            var results = await _client.HistoryReadEventsNextAsync(connection, request, ct);
            return results;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadValuesDetailsModel> request, CancellationToken ct) {
            var results = await _client.HistoryReadValuesAsync(connection, request, ct);
            return results;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request, CancellationToken ct) {
            var results = await _client.HistoryReadValuesAtTimesAsync(connection, request, ct);
            return results;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request, CancellationToken ct) {
            var results = await _client.HistoryReadProcessedValuesAsync(connection, request, ct);
            return results;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request, CancellationToken ct) {
            var results = await _client.HistoryReadModifiedValuesAsync(connection, request, ct);
            return results;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request, CancellationToken ct) {
            var results = await _client.HistoryReadValuesNextAsync(connection, request, ct);
            return results;
        }

        private readonly IHistoryApi _client;
    }
}
