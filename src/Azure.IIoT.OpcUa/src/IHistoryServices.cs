// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa {
    using Azure.IIoT.OpcUa.Shared.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Historian services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHistoryServices<T> {

        /// <summary>
        /// Replace events
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(T id,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Insert events
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(T id,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Update or replace events
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(T id,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete events
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(T id,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete values at specified times
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(T id,
            HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete modified values
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(T id,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete values
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(T id,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Replace values
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(T id,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Insert values
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(T id,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Update or replace values
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(T id,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read historic events
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            T id, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read next set of events
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            T id, HistoryReadNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read historic values
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            T id, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read historic values at times
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            T id, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read processed historic values
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            T id, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read modified values
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            T id, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read next set of historic values
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            T id, HistoryReadNextRequestModel request, CancellationToken ct = default);
    }
}
