// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// <para>
    /// This section lists all OPC UA HDA or Historian related API provided by
    /// OPC Publisher.
    /// </para>
    /// <para>
    /// The method name for all transports other than HTTP (which uses the shown
    /// HTTP methods and resource uris) is the name of the subsection header.
    /// To use the version specific method append "_V1" or "_V2" to the method
    /// name.
    /// </para>
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    [ControllerExceptionFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/history")]
    [ApiController]
    [Authorize]
    public class HistoryController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        public HistoryController(IHistoryServices<ConnectionModel> historian)
        {
            _history = historian ?? throw new ArgumentNullException(nameof(historian));
        }

        /// <summary>
        /// HistoryReplaceEvents
        /// </summary>
        /// <remarks>
        /// Replace events in a timeseries in the historian of the OPC UA server. See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The events to replace with in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("events/replace")]
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReplaceEventsAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryInsertEvents
        /// </summary>
        /// <remarks>
        /// Insert event entries into a specified timeseries of the historian. See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The events to insert into the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("events/insert")]
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryInsertEventsAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryUpsertEvents
        /// </summary>
        /// <remarks>
        /// Upsert events into a time series of the opc server historian.  See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The events to upsert into the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("events/upsert")]
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryUpsertEventsAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryDeleteEvents
        /// </summary>
        /// <remarks>
        /// Delete event entries in a timeseries of the server historian.  See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The events to delete in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("events/delete")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<DeleteEventsDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryDeleteEventsAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryDeleteValuesAtTimes
        /// </summary>
        /// <remarks>
        /// Delete value change entries in a timeseries of the server historian. See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to delete in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/delete/attimes")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryDeleteValuesAtTimesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryDeleteModifiedValues
        /// </summary>
        /// <remarks>
        /// Delete value change entries in a timeseries of the server historian. See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to delete in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/delete/modified")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryDeleteModifiedValuesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryDeleteValues
        /// </summary>
        /// <remarks>
        /// Delete value change entries in a timeseries of the server historian. See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to delete in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/delete")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryDeleteValuesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryReplaceValues
        /// </summary>
        /// <remarks>
        /// Replace value change entries in a timeseries of the server historian. See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to replace with in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/replace")]
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReplaceValuesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryInsertValues
        /// </summary>
        /// <remarks>
        /// Insert value change entries in a timeseries of the server historian. See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to insert into the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/insert")]
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryInsertValuesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryUpsertValues
        /// </summary>
        /// <remarks>
        /// Upsert value change entries in a timeseries of the server historian. See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to upsert into the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/upsert")]
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryUpsertValuesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryReadEvents
        /// </summary>
        /// <remarks>
        /// Read an event timeseries inside the OPC UA server historian. See
        /// <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The events to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The events read from the historian.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("events/read/first")]
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadEventsDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadEventsAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryReadEventsNext
        /// </summary>
        /// <remarks>
        /// Continue reading an event timeseries inside the OPC UA server historian.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The continuation from a previous read request.</param>
        /// <param name="ct"></param>
        /// <returns>The events read from the historian.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("events/read/next")]
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadNextRequestModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadEventsNextAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryReadValues
        /// </summary>
        /// <remarks>
        /// Read a data change timeseries inside the OPC UA server historian.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The values read from the historian.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/read/first")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadValuesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryReadValuesAtTimes
        /// </summary>
        /// <remarks>
        /// Read parts of a timeseries inside the OPC UA server historian.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The values read from the historian.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/read/first/attimes")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadValuesAtTimesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryReadProcessedValues
        /// </summary>
        /// <remarks>
        /// Read processed timeseries data inside the OPC UA server historian.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The processed values read from the historian.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/read/first/processed")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadProcessedValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadProcessedValuesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryReadModifiedValues
        /// </summary>
        /// <remarks>
        /// Read modified changes in a timeseries inside the OPC UA server historian.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The modified values read from the historian.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/read/first/modified")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadModifiedValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadModifiedValuesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryReadValuesNext
        /// </summary>
        /// <remarks>
        /// Continue reading a timeseries inside the OPC UA server historian.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The continuation token from a previous read operation.</param>
        /// <param name="ct"></param>
        /// <returns>The values read from the historian.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/read/next")]
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadNextRequestModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _history.HistoryReadValuesNextAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryStreamValues (only HTTP transport)
        /// </summary>
        /// <remarks>
        /// Read an entire timeseries from an OPC UA server historian as stream.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The values read from the historian as stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/read")]
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamValuesAsync(request.Connection, request.Request, ct);
        }

        /// <summary>
        /// HistoryStreamModifiedValues (only HTTP transport)
        /// </summary>
        /// <remarks>
        /// Read an entire modified series from an OPC UA server historian as stream.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The modified values read from the historian as stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/read/modified")]
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamModifiedValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadModifiedValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamModifiedValuesAsync(request.Connection, request.Request, ct);
        }

        /// <summary>
        /// HistoryStreamValuesAtTimes (only HTTP transport)
        /// </summary>
        /// <remarks>
        /// Read specific timeseries data from an OPC UA server historian as stream.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The values read from the historian as stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/read/attimes")]
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAtTimesAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamValuesAtTimesAsync(request.Connection, request.Request, ct);
        }

        /// <summary>
        /// HistoryStreamProcessedValues (only HTTP transport)
        /// </summary>
        /// <remarks>
        /// Read processed timeseries data from an OPC UA server historian as stream.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The values to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The processed values read from the historian as stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("values/read/processed")]
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamProcessedValuesAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadProcessedValuesDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamProcessedValuesAsync(request.Connection, request.Request, ct);
        }

        /// <summary>
        /// HistoryStreamEvents (only HTTP transport)
        /// </summary>
        /// <remarks>
        /// Read an entire event timeseries from an OPC UA server historian as stream.
        /// See <a href="https://reference.opcfoundation.org/Core/Part11/v104/docs/">
        /// the relevant section of the OPC UA reference specification</a> and
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// respective service documentation</a> for more information.
        /// </remarks>
        /// <param name="request">The events to read in the timeseries.</param>
        /// <param name="ct"></param>
        /// <returns>The events read from the historian as stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("events/read")]
        public IAsyncEnumerable<HistoricEventModel> HistoryStreamEventsAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<ReadEventsDetailsModel>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _history.HistoryStreamEventsAsync(request.Connection, request.Request, ct);
        }

        private readonly IHistoryServices<ConnectionModel> _history;
    }
}
