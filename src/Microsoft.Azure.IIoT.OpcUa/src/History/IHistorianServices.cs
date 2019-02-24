// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Historian services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHistorianServices<T> {

        /// <summary>
        /// Delete events
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResultModel> HistoryDeleteEventsAsync(T endpoint,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request);

        /// <summary>
        /// Delete values at specified times
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResultModel> HistoryDeleteValuesAtTimesAsync(T endpoint,
            HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request);

        /// <summary>
        /// Delete modified values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResultModel> HistoryDeleteModifiedValuesAsync(T endpoint,
            HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request);

        /// <summary>
        /// Delete values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResultModel> HistoryDeleteValuesAsync(T endpoint,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request);

        /// <summary>
        /// Update events
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResultModel> HistoryUpdateEventsAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request);

        /// <summary>
        /// Update values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResultModel> HistoryUpdateValuesAsync(T endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request);

        /// <summary>
        /// Read historic events
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResultModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            T endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request);

        /// <summary>
        /// Read next set of events
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadNextResultModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            T endpoint, HistoryReadNextRequestModel request);

        /// <summary>
        /// Read historic values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request);

        /// <summary>
        /// Read historic values at times
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            T endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request);

        /// <summary>
        /// Read processed historic values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request);

        /// <summary>
        /// Read modified values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResultModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            T endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request);

        /// <summary>
        /// Read next set of historic values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadNextResultModel<HistoricValueModel[]>> HistoryReadValueNextAsync(
            T endpoint, HistoryReadNextRequestModel request);
    }
}
