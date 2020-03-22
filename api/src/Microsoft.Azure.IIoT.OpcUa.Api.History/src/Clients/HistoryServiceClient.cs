// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Implementation of Historian service api.
    /// </summary>
    public sealed class HistoryServiceClient : IHistoryServiceRawApi, IHistoryServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public HistoryServiceClient(IHttpClient httpClient, IHistoryConfig config,
            ISerializer serializer) : this(httpClient,
                config?.OpcUaHistoryServiceUrl, config?.OpcUaHistoryServiceResourceId,
                serializer) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        /// <param name="serializer"></param>
        public HistoryServiceClient(IHttpClient httpClient, string serviceUri, string resourceId,
            ISerializer serializer = null) {
            if (string.IsNullOrEmpty(serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _serviceUri = serviceUri;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _resourceId = resourceId;
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/healthz",
                _resourceId);
            try {
                var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
                response.Validate();
                return response.GetContentAsString();
            }
            catch (Exception ex) {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<VariantValue>> HistoryReadRawAsync(
            string endpointId, HistoryReadRequestApiModel<VariantValue> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Details == null) {
                throw new ArgumentNullException(nameof(content.Details));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/history/read/{endpointId}", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseApiModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseApiModel<VariantValue>> HistoryReadRawNextAsync(
            string endpointId, HistoryReadNextRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.ContinuationToken)) {
                throw new ArgumentNullException(nameof(content.ContinuationToken));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/history/read/{endpointId}/next", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadNextResponseApiModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            string endpointId, HistoryUpdateRequestApiModel<VariantValue> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Details == null) {
                throw new ArgumentNullException(nameof(content.Details));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/history/update/{endpointId}", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadValuesDetailsApiModel> content,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/values", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseApiModel<HistoricValueApiModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadModifiedValuesDetailsApiModel> content,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/values/modified", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseApiModel<HistoricValueApiModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadValuesAtTimesDetailsApiModel> content,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/values/pick", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseApiModel<HistoricValueApiModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadProcessedValuesDetailsApiModel> content,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/values/processed", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseApiModel<HistoricValueApiModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesNextAsync(
            string endpointId, HistoryReadNextRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.ContinuationToken)) {
                throw new ArgumentNullException(nameof(content.ContinuationToken));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/values/next", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadNextResponseApiModel<HistoricValueApiModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsAsync(
            string endpointId, HistoryReadRequestApiModel<ReadEventsDetailsApiModel> content,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/events", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadResponseApiModel<HistoricEventApiModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsNextAsync(
            string endpointId, HistoryReadNextRequestApiModel content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.ContinuationToken)) {
                throw new ArgumentNullException(nameof(content.ContinuationToken));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/events/next", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryReadNextResponseApiModel<HistoricEventApiModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryReplaceValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<ReplaceValuesDetailsApiModel> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/replace/{endpointId}/values", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryReplaceEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<ReplaceEventsDetailsApiModel> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/replace/{endpointId}/events", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryInsertValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<InsertValuesDetailsApiModel> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/insert/{endpointId}/values", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryInsertEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<InsertEventsDetailsApiModel> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/insert/{endpointId}/events", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteValuesDetailsApiModel> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/delete/{endpointId}/values", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAtTimesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteValuesAtTimesDetailsApiModel> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/delete/{endpointId}/values/pick", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteModifiedValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteModifiedValuesDetailsApiModel> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/delete/{endpointId}/values/modified", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteEventsDetailsApiModel> content, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/delete/{endpointId}/events", _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<HistoryUpdateResponseApiModel>(response);
        }

        private readonly IHttpClient _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
        private readonly string _resourceId;
    }
}
