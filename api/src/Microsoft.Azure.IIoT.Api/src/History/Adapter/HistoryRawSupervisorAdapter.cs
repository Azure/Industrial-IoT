// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements historic access services as adapter on top of supervisor api.
    /// </summary>
    public sealed class HistoryRawSupervisorAdapter : IHistoricAccessServices<EndpointApiModel> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public HistoryRawSupervisorAdapter(IHistoryModuleApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<JToken>> HistoryReadAsync(
            EndpointApiModel endpoint, HistoryReadRequestModel<JToken> request) {
            var result = await _client.HistoryReadRawAsync(endpoint,
                request.Map<HistoryReadRequestApiModel<JToken>>());
            return result.Map<HistoryReadResultModel<JToken>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<JToken>> HistoryReadNextAsync(
            EndpointApiModel endpoint, HistoryReadNextRequestModel request) {
            var result = await _client.HistoryReadRawNextAsync(endpoint,
                request.Map<HistoryReadNextRequestApiModel>());
            return result.Map<HistoryReadNextResultModel<JToken>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryUpdateAsync(
            EndpointApiModel endpoint, HistoryUpdateRequestModel<JToken> request) {
            var result = await _client.HistoryUpdateRawAsync(endpoint,
                request.Map<HistoryUpdateRequestApiModel<JToken>>());
            return result.Map<HistoryUpdateResultModel>();
        }

        private readonly IHistoryModuleApi _client;
    }
}
