// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extensions
    /// </summary>
    public static class HistorySupervisorApiEx {

        /// <summary>
        /// Read node history with custom encoded extension object details
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<HistoryReadResponseApiModel<VariantValue>> HistoryReadRawAsync(
            this IHistoryModuleApi api, string endpointUrl, HistoryReadRequestApiModel<VariantValue> request,
            CancellationToken ct = default) {
            return api.HistoryReadRawAsync(NewEndpoint(endpointUrl), request, ct);
        }

        /// <summary>
        /// Read history call with custom encoded extension object details
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<HistoryReadNextResponseApiModel<VariantValue>> HistoryReadRawNextAsync(
            this IHistoryModuleApi api, string endpointUrl, HistoryReadNextRequestApiModel request,
            CancellationToken ct = default) {
            return api.HistoryReadRawNextAsync(NewEndpoint(endpointUrl), request, ct);
        }

        /// <summary>
        /// Update using raw extension object details
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            this IHistoryModuleApi api, string endpointUrl, HistoryUpdateRequestApiModel<VariantValue> request,
            CancellationToken ct = default) {
            return api.HistoryUpdateRawAsync(NewEndpoint(endpointUrl), request, ct);
        }

        /// <summary>
        /// New endpoint
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <returns></returns>
        private static EndpointApiModel NewEndpoint(string endpointUrl) {
            return new EndpointApiModel {
                Url = endpointUrl,
                SecurityMode = SecurityMode.None,
                SecurityPolicy = "None"
            };
        }
    }
}
