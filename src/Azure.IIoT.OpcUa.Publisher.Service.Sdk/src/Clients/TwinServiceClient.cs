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
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin service api.
    /// </summary>
    public sealed class TwinServiceClient : ITwinServiceApi
    {
        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="options"></param>
        /// <param name="serializers"></param>
        public TwinServiceClient(IHttpClientFactory httpClient,
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
        public TwinServiceClient(IHttpClientFactory httpClient, string serviceUri,
            Func<Task<string?>>? authorization, ISerializer? serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serviceUri = serviceUri.TrimEnd('/');
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authorization = authorization;
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serializer"></param>
        public TwinServiceClient(HttpClient httpClient, ISerializer? serializer = null) :
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
        public async Task<BrowseFirstResponseModel> NodeBrowseFirstAsync(string endpointId,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/twin/v2/browse/{Uri.EscapeDataString(endpointId)}");
            return await _httpClient.PostAsync<BrowseFirstResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/twin/v2/browse/{Uri.EscapeDataString(endpointId)}/next");
            return await _httpClient.PostAsync<BrowseNextResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.BrowsePaths == null || request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Count == 0))
            {
                throw new ArgumentException("Browse paths missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/twin/v2/browse/{Uri.EscapeDataString(endpointId)}/path");
            return await _httpClient.PostAsync<BrowsePathResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> NodeReadAsync(string endpointId,
            ReadRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Attributes == null || request.Attributes.Count == 0)
            {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var uri = new Uri($"{_serviceUri}/twin/v2/read/{Uri.EscapeDataString(endpointId)}/attributes");
            return await _httpClient.PostAsync<ReadResponseModel>(uri, request,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> NodeWriteAsync(string endpointId,
            WriteRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Attributes == null || request.Attributes.Count == 0)
            {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var uri = new Uri($"{_serviceUri}/twin/v2/write/{Uri.EscapeDataString(endpointId)}/attributes");
            return await _httpClient.PostAsync<WriteResponseModel>(uri, request,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/twin/v2/read/{Uri.EscapeDataString(endpointId)}");
            return await _httpClient.PostAsync<ValueReadResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Value is null)
            {
                throw new ArgumentException("Value missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/twin/v2/write/{Uri.EscapeDataString(endpointId)}");
            return await _httpClient.PostAsync<ValueWriteResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/twin/v2/call/{Uri.EscapeDataString(endpointId)}/metadata");
            return await _httpClient.PostAsync<MethodMetadataResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/twin/v2/call/{Uri.EscapeDataString(endpointId)}");
            return await _httpClient.PostAsync<MethodCallResponseModel>
                (uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> NodeGetMetadataAsync(string endpointId,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/twin/v2/metadata/{Uri.EscapeDataString(endpointId)}/node");
            return await _httpClient.PostAsync<NodeMetadataResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(string endpointId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/twin/v2/capabilities/{Uri.EscapeDataString(endpointId)}");
            return await _httpClient.GetAsync<ServerCapabilitiesModel>(uri,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            string endpointId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/history/v2/capabilities/{Uri.EscapeDataString(endpointId)}");
            return await _httpClient.GetAsync<HistoryServerCapabilitiesModel>(uri,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            string endpointId, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.NodeId == null)
            {
                throw new ArgumentException("NodeId missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/history/v2/read/{Uri.EscapeDataString(endpointId)}/configuration");
            return await _httpClient.PostAsync<HistoryConfigurationResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            string endpointId, HistoryReadRequestModel<VariantValue> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/history/v2/history/read/{Uri.EscapeDataString(endpointId)}");
            return await _httpClient.PostAsync<HistoryReadResponseModel<VariantValue>>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
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
            var uri = new Uri($"{_serviceUri}/history/v2/history/read/{Uri.EscapeDataString(endpointId)}/next");
            return await _httpClient.PostAsync<HistoryReadNextResponseModel<VariantValue>>(
                uri, request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            string endpointId, HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/history/v2/history/update/{Uri.EscapeDataString(endpointId)}");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly Func<Task<string?>>? _authorization;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
