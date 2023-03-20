// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// History methods controller
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    [ControllerExceptionFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/history")]
    [ApiController]
    public class HistoryMethodsController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        public HistoryMethodsController(IHistoryServices<ConnectionModel> historian)
        {
            _history = historian ?? throw new ArgumentNullException(nameof(historian));
        }

        /// <summary>
        /// Replace events
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("events/replace")]
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(
            RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReplaceEventsAsync(
                request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Insert events
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("events/insert")]
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(
            RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryInsertEventsAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Upsert events
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("events/upsert")]
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(
            RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryUpsertEventsAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete events
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("events/delete")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(
            RequestEnvelope<HistoryUpdateRequestModel<DeleteEventsDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryDeleteEventsAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete values at specified times
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/delete/attimes")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryDeleteValuesAtTimesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete modified values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/delete/modified")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryDeleteModifiedValuesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/delete")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(
            RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryDeleteValuesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Replace values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/replace")]
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(
            RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReplaceValuesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Insert values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/insert")]
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(
            RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryInsertValuesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Upsert values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/upsert")]
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(
            RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryUpsertValuesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic events
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("events/read/first")]
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadEventsDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadEventsAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next set of events
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("events/read/next")]
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            RequestEnvelope<HistoryReadNextRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadEventsNextAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/read/first")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadValuesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic values at times
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/read/first/attimes")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadValuesAtTimesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read processed historic values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/read/first/processed")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadProcessedValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadProcessedValuesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read modified values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/read/first/modified")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadModifiedValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadModifiedValuesAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next set of historic values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/read/next")]
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            RequestEnvelope<HistoryReadNextRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadValuesNextAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Stream values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/read")]
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamValuesAsync(request.Connection, request.Request);
        }

        /// <summary>
        /// Stream modified historic values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/read/modified")]
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamModifiedValuesAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadModifiedValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamModifiedValuesAsync(request.Connection, request.Request);
        }

        /// <summary>
        /// Stream historic values at times
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/read/attimes")]
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAtTimesAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamValuesAtTimesAsync(request.Connection, request.Request);
        }

        /// <summary>
        /// Stream processed historic values
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("values/read/processed")]
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamProcessedValuesAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadProcessedValuesDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamProcessedValuesAsync(request.Connection, request.Request);
        }

        /// <summary>
        /// Stream modified historic events
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("events/read")]
        public IAsyncEnumerable<HistoricEventModel> HistoryStreamEventsAsync(
            RequestEnvelope<HistoryReadRequestModel<ReadEventsDetailsModel>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamEventsAsync(request.Connection, request.Request);
        }

        private readonly IHistoryServices<ConnectionModel> _history;
    }
}
