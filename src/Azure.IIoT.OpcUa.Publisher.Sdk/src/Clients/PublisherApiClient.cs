// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Rpc;
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
        public PublisherApiClient(IMethodClient methodClient, string target,
            TimeSpan? timeout = null)
        {
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
        public PublisherApiClient(IMethodClient methodClient,
            IOptions<SdkOptions> options) :
            this(methodClient, options.Value.Target!, options.Value.Timeout)
        {
        }

        /// <inheritdoc/>
        public async Task CreateOrUpdateDataSetWriterEntryAsync(PublishedNodesEntryModel entry,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(entry);
            await _methodClient.CallMethodAsync(_target,
                "CreateOrUpdateDataSetWriterEntry", Json.SerializeToMemory(entry),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesEntryModel> GetDataSetWriterEntryAsync(string dataSetWriterGroup,
            string dataSetWriterId, CancellationToken ct)
        {
            ArgumentException.ThrowIfNullOrEmpty(dataSetWriterGroup);
            ArgumentException.ThrowIfNullOrEmpty(dataSetWriterId);
            var response = await _methodClient.CallMethodAsync(_target,
                "GetDataSetWriterEntry", Json.SerializeToMemory(new
                {
                    dataSetWriterGroup,
                    dataSetWriterId
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<PublishedNodesEntryModel>();
        }

        /// <inheritdoc/>
        public async Task AddOrUpdateNodesAsync(string dataSetWriterGroup, string dataSetWriterId,
            IReadOnlyList<OpcNodeModel> opcNodes, string? insertAfterFieldId, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            await _methodClient.CallMethodAsync(_target,
                "AddOrUpdateNodes", Json.SerializeToMemory(new
                {
                    dataSetWriterGroup,
                    dataSetWriterId,
                    opcNodes,
                    insertAfterFieldId
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RemoveNodesAsync(string dataSetWriterGroup, string dataSetWriterId,
            IReadOnlyList<string> dataSetFieldIds, CancellationToken ct)
        {
            ArgumentException.ThrowIfNullOrEmpty(dataSetWriterGroup);
            ArgumentException.ThrowIfNullOrEmpty(dataSetWriterId);
            ArgumentNullException.ThrowIfNull(dataSetFieldIds);
            if (dataSetFieldIds.Count == 0)
            {
                throw new ArgumentException("No fields to remove.", nameof(dataSetFieldIds));
            }
            await _methodClient.CallMethodAsync(_target,
                "RemoveNodes", Json.SerializeToMemory(new
                {
                    dataSetWriterGroup,
                    dataSetWriterId,
                    dataSetFieldIds
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<OpcNodeModel>> GetNodesAsync(string dataSetWriterGroup,
            string dataSetWriterId, string? lastDataSetFieldId, int? pageSize, CancellationToken ct)
        {
            ArgumentException.ThrowIfNullOrEmpty(dataSetWriterGroup);
            ArgumentException.ThrowIfNullOrEmpty(dataSetWriterId);
            var response = await _methodClient.CallMethodAsync(_target,
                "GetNodes", Json.SerializeToMemory(new
                {
                    dataSetWriterGroup,
                    dataSetWriterId,
                    lastDataSetFieldId,
                    pageSize
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<List<OpcNodeModel>>();
        }

        /// <inheritdoc/>
        public async Task RemoveDataSetWriterEntryAsync(string dataSetWriterGroup,
            string dataSetWriterId, CancellationToken ct)
        {
            ArgumentException.ThrowIfNullOrEmpty(dataSetWriterGroup);
            ArgumentException.ThrowIfNullOrEmpty(dataSetWriterId);
            await _methodClient.CallMethodAsync(_target,
                "RemoveDataSetWriterEntry", Json.SerializeToMemory(new
                {
                    dataSetWriterGroup,
                    dataSetWriterId
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseModel> PublishStartAsync(ConnectionModel connection,
            PublishStartRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishStart", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<PublishStartResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> PublishStopAsync(ConnectionModel connection,
            PublishStopRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishStop", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<PublishStopResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> PublishBulkAsync(ConnectionModel connection,
            PublishBulkRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishBulk", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<PublishBulkResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> PublishListAsync(ConnectionModel connection,
            PublishedItemListRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishList", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<PublishedItemListResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> PublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "PublishNodes", Json.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<PublishedNodesResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> UnpublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "UnpublishNodes", Json.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<PublishedNodesResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> UnpublishAllNodesAsync(
            PublishedNodesEntryModel? request, CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
                "UnpublishAllNodes", Json.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<PublishedNodesResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> AddOrUpdateEndpointsAsync(
            IReadOnlyList<PublishedNodesEntryModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
               "AddOrUpdateEndpoints", Json.SerializeToMemory(request),
               ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<PublishedNodesResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredEndpointsResponseModel> GetConfiguredEndpointsAsync(
            GetConfiguredEndpointsRequestModel? request, CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
               "GetConfiguredEndpoints", request == null ? null : Json.SerializeToMemory(request),
               ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<GetConfiguredEndpointsResponseModel>();
        }

        /// <inheritdoc/>
        public async Task SetConfiguredEndpointsAsync(
            SetConfiguredEndpointsRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _methodClient.CallMethodAsync(_target,
               "SetConfiguredEndpoints", Json.SerializeToMemory(request),
               ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredNodesOnEndpointResponseModel> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "GetConfiguredNodesOnEndpoint", Json.SerializeToMemory(request),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<GetConfiguredNodesOnEndpointResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
                "GetDiagnosticInfo", null, ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<List<PublishDiagnosticInfoModel>>();
        }

        /// <inheritdoc/>
        public async Task ShutdownAsync(bool failFast, CancellationToken ct)
        {
            await _methodClient.CallMethodAsync(_target, "Shutdown",
                Json.SerializeToMemory(failFast),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string?> GetServerCertificateAsync(CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
                "GetServerCertificate", null, ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<string?>();
        }

        /// <inheritdoc/>
        public async Task<string?> GetApiKeyAsync(CancellationToken ct)
        {
            var response = await _methodClient.CallMethodAsync(_target,
                "GetApiKey", null, ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<string?>();
        }

        private readonly IMethodClient _methodClient;
        private readonly string _target;
        private readonly TimeSpan _timeout;
    }
}
