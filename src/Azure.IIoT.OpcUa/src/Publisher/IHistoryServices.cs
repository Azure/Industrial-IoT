// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Historian services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHistoryServices<T>
    {
        /// <summary>
        /// Replace events
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Insert events
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Update or replace events
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete events
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(T endpoint,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete values at specified times
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(T endpoint,
            HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete modified values
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(T endpoint,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete values
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(T endpoint,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Replace values
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Insert values
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Update or replace values
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read historic events
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            T endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read next set of events
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            T endpoint, HistoryReadNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read historic values
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read historic values at times
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read processed historic values
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read modified values
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read next set of historic values
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            T endpoint, HistoryReadNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Stream values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAsync(T endpoint,
            HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Stream modified historic values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<HistoricValueModel> HistoryStreamModifiedValuesAsync(T endpoint,
            HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Stream historic values at times
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAtTimesAsync(T endpoint,
            HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Stream processed historic values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<HistoricValueModel> HistoryStreamProcessedValuesAsync(T endpoint,
            HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Stream modified historic events
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<HistoricEventModel> HistoryStreamEventsAsync(T endpoint,
            HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct = default);
    }
}
