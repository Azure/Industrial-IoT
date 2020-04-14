// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin service api extensions
    /// </summary>
    public static class HistoryServiceApiEx {

        /// <summary>
        /// Read all historic values
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricValueApiModel>> HistoryReadAllValuesAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestApiModel<ReadValuesDetailsApiModel> request) {
            var result = await client.HistoryReadValuesAsync(endpointId, request);
            return await HistoryReadAllRemainingValuesAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History.AsEnumerable());
        }

        /// <summary>
        /// Read entire list of modified values
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricValueApiModel>> HistoryReadAllModifiedValuesAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestApiModel<ReadModifiedValuesDetailsApiModel> request) {
            var result = await client.HistoryReadModifiedValuesAsync(endpointId, request);
            return await HistoryReadAllRemainingValuesAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History.AsEnumerable());
        }

        /// <summary>
        /// Read entire historic values at specific datum
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricValueApiModel>> HistoryReadAllValuesAtTimesAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestApiModel<ReadValuesAtTimesDetailsApiModel> request) {
            var result = await client.HistoryReadValuesAtTimesAsync(endpointId, request);
            return await HistoryReadAllRemainingValuesAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History.AsEnumerable());
        }

        /// <summary>
        /// Read entire processed historic values
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricValueApiModel>> HistoryReadAllProcessedValuesAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestApiModel<ReadProcessedValuesDetailsApiModel> request) {
            var result = await client.HistoryReadProcessedValuesAsync(endpointId, request);
            return await HistoryReadAllRemainingValuesAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History.AsEnumerable());
        }

        /// <summary>
        /// Read entire event history
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricEventApiModel>> HistoryReadAllEventsAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestApiModel<ReadEventsDetailsApiModel> request) {
            var result = await client.HistoryReadEventsAsync(endpointId, request);
            return await HistoryReadAllRemainingEventsAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History.AsEnumerable());
        }


        /// <summary>
        /// Read all remaining values
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="header"></param>
        /// <param name="continuationToken"></param>
        /// <param name="returning"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<HistoricValueApiModel>> HistoryReadAllRemainingValuesAsync(
            IHistoryServiceApi client, string endpointId, RequestHeaderApiModel header,
            string continuationToken, IEnumerable<HistoricValueApiModel> returning) {
            while (continuationToken != null) {
                var response = await client.HistoryReadValuesNextAsync(endpointId, new HistoryReadNextRequestApiModel {
                    ContinuationToken = continuationToken,
                    Header = header
                });
                continuationToken = response.ContinuationToken;
                returning = returning.Concat(response.History);
            }
            return returning;
        }

        /// <summary>
        /// Read all remaining events
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="header"></param>
        /// <param name="continuationToken"></param>
        /// <param name="returning"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<HistoricEventApiModel>> HistoryReadAllRemainingEventsAsync(
            IHistoryServiceApi client, string endpointId, RequestHeaderApiModel header,
            string continuationToken, IEnumerable<HistoricEventApiModel> returning) {
            while (continuationToken != null) {
                var response = await client.HistoryReadEventsNextAsync(endpointId, new HistoryReadNextRequestApiModel {
                    ContinuationToken = continuationToken,
                    Header = header
                });
                continuationToken = response.ContinuationToken;
                returning = returning.Concat(response.History);
            }
            return returning;
        }

    }
}
