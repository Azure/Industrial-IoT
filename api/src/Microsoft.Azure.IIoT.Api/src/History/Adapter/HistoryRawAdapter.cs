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
    /// Implements historic access services as adapter on top of api.
    /// </summary>
    public sealed class HistoryRawAdapter : IHistoricAccessServices<string> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        public HistoryRawAdapter(IHistoryServiceRawApi client, ISerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<VariantValue>> HistoryReadAsync(
            string endpoint, HistoryReadRequestModel<VariantValue> request) {
            var result = await _client.HistoryReadRawAsync(endpoint,
                _serializer.Map<HistoryReadRequestApiModel<VariantValue>>(request));
            return _serializer.Map<HistoryReadResultModel<VariantValue>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<VariantValue>> HistoryReadNextAsync(
            string endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadRawNextAsync(endpoint,
                _serializer.Map<HistoryReadNextRequestApiModel>(request));
            return _serializer.Map<HistoryReadNextResultModel<VariantValue>>(result);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryUpdateAsync(
            string endpoint, HistoryUpdateRequestModel<VariantValue> request) {
            var result = await _client.HistoryUpdateRawAsync(endpoint,
                _serializer.Map<HistoryUpdateRequestApiModel<VariantValue>>(request));
            return _serializer.Map<HistoryUpdateResultModel>(result);
        }

        private readonly ISerializer _serializer;
        private readonly IHistoryServiceRawApi _client;
    }
}
