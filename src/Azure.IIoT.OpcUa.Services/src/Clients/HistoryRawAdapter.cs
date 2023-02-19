// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter {
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements historic access services as adapter on top of supervisor api.
    /// </summary>
    public sealed class HistoryRawAdapter : IHistoricAccessServices<ConnectionModel> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public HistoryRawAdapter(IHistoryRawApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct) {
            var result = await _client.HistoryReadRawAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel endpoint, HistoryReadNextRequestModel request,
            CancellationToken ct) {
            var result = await _client.HistoryReadRawNextAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct) {
            var result = await _client.HistoryUpdateRawAsync(endpoint, request, ct);
            return result;
        }

        private readonly IHistoryRawApi _client;
    }
}
