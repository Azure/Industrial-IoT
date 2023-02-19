// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk {
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents twin historic access api
    /// </summary>
    public interface IHistoryRawApi {

        /// <summary>
        /// Read node history with custom encoded extension object details
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<VariantValue>> HistoryReadRawAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read history call with custom encoded extension object details
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadRawNextAsync(
            ConnectionModel endpoint, HistoryReadNextRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Update using raw extension object details
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpdateRawAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct = default);
    }
}
