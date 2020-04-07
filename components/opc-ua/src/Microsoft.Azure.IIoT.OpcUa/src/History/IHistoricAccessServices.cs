// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.Serializers;
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
        /// <returns></returns>
        Task<HistoryReadResultModel<VariantValue>> HistoryReadAsync(T endpoint,
            HistoryReadRequestModel<VariantValue> request);

        /// <summary>
        /// Read node history continuation
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadNextResultModel<VariantValue>> HistoryReadNextAsync(T endpoint,
            HistoryReadNextRequestModel request);

        /// <summary>
        /// Update node history
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResultModel> HistoryUpdateAsync(T endpoint,
            HistoryUpdateRequestModel<VariantValue> request);
    }
}
