// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Publisher.Adapter {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements historic access services as adapter on top of supervisor api.
    /// </summary>
    public sealed class HistoryRawSupervisorAdapter : IHistoricAccessServices<ConnectionApiModel> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public HistoryRawSupervisorAdapter(IHistoryModuleApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<VariantValue>> HistoryReadAsync(
            ConnectionApiModel endpoint, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct) {
            var result = await _client.HistoryReadRawAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<VariantValue>> HistoryReadNextAsync(
            ConnectionApiModel endpoint, HistoryReadNextRequestModel request,
            CancellationToken ct) {
            var result = await _client.HistoryReadRawNextAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryUpdateAsync(
            ConnectionApiModel endpoint, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct) {
            var result = await _client.HistoryUpdateRawAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        private readonly IHistoryModuleApi _client;
    }
}
