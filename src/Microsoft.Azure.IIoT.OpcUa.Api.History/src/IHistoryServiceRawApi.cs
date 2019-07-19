// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Newtonsoft.Json.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Raw OPC Twin Historic Access service api
    /// </summary>
    public interface IHistoryServiceRawApi {

        /// <summary>
        /// Read node history with custom encoded extension object details
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<JToken>> HistoryReadRawAsync(
            string endpointId, HistoryReadRequestApiModel<JToken> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read history call with custom encoded extension object details
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseApiModel<JToken>> HistoryReadRawNextAsync(
            string endpointId, HistoryReadNextRequestApiModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Update using raw extension object details
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            string endpointId, HistoryUpdateRequestApiModel<JToken> request,
            CancellationToken ct = default);
    }
}
