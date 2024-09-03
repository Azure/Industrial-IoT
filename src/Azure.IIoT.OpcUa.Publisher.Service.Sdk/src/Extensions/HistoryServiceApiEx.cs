// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin service api extensions
    /// </summary>
    public static class HistoryServiceApiEx
    {
        /// <summary>
        /// Read all historic values
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricValueModel>> HistoryReadAllValuesAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestModel<ReadValuesDetailsModel> request)
        {
            var result = await client.HistoryReadValuesAsync(endpointId, request).ConfigureAwait(false);
            return await HistoryReadAllRemainingValuesAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History?.AsEnumerable()
                    ?? Enumerable.Empty<HistoricValueModel>()).ConfigureAwait(false);
        }

        /// <summary>
        /// Read entire list of modified values
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricValueModel>> HistoryReadAllModifiedValuesAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request)
        {
            var result = await client.HistoryReadModifiedValuesAsync(endpointId, request).ConfigureAwait(false);
            return await HistoryReadAllRemainingValuesAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History?.AsEnumerable()
                    ?? Enumerable.Empty<HistoricValueModel>()).ConfigureAwait(false);
        }

        /// <summary>
        /// Read entire historic values at specific datum
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricValueModel>> HistoryReadAllValuesAtTimesAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request)
        {
            var result = await client.HistoryReadValuesAtTimesAsync(endpointId, request).ConfigureAwait(false);
            return await HistoryReadAllRemainingValuesAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History?.AsEnumerable()
                    ?? Enumerable.Empty<HistoricValueModel>()).ConfigureAwait(false);
        }

        /// <summary>
        /// Read entire processed historic values
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricValueModel>> HistoryReadAllProcessedValuesAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request)
        {
            var result = await client.HistoryReadProcessedValuesAsync(endpointId, request).ConfigureAwait(false);
            return await HistoryReadAllRemainingValuesAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History?.AsEnumerable()
                    ?? Enumerable.Empty<HistoricValueModel>()).ConfigureAwait(false);
        }

        /// <summary>
        /// Read entire event history
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<HistoricEventModel>> HistoryReadAllEventsAsync(
            this IHistoryServiceApi client, string endpointId,
            HistoryReadRequestModel<ReadEventsDetailsModel> request)
        {
            var result = await client.HistoryReadEventsAsync(endpointId, request).ConfigureAwait(false);
            return await HistoryReadAllRemainingEventsAsync(client, endpointId, request.Header,
                result.ContinuationToken, result.History?.AsEnumerable()
                    ?? Enumerable.Empty<HistoricEventModel>()).ConfigureAwait(false);
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
        private static async Task<IEnumerable<HistoricValueModel>> HistoryReadAllRemainingValuesAsync(
            IHistoryServiceApi client, string endpointId, RequestHeaderModel? header,
            string? continuationToken, IEnumerable<HistoricValueModel> returning)
        {
            while (continuationToken != null)
            {
                var response = await client.HistoryReadValuesNextAsync(endpointId, new HistoryReadNextRequestModel
                {
                    ContinuationToken = continuationToken,
                    Header = header
                }).ConfigureAwait(false);
                continuationToken = response.ContinuationToken;
                if (response.History != null)
                {
                    returning = returning.Concat(response.History);
                }
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
        private static async Task<IEnumerable<HistoricEventModel>> HistoryReadAllRemainingEventsAsync(
            IHistoryServiceApi client, string endpointId, RequestHeaderModel? header,
            string? continuationToken, IEnumerable<HistoricEventModel> returning)
        {
            while (continuationToken != null)
            {
                var response = await client.HistoryReadEventsNextAsync(endpointId, new HistoryReadNextRequestModel
                {
                    ContinuationToken = continuationToken,
                    Header = header
                }).ConfigureAwait(false);
                continuationToken = response.ContinuationToken;
                if (response.History != null)
                {
                    returning = returning.Concat(response.History);
                }
            }
            return returning;
        }
    }
}
