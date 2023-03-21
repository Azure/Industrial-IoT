// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of Historian services over http.
    /// </summary>
    public sealed class HistoryServicesRestClient : IHistoryServices<ConnectionModel>
    {
        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public HistoryServicesRestClient(IHttpClientFactory httpClient,
            IOptions<SdkOptions> options, ISerializer serializer) :
            this(httpClient, options?.Value.Target, serializer)
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public HistoryServicesRestClient(IHttpClientFactory httpClient, string serviceUri,
            ISerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _serviceUri = serviceUri.TrimEnd('/');
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/read/first");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricValueModel[]>>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/read/first/modified");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricValueModel[]>>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/read/first/attimes");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricValueModel[]>>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/read/first/processed");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricValueModel[]>>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            ConnectionModel endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/history/values/read/next");
            return await _httpClient.PostAsync<HistoryReadNextResponseModel<HistoricValueModel[]>>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/events/read/first");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricEventModel[]>>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            ConnectionModel endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/history/events/read/next");
            return await _httpClient.PostAsync<HistoryReadNextResponseModel<HistoricEventModel[]>>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/replace");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/events/replace");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/insert");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/upsert");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/events/insert");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/events/upsert");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/delete");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/delete/attimes");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/delete/modified");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(ConnectionModel endpoint,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/events/delete");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/read");
            return _httpClient.PostStreamAsync<HistoricValueModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct);
        }

        public IAsyncEnumerable<HistoricValueModel> HistoryStreamModifiedValuesAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/read/modified");
            return _httpClient.PostStreamAsync<HistoricValueModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct);
        }

        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAtTimesAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/read/attimes");
            return _httpClient.PostStreamAsync<HistoricValueModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct);
        }

        public IAsyncEnumerable<HistoricValueModel> HistoryStreamProcessedValuesAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/values/read/processed");
            return _httpClient.PostStreamAsync<HistoricValueModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct);
        }

        public IAsyncEnumerable<HistoricEventModel> HistoryStreamEventsAsync(ConnectionModel endpoint,
            HistoryReadRequestModel<ReadEventsDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/history/events/read");
            return _httpClient.PostStreamAsync<HistoricEventModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct);
        }

        /// <summary>
        /// Create envelope
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static RequestEnvelope<T> RequestBody<T>(ConnectionModel connection, T request)
        {
            return new RequestEnvelope<T> { Connection = connection, Request = request };
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
