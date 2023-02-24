// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Abstractions.Serializers.Extensions;
    using Microsoft.Azure.IIoT.Http;
    using System;
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
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public HistoryServiceClient(IHttpClient httpClient, IServiceApiConfig config,
            ISerializer serializer) :
            this(httpClient, config?.ServiceUrl, serializer)
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public HistoryServiceClient(IHttpClient httpClient, string serviceUri,
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
        public async Task<string> GetServiceStatusAsync(CancellationToken ct)
        {
            var request = _httpClient.NewRequest($"{_serviceUri}/history/healthz", Resource.Platform);
            try
            {
                var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
                response.Validate();
                return response.GetContentAsString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadValuesDetailsModel> content,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/read/{endpointId}/values", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> content,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/read/{endpointId}/values/modified", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> content,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/read/{endpointId}/values/pick", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> content,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/read/{endpointId}/values/processed", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpointId, HistoryReadNextRequestModel content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.ContinuationToken))
            {
                throw new ArgumentNullException(nameof(content.ContinuationToken));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/read/{endpointId}/values/next", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadNextResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpointId, HistoryReadRequestModel<ReadEventsDetailsModel> content,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/read/{endpointId}/events", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricEventModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpointId, HistoryReadNextRequestModel content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.ContinuationToken))
            {
                throw new ArgumentNullException(nameof(content.ContinuationToken));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/read/{endpointId}/events/next", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadNextResponseModel<HistoricEventModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/replace/{endpointId}/values", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/replace/{endpointId}/events", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/insert/{endpointId}/values", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/upsert/{endpointId}/values", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/insert/{endpointId}/events", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/upsert/{endpointId}/events", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/delete/{endpointId}/values", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/delete/{endpointId}/values/pick", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/delete/{endpointId}/values/modified", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> content, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/history/v2/delete/{endpointId}/events", Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        private readonly IHttpClient _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
