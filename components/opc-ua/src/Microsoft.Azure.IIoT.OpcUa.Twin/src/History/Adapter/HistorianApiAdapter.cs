// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Api.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapts historian services to historic access services
    /// </summary>
    public sealed class HistorianApiAdapter<T> : IHistorianServices<T> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="codec"></param>
        public HistorianApiAdapter(IHistoricAccessServices<T> client, IVariantEncoderFactory codec) {
            _codec = codec?.Default ?? throw new ArgumentNullException(nameof(codec));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteEventsDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(
            T endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(
            T endpoint, HistoryUpdateRequestModel<ReplaceEventsDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(
            T endpoint, HistoryUpdateRequestModel<ReplaceValuesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(
            T endpoint, HistoryUpdateRequestModel<InsertEventsDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(
            T endpoint, HistoryUpdateRequestModel<InsertValuesDetailsModel> request) {
            return _client.HistoryUpdateAsync(endpoint, request.ToRawModel(_codec.Encode));
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            T endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeEvents);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            T endpoint, HistoryReadNextRequestModel request) {
            var results = await _client.HistoryReadNextAsync(endpoint, request);
            return results.ToSpecificModel(_codec.DecodeEvents);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request) {
            var results = await _client.HistoryReadAsync(endpoint, request.ToRawModel(_codec.Encode));
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            T endpoint, HistoryReadNextRequestModel request) {
            var results = await _client.HistoryReadNextAsync(endpoint, request);
            return results.ToSpecificModel(_codec.DecodeValues);
        }

        private readonly IVariantEncoder _codec;
        private readonly IHistoricAccessServices<T> _client;
    }
}
