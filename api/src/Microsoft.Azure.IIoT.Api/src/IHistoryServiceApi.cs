// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api {
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
#if ZOMBIE
#if ZOMBIE

        /// <summary>
        /// Read raw historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Read modified historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Read historic values at specific datum
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Read processed historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct = default);
#endif

        /// <summary>
        /// Read next set of historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpointId, HistoryReadNextRequestModel request,
            CancellationToken ct = default);
#endif
#if ZOMBIE
#if ZOMBIE

        /// <summary>
        /// Replace historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(string endpointId,
            HistoryUpdateRequestModel<ReplaceValuesDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Insert historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(string endpointId,
            HistoryUpdateRequestModel<InsertValuesDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Delete historic values at specified datum
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Read event history
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpointId, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct = default);
#endif

        /// <summary>
        /// Read next set of historic events
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpointId, HistoryReadNextRequestModel request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Replace historic events
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(string endpointId,
            HistoryUpdateRequestModel<ReplaceEventsDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Insert historic events
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(string endpointId,
            HistoryUpdateRequestModel<InsertEventsDetailsModel> request,
            CancellationToken ct = default);
#endif
#if ZOMBIE

        /// <summary>
        /// Delete event history
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request,
            CancellationToken ct = default);
#endif
    }
}
