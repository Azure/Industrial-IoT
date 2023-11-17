// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of Historian service api.
    /// </summary>
    public sealed class HistoryServiceClient : IHistoryServiceApi
    {
        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="options"></param>
        /// <param name="serializers"></param>
        public HistoryServiceClient(IHttpClientFactory httpClient,
            IOptions<ServiceSdkOptions> options, IEnumerable<ISerializer> serializers) :
            this(httpClient, options.Value.ServiceUrl!, options.Value.TokenProvider,
                serializers.Resolve(options.Value))
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="authorization"></param>
        /// <param name="serializer"></param>
        public HistoryServiceClient(IHttpClientFactory httpClient, string serviceUri,
            Func<Task<string?>>? authorization, ISerializer? serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _serviceUri = serviceUri.TrimEnd('/');
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authorization = authorization;
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serializer"></param>
        public HistoryServiceClient(HttpClient httpClient, ISerializer? serializer = null) :
            this(httpClient.ToHttpClientFactory(), httpClient.BaseAddress?.ToString()!,
                null, serializer)
        {
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct)
        {
            using var httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri($"{_serviceUri}/healthz")
            };
            try
            {
                using var response = await _httpClient.GetAsync(httpRequest,
                    authorization: _authorization, ct: ct).ConfigureAwait(false);
                response.ValidateResponse();
                return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/read/{Uri.EscapeDataString(endpointId)}/values");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricValueModel[]>>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/read/{Uri.EscapeDataString(endpointId)}/values/modified");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricValueModel[]>>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/read/{Uri.EscapeDataString(endpointId)}/values/pick");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricValueModel[]>>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/read/{Uri.EscapeDataString(endpointId)}/values/processed");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricValueModel[]>>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpointId, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/history/v2/read/{Uri.EscapeDataString(endpointId)}/values/next");
            return await _httpClient.PostAsync<HistoryReadNextResponseModel<HistoricValueModel[]>>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpointId, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/read/{Uri.EscapeDataString(endpointId)}/events");
            return await _httpClient.PostAsync<HistoryReadResponseModel<HistoricEventModel[]>>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpointId, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/history/v2/read/{Uri.EscapeDataString(endpointId)}/events/next");
            return await _httpClient.PostAsync<HistoryReadNextResponseModel<HistoricEventModel[]>>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/replace/{Uri.EscapeDataString(endpointId)}/values");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/replace/{Uri.EscapeDataString(endpointId)}/events");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/insert/{Uri.EscapeDataString(endpointId)}/values");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/upsert/{Uri.EscapeDataString(endpointId)}/values");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/insert/{Uri.EscapeDataString(endpointId)}/events");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/upsert/{Uri.EscapeDataString(endpointId)}/events");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/delete/{Uri.EscapeDataString(endpointId)}/values");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/delete/{Uri.EscapeDataString(endpointId)}/values/pick");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/delete/{Uri.EscapeDataString(endpointId)}/values/modified");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/history/v2/delete/{Uri.EscapeDataString(endpointId)}/events");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly Func<Task<string?>>? _authorization;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
