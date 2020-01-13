// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC Twin Historic Access service api functions
    /// </summary>
    public interface IHistoryServiceApi {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<string> GetServiceStatusAsync(CancellationToken ct = default);

        /// <summary>
        /// Read raw historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadValuesDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read modified historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadModifiedValuesDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read historic values at specific datum
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadValuesAtTimesDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read processed historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadProcessedValuesDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read next set of historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesNextAsync(
            string endpointId, HistoryReadNextRequestApiModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Replace historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryReplaceValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<ReplaceValuesDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Insert historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryInsertValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<InsertValuesDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteValuesDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryDeleteModifiedValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteModifiedValuesDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete historic values at specified datum
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAtTimesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteValuesAtTimesDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read event history
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsAsync(
            string endpointId, HistoryReadRequestApiModel<ReadEventsDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Read next set of historic events
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsNextAsync(
            string endpointId, HistoryReadNextRequestApiModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Replace historic events
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryReplaceEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<ReplaceEventsDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Insert historic events
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryInsertEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<InsertEventsDetailsApiModel> request,
            CancellationToken ct = default);

        /// <summary>
        /// Delete event history
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseApiModel> HistoryDeleteEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteEventsDetailsApiModel> request,
            CancellationToken ct = default);
    }
}
