// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api {
    /// <summary>
    /// Raw OPC Twin Historic Access service api
    /// </summary>
    public interface IHistoryServiceRawApi {
#if ZOMBIE

        /// <summary>
        /// Read node history with custom encoded extension object details
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<VariantValue>> HistoryReadRawAsync(
            string endpointId, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Read history call with custom encoded extension object details
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadRawNextAsync(
            string endpointId, HistoryReadNextRequestModel request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Update using raw extension object details
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpdateRawAsync(
            string endpointId, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct = default);
#endif
    }
}
