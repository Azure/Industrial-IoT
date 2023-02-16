// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api {
#if ZOMBIE

    /// <summary>
    /// Extensions
    /// </summary>
    public static class HistorySupervisorApiEx {
#if ZOMBIE

        /// <summary>
        /// Read node history with custom encoded extension object details
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<HistoryReadResponseModel<VariantValue>> HistoryReadRawAsync(
            this IHistoryModuleApi api, string endpointUrl, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct = default) {
            return api.HistoryReadRawAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Read history call with custom encoded extension object details
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadRawNextAsync(
            this IHistoryModuleApi api, string endpointUrl, HistoryReadNextRequestModel request,
            CancellationToken ct = default) {
            return api.HistoryReadRawNextAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Update using raw extension object details
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<HistoryUpdateResponseModel> HistoryUpdateRawAsync(
            this IHistoryModuleApi api, string endpointUrl, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct = default) {
            return api.HistoryUpdateRawAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif

        /// <summary>
        /// New connection
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <returns></returns>
        private static ConnectionModel ConnectionTo(string endpointUrl) {
            return new ConnectionModel {
                Endpoint = new EndpointModel {
                    Url = endpointUrl,
                    SecurityMode = SecurityMode.None,
                    SecurityPolicy = "None"
                }
            };
        }
    }
#endif
}
