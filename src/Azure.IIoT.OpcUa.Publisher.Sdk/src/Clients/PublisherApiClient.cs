// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Tunnel;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node and publish services through command control against
    /// the OPC Publihser module.
    /// </summary>
    public sealed class PublisherApiClient : IPublisherApi
    {
        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="target"></param>
        /// <param name="timeout"></param>
        /// <param name="serializer"></param>
        public PublisherApiClient(IMethodClient methodClient, string target,
            TimeSpan? timeout = null, IJsonSerializer? serializer = null)
        {
            _serializer = serializer ??
                new NewtonsoftJsonSerializer();
            _methodClient = methodClient ??
                throw new ArgumentNullException(nameof(methodClient));
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target));
            }
            _target = target;
            _timeout = timeout ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public PublisherApiClient(IMethodClient methodClient,
            IOptions<SdkOptions> options, IJsonSerializer? serializer = null) :
            this(methodClient, options.Value.Target!, options.Value.Timeout,
                serializer)
        {
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseModel> PublishStartAsync(ConnectionModel connection,
            PublishStartRequestModel request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishStart", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<PublishStartResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> PublishStopAsync(ConnectionModel connection,
            PublishStopRequestModel request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishStop", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<PublishStopResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> PublishBulkAsync(ConnectionModel connection,
            PublishBulkRequestModel request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishBulk", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<PublishBulkResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> PublishListAsync(ConnectionModel connection,
            PublishedItemListRequestModel request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishList", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<PublishedItemListResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> PublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishNodes", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<PublishedNodesResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> UnpublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "UnpublishNodes", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<PublishedNodesResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> UnpublishAllNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "UnpublishAllNodes", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<PublishedNodesResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> AddOrUpdateEndpointsAsync(
            List<PublishedNodesEntryModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
               "AddOrUpdateEndpoints", _serializer.SerializeToMemory(request),
               ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<PublishedNodesResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredEndpointsResponseModel> GetConfiguredEndpointsAsync(
            GetConfiguredEndpointsRequestModel? request, CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
               "GetConfiguredEndpoints", request == null ? null : _serializer.SerializeToMemory(request),
               ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<GetConfiguredEndpointsResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task SetConfiguredEndpointsAsync(
            SetConfiguredEndpointsRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            await _methodClient.CallMethodAsync(_target,
               "SetConfiguredEndpoints", _serializer.SerializeToMemory(request),
               ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredNodesOnEndpointResponseModel> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "GetConfiguredNodesOnEndpoint", _serializer.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<GetConfiguredNodesOnEndpointResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
                "GetDiagnosticInfo", null, ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<List<PublishDiagnosticInfoModel>>(response);
        }

        /// <inheritdoc/>
        public async Task ShutdownAsync(bool failFast, CancellationToken ct)
        {
            await _methodClient.CallMethodAsync(_target, "Shutdown",
                _serializer.SerializeToMemory(failFast),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string?> GetServerCertificateAsync(CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
                "GetServerCertificate", null, ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<string?>(response);
        }

        /// <inheritdoc/>
        public async Task<string?> GetApiKeyAsync(CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
                "GetApiKey", null, ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<string?>(response);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _target;
        private readonly TimeSpan _timeout;
    }
}
