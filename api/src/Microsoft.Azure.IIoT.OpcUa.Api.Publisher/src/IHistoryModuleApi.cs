// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC twin module history api
    /// </summary>
    public interface IHistoryModuleApi {

        /// <summary>
        /// Read node history with custom encoded extension object details
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<VariantValue>> HistoryReadRawAsync(
            ConnectionApiModel endpoint, HistoryReadRequestApiModel<VariantValue> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read history call with custom encoded extension object details
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseApiModel<VariantValue>> HistoryReadRawNextAsync(
            ConnectionApiModel endpoint, HistoryReadNextRequestApiModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Update using raw extension object details
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            ConnectionApiModel endpoint, HistoryUpdateRequestApiModel<VariantValue> request,
            CancellationToken ct = default);
    }
}
