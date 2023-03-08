// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Tunnel;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Furly;

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
        /// <param name="serializer"></param>
        public PublisherApiClient(IMethodClient methodClient, string target,
             IJsonSerializer serializer = null)
        {
            _serializer = serializer ??
                new NewtonsoftJsonSerializer();
            _methodClient = methodClient ??
                throw new ArgumentNullException(nameof(methodClient));
            _target = target;
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public PublisherApiClient(IMethodClient methodClient,
            ISdkConfig config = null, IJsonSerializer serializer = null) :
            this(methodClient, config?.Target, serializer)
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
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<PublishStartResponseModel>(response);
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
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<PublishStopResponseModel>(response);
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
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<PublishBulkResponseModel>(response);
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
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<PublishedItemListResponseModel>(response);
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
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<PublishedNodesResponseModel>(response);
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
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<PublishedNodesResponseModel>(response);
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
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<PublishedNodesResponseModel>(response);
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
               ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<PublishedNodesResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredEndpointsResponseModel> GetConfiguredEndpointsAsync(
            CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
               "GetConfiguredEndpoints", null, ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<GetConfiguredEndpointsResponseModel>(response);
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
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<GetConfiguredNodesOnEndpointResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
                "GetDiagnosticInfo", null, ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(response);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _target;
    }
}
