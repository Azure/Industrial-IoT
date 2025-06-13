// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Service.Clients.Adapters
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapts historian services to historic access services
    /// </summary>
    public sealed class HistoryApiAdapter : IHistoryServices<ConnectionModel>
    {
        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        public HistoryApiAdapter(IHistoryApi client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct)
        {
            return _client.HistoryDeleteEventsAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            return _client.HistoryDeleteValuesAtTimesAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            return _client.HistoryDeleteModifiedValuesAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            return _client.HistoryDeleteValuesAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct)
        {
            return _client.HistoryReplaceEventsAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct)
        {
            return _client.HistoryReplaceValuesAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct)
        {
            return _client.HistoryInsertEventsAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct)
        {
            return _client.HistoryInsertValuesAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<UpdateEventsDetailsModel> request,
            CancellationToken ct)
        {
            return _client.HistoryUpsertEventsAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<UpdateValuesDetailsModel> request,
            CancellationToken ct)
        {
            return _client.HistoryUpsertValuesAsync(endpoint, request, ct);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct)
        {
            return await _client.HistoryReadEventsAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            ConnectionModel endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            return await _client.HistoryReadEventsNextAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct)
        {
            return await _client.HistoryReadValuesAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            return await _client.HistoryReadValuesAtTimesAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct)
        {
            return await _client.HistoryReadProcessedValuesAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct)
        {
            return await _client.HistoryReadModifiedValuesAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            ConnectionModel endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            return await _client.HistoryReadValuesNextAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadValuesDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamModifiedValuesAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAtTimesAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamProcessedValuesAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricEventModel> HistoryStreamEventsAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadEventsDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        private readonly IHistoryApi _client;
    }
}
