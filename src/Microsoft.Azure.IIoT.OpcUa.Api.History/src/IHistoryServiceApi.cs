// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC Twin Historic Access service api functions
    /// </summary>
    public interface IHistoryServiceApi {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<StatusResponseApiModel> GetServiceStatusAsync();

        /// <summary>
        /// Read raw historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadValuesDetailsApiModel> request);

        /// <summary>
        /// Read modified historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadModifiedValuesDetailsApiModel> request);

        /// <summary>
        /// Read historic values at specific datum
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadValuesAtTimesDetailsApiModel> request);

        /// <summary>
        /// Read processed historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadProcessedValuesDetailsApiModel> request);

        /// <summary>
        /// Read next set of historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesNextAsync(
            string endpointId, HistoryReadNextRequestApiModel request);

        /// <summary>
        /// Read event history
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsAsync(
            string endpointId, HistoryReadRequestApiModel<ReadEventsDetailsApiModel> request);

        /// <summary>
        /// Read next set of historic events
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsNextAsync(
            string endpointId, HistoryReadNextRequestApiModel request);

        /// <summary>
        /// Update historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryUpdateValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<UpdateValuesDetailsApiModel> request);

        /// <summary>
        /// Update historic events
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryUpdateEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<UpdateEventsDetailsApiModel> request);

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteValuesDetailsApiModel> request);

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryDeleteModifiedValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteModifiedValuesDetailsApiModel> request);

        /// <summary>
        /// Delete historic values at specified datum
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAtTimesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteValuesAtTimesDetailsApiModel> request);

        /// <summary>
        /// Delete event history
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryDeleteEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteEventsDetailsApiModel> request);
    }
}
