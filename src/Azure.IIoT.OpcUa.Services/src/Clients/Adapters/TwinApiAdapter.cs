// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Clients.Adapters
{
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Models;
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
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(ConnectionModel connection,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            return await _client.NodeBrowseFirstAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(ConnectionModel connection,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            return await _client.NodeBrowseNextAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(ConnectionModel connection,
            BrowseStreamRequestModel request, CancellationToken ct = default)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(ConnectionModel connection,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            return await _client.NodeBrowsePathAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(ConnectionModel connection,
            ValueReadRequestModel request, CancellationToken ct)
        {
            return await _client.NodeValueReadAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(ConnectionModel connection,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            return await _client.NodeValueWriteAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(ConnectionModel connection,
            MethodMetadataRequestModel request, CancellationToken ct)
        {
            return await _client.NodeMethodGetMetadataAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(ConnectionModel connection,
            MethodCallRequestModel request, CancellationToken ct)
        {
            return await _client.NodeMethodCallAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(ConnectionModel connection,
            ReadRequestModel request, CancellationToken ct)
        {
            return await _client.NodeReadAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(ConnectionModel connection,
            WriteRequestModel request, CancellationToken ct)
        {
            return await _client.NodeWriteAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(ConnectionModel connection,
            CancellationToken ct)
        {
            return await _client.GetServerCapabilitiesAsync(connection, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(ConnectionModel connection,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            return await _client.GetMetadataAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection, CancellationToken ct)
        {
            return await _client.HistoryGetServerCapabilitiesAsync(connection, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel connection, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            return await _client.HistoryGetConfigurationAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct)
        {
            return await _client.HistoryReadAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request,
            CancellationToken ct)
        {
            return await _client.HistoryReadNextAsync(connection, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct)
        {
            return await _client.HistoryUpdateAsync(connection, request, ct).ConfigureAwait(false);
        }

        private readonly ITwinApi _client;
    }
}
