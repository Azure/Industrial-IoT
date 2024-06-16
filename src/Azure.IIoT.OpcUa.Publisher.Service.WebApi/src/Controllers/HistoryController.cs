// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly.Extensions.Serializers;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// History raw access services
    /// </summary>
    [ApiVersion("2")]
    [Route("history/v{version:apiVersion}")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        /// <param name="nodes"></param>
        public HistoryController(IHistoryServices<string> historian,
            INodeServices<string> nodes)
        {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        /// <summary>
        /// Get the history server capabilities
        /// </summary>
        /// <remarks>
        /// Gets the capabilities of the connected historian server.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns>Server capabilities</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [HttpGet("capabilities/{endpointId}")]
        public async Task<HistoryServerCapabilitiesModel> GetHistoryServerCapabilitiesAsync(
            string endpointId, [FromQuery] NamespaceFormat? namespaceFormat, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            return await _nodes.HistoryGetServerCapabilitiesAsync(endpointId,
                new RequestHeaderModel { NamespaceFormat = namespaceFormat }, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read history using json details
        /// </summary>
        /// <remarks>
        /// Read node history if available using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <param name="ct"></param>
        /// <returns>The history read response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("history/read/{endpointId}")]
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadRawAsync(
            string endpointId, [FromBody][Required] HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.HistoryReadAsync(endpointId, request,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next batch of history as json
        /// </summary>
        /// <remarks>
        /// Read next batch of node history values using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read next request</param>
        /// <param name="ct"></param>
        /// <returns>The history read response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("history/read/{endpointId}/next")]
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadRawNextAsync(
            string endpointId, [FromBody][Required] HistoryReadNextRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.HistoryReadNextAsync(endpointId, request,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Update node history using raw json
        /// </summary>
        /// <remarks>
        /// Update node history using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <param name="ct"></param>
        /// <returns>The history update result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("history/update/{endpointId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<HistoryUpdateResponseModel> HistoryUpdateRawAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.HistoryUpdateAsync(endpointId, request,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get history node configuration
        /// </summary>
        /// <remarks>
        /// Read history node configuration if available.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history configuration read request</param>
        /// <param name="ct"></param>
        /// <returns>The history node configuration</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("read/{endpointId}/configuration")]
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            string endpointId, HistoryConfigurationRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.HistoryGetConfigurationAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic events
        /// </summary>
        /// <remarks>
        /// Read historic events of a node if available using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <param name="ct"></param>
        /// <returns>The historic events</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("read/{endpointId}/events")]
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryReadEventsAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next batch of historic events
        /// </summary>
        /// <remarks>
        /// Read next batch of historic events of a node using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read next request</param>
        /// <param name="ct"></param>
        /// <returns>The historic events</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("read/{endpointId}/events/next")]
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpointId,
            [FromBody][Required] HistoryReadNextRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryReadEventsNextAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic processed values at specified times
        /// </summary>
        /// <remarks>
        /// Read processed history values of a node if available using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <param name="ct"></param>
        /// <returns>The historic values</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("read/{endpointId}/values")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryReadValuesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic values at specified times
        /// </summary>
        /// <remarks>
        /// Read historic values of a node if available using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <param name="ct"></param>
        /// <returns>The historic values</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("read/{endpointId}/values/pick")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryReadValuesAtTimesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic processed values at specified times
        /// </summary>
        /// <remarks>
        /// Read processed history values of a node if available using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <param name="ct"></param>
        /// <returns>The historic values</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("read/{endpointId}/values/processed")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryReadProcessedValuesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic modified values at specified times
        /// </summary>
        /// <remarks>
        /// Read processed history values of a node if available using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <param name="ct"></param>
        /// <returns>The historic values</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("read/{endpointId}/values/modified")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryReadModifiedValuesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next batch of historic values
        /// </summary>
        /// <remarks>
        /// Read next batch of historic values of a node using historic access.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read next request</param>
        /// <param name="ct"></param>
        /// <returns>The historic values</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("read/{endpointId}/values/next")]
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValueNextAsync(
            string endpointId, [FromBody][Required] HistoryReadNextRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryReadValuesNextAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Replace historic values
        /// </summary>
        /// <remarks>
        /// Replace historic values using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history replace request</param>
        /// <param name="ct"></param>
        /// <returns>The history replace result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("replace/{endpointId}/values")]
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryReplaceValuesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Replace historic events
        /// </summary>
        /// <remarks>
        /// Replace historic events using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history replace request</param>
        /// <param name="ct"></param>
        /// <returns>The history replace result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("replace/{endpointId}/events")]
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryReplaceEventsAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Insert historic values
        /// </summary>
        /// <remarks>
        /// Insert historic values using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history insert request</param>
        /// <param name="ct"></param>
        /// <returns>The history insert result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("insert/{endpointId}/values")]
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryInsertValuesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Insert historic events
        /// </summary>
        /// <remarks>
        /// Insert historic events using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history insert request</param>
        /// <param name="ct"></param>
        /// <returns>The history insert result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("insert/{endpointId}/events")]
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryInsertEventsAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Upsert historic values
        /// </summary>
        /// <remarks>
        /// Upsert historic values using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history upsert request</param>
        /// <param name="ct"></param>
        /// <returns>The history upsert result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("upsert/{endpointId}/values")]
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryUpsertValuesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Upsert historic events
        /// </summary>
        /// <remarks>
        /// Upsert historic events using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history upsert request</param>
        /// <param name="ct"></param>
        /// <returns>The history upsert result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("upsert/{endpointId}/events")]
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryUpsertEventsAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete value history at specified times
        /// </summary>
        /// <remarks>
        /// Delete value history using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <param name="ct"></param>
        /// <returns>The history delete result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("delete/{endpointId}/values/pick")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryDeleteValuesAtTimesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <remarks>
        /// Delete historic values using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <param name="ct"></param>
        /// <returns>The history delete result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("delete/{endpointId}/values")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryDeleteValuesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <remarks>
        /// Delete historic values using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <param name="ct"></param>
        /// <returns>The history delete result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("delete/{endpointId}/values/modified")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryDeleteModifiedValuesAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete historic events
        /// </summary>
        /// <remarks>
        /// Delete historic events using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <param name="ct"></param>
        /// <returns>The history delete result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        [HttpPost("delete/{endpointId}/events")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<DeleteEventsDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _historian.HistoryDeleteEventsAsync(endpointId,
                request, ct).ConfigureAwait(false);
        }

        private readonly IHistoryServices<string> _historian;
        private readonly INodeServices<string> _nodes;
    }
}
