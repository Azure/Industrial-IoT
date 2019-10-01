// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Clients {
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapts historian services to historic access services
    /// </summary>
    public sealed class HistoricAccessAdapter<T> : IHistorianServices<T> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="codec"></param>
        public HistoricAccessAdapter(IHistoricAccessServices<T> client, IVariantEncoder codec) {
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryDeleteEventsAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteEventsDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryDeleteValuesAtTimesAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryDeleteModifiedValuesAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryDeleteValuesAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryReplaceEventsAsync(
            T endpoint, HistoryUpdateRequestModel<ReplaceEventsDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryReplaceValuesAsync(
            T endpoint, HistoryUpdateRequestModel<ReplaceValuesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryInsertEventsAsync(
            T endpoint, HistoryUpdateRequestModel<InsertEventsDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResultModel> HistoryInsertValuesAsync(
            T endpoint, HistoryUpdateRequestModel<InsertValuesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            T endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeEvents);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            T endpoint, HistoryReadNextRequestModel request) {
            var results = await _client.HistoryReadNextAsync(endpoint, request);
            return results.ToSpecificModel(_codec.DecodeEvents);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            T endpoint, HistoryReadNextRequestModel request) {
            var results = await _client.HistoryReadNextAsync(endpoint, request);
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        private readonly IVariantEncoder _codec;
        private readonly IHistoricAccessServices<T> _client;
    }
}
