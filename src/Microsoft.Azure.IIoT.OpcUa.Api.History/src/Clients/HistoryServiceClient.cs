// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Implementation of Historian service api.
    /// </summary>
    public sealed class HistoryServiceClient : IHistoryServiceRawApi, IHistoryServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public HistoryServiceClient(IHttpClient httpClient, IHistoryConfig config,
            ILogger logger) :
            this(httpClient, config.OpcUaHistoryServiceUrl,
                config.OpcUaHistoryServiceResourceId, logger) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        /// <param name="logger"></param>
        public HistoryServiceClient(IHttpClient httpClient, string serviceUri,
            string resourceId, ILogger logger) {
            if (string.IsNullOrEmpty(serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serviceUri = serviceUri;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceId = resourceId;
        }

        /// <inheritdoc/>
        public async Task<StatusResponseApiModel> GetServiceStatusAsync() {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/status",
                _resourceId);
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<StatusResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<JToken>> HistoryReadRawAsync(
            string endpointId, HistoryReadRequestApiModel<JToken> content) {
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
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryReadResponseApiModel<JToken>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseApiModel<JToken>> HistoryReadRawNextAsync(
            string endpointId, HistoryReadNextRequestApiModel content) {
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
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryReadNextResponseApiModel<JToken>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            string endpointId, HistoryUpdateRequestApiModel<JToken> content) {
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
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryUpdateResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadValuesDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/values", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryReadResponseApiModel<HistoricValueApiModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadModifiedValuesDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/values/modified", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryReadResponseApiModel<HistoricValueApiModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadValuesAtTimesDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/values/pick", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryReadResponseApiModel<HistoricValueApiModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId, HistoryReadRequestApiModel<ReadProcessedValuesDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/values/processed", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryReadResponseApiModel<HistoricValueApiModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesNextAsync(
            string endpointId, HistoryReadNextRequestApiModel content) {
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
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryReadNextResponseApiModel<HistoricValueApiModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsAsync(
            string endpointId, HistoryReadRequestApiModel<ReadEventsDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/read/{endpointId}/events", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryReadResponseApiModel<HistoricEventApiModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsNextAsync(
            string endpointId, HistoryReadNextRequestApiModel content) {
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
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryReadNextResponseApiModel<HistoricEventApiModel[]>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryReplaceValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<ReplaceValuesDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/replace/{endpointId}/values", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryUpdateResponseApiModel> ();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryReplaceEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<ReplaceEventsDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/replace/{endpointId}/events", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryUpdateResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryInsertValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<InsertValuesDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/insert/{endpointId}/values", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryUpdateResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryInsertEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<InsertEventsDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/insert/{endpointId}/events", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryUpdateResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteValuesDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/delete/{endpointId}/values", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryUpdateResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAtTimesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteValuesAtTimesDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/delete/{endpointId}/values/pick", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryUpdateResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteModifiedValuesAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteModifiedValuesDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/delete/{endpointId}/values/modified", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryUpdateResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteEventsAsync(string endpointId,
            HistoryUpdateRequestApiModel<DeleteEventsDetailsApiModel> content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/delete/{endpointId}/events", _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<HistoryUpdateResponseApiModel>();
        }

        private const string kContinuationTokenHeaderKey = "x-ms-continuation";
        private const string kPageSizeHeaderKey = "x-ms-max-item-count";
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _serviceUri;
        private readonly string _resourceId;
    }
}
