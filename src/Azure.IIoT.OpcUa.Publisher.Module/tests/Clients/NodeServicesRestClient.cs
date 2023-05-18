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
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of node services over http
    /// </summary>
    public sealed class NodeServicesRestClient : INodeServices<ConnectionModel>
    {
        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public NodeServicesRestClient(IHttpClientFactory httpClient,
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
        public NodeServicesRestClient(IHttpClientFactory httpClient, string serviceUri,
            ISerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serviceUri = serviceUri.TrimEnd('/');
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(ConnectionModel endpoint,
            BrowseStreamRequestModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/browse");
            return _httpClient.PostStreamAsync<BrowseStreamChunkModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct);
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(ConnectionModel endpoint,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/browse/first");
            return await _httpClient.PostAsync<BrowseFirstResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(ConnectionModel endpoint,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/browse/next");
            return await _httpClient.PostAsync<BrowseNextResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(ConnectionModel endpoint,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (request.BrowsePaths == null || request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Count == 0))
            {
                throw new ArgumentException("Browse paths missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/browse/path");
            return await _httpClient.PostAsync<BrowsePathResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(ConnectionModel endpoint,
            ReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (request.Attributes == null || request.Attributes.Count == 0)
            {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var uri = new Uri($"{_serviceUri}/v2/read/attributes");
            return await _httpClient.PostAsync<ReadResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(ConnectionModel endpoint,
            WriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (request.Attributes == null || request.Attributes.Count == 0)
            {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var uri = new Uri($"{_serviceUri}/v2/write/attributes");
            return await _httpClient.PostAsync<WriteResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(ConnectionModel endpoint,
            ValueReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/read");
            return await _httpClient.PostAsync<ValueReadResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(ConnectionModel endpoint,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (request.Value is null)
            {
                throw new ArgumentException("Value missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/write");
            return await _httpClient.PostAsync<ValueWriteResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(
            ConnectionModel endpoint, MethodMetadataRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/call/$metadata");
            return await _httpClient.PostAsync<MethodMetadataResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(
            ConnectionModel endpoint, MethodCallRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/call");
            return await _httpClient.PostAsync<MethodCallResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(ConnectionModel endpoint,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/metadata");
            return await _httpClient.PostAsync<NodeMetadataResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<QueryCompilationResponseModel> CompileQueryAsync(ConnectionModel endpoint,
            QueryCompilationRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/query/compile");
            return await _httpClient.PostAsync<QueryCompilationResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(ConnectionModel endpoint,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            var uri = new Uri($"{_serviceUri}/v2/capabilities");
            return await _httpClient.PostAsync<ServerCapabilitiesModel>(uri,
                endpoint, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel endpoint, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            var uri = new Uri($"{_serviceUri}/v2/history/capabilities");
            return await _httpClient.PostAsync<HistoryServerCapabilitiesModel>(uri,
                endpoint, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel endpoint, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (request.NodeId == null)
            {
                throw new ArgumentException("NodeId missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/history/configuration");
            return await _httpClient.PostAsync<HistoryConfigurationResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<VariantValue> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/historyread/first");
            return await _httpClient.PostAsync<HistoryReadResponseModel<VariantValue>>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}v2/historyread/next");
            return await _httpClient.PostAsync<HistoryReadNextResponseModel<VariantValue>>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/historyupdate");
            return await _httpClient.PostAsync<HistoryUpdateResponseModel>(uri,
                RequestBody(endpoint, request), _serializer, ct: ct).ConfigureAwait(false);
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
