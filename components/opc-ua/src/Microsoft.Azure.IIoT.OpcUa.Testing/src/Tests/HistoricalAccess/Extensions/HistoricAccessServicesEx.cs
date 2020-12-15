// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapts historian services to historic access services
    /// </summary>
    internal static class HistoricAccessServicesEx {

        /// <summary>
        /// Read values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static async Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAsync<T>(
            this IHistoricAccessServices<T> client, T endpoint,
            HistoryReadRequestModel<ReadValuesDetailsModel> request, IVariantEncoderFactory codec) {
            var results = await client.HistoryReadAsync(endpoint, request.ToRawModel(codec.Default.Encode));
            return results.ToSpecificModel(codec.Default.DecodeValues);
        }

        /// <summary>
        /// Read next set of values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static async Task<HistoryReadNextResultModel<HistoricValueModel[]>> HistoryReadValuesNextAsync<T>(
            this IHistoricAccessServices<T> client, T endpoint,
            HistoryReadNextRequestModel request, IVariantEncoderFactory codec) {
            var results = await client.HistoryReadNextAsync(endpoint, request);
            return results.ToSpecificModel(codec.Default.DecodeValues);
        }
    }
}
