// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Service.Clients.Adapters
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node services as adapter on top of twin api.
    /// </summary>
    public sealed class TwinApiAdapter : INodeServices<ConnectionModel>
    {
        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinApiAdapter(ITwinApi client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(ConnectionModel endpoint,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            return await _client.NodeBrowseFirstAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(ConnectionModel endpoint,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            return await _client.NodeBrowseNextAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(ConnectionModel endpoint,
            BrowseStreamRequestModel request, CancellationToken ct = default)
        {
            // TODO
            throw new NotSupportedException("Browse stream is not supported");
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(ConnectionModel endpoint,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            return await _client.NodeBrowsePathAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(ConnectionModel endpoint,
            ValueReadRequestModel request, CancellationToken ct)
        {
            return await _client.NodeValueReadAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(ConnectionModel endpoint,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            return await _client.NodeValueWriteAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(ConnectionModel endpoint,
            MethodMetadataRequestModel request, CancellationToken ct)
        {
            return await _client.NodeMethodGetMetadataAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(ConnectionModel endpoint,
            MethodCallRequestModel request, CancellationToken ct)
        {
            return await _client.NodeMethodCallAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(ConnectionModel endpoint,
            ReadRequestModel request, CancellationToken ct)
        {
            return await _client.NodeReadAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(ConnectionModel endpoint,
            WriteRequestModel request, CancellationToken ct)
        {
            return await _client.NodeWriteAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(ConnectionModel endpoint,
            RequestHeaderModel? header, CancellationToken ct)
        {
            return await _client.GetServerCapabilitiesAsync(endpoint, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(ConnectionModel endpoint,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            return await _client.GetMetadataAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<QueryCompilationResponseModel> CompileQueryAsync(ConnectionModel endpoint,
            QueryCompilationRequestModel request, CancellationToken ct)
        {
            return await _client.CompileQueryAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel endpoint, RequestHeaderModel? header, CancellationToken ct)
        {
            return await _client.HistoryGetServerCapabilitiesAsync(endpoint, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel endpoint, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            return await _client.HistoryGetConfigurationAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel endpoint, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct)
        {
            return await _client.HistoryReadAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel endpoint, HistoryReadNextRequestModel request,
            CancellationToken ct)
        {
            return await _client.HistoryReadNextAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel endpoint, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct)
        {
            return await _client.HistoryUpdateAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        private readonly ITwinApi _client;
    }
}
