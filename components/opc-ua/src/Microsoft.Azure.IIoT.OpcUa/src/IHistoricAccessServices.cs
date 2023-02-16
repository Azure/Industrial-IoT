// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History {
    using Microsoft.Azure.IIoT.Api.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Historic access services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHistoricAccessServices<T> {

        /// <summary>
        /// Read node history
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(T endpoint,
            HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read node history continuation
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            T endpoint, HistoryReadNextRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Update node history
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpdateAsync(T endpoint,
            HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct = default);
    }
}
