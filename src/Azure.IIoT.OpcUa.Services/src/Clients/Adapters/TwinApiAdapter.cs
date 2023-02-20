// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter {
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node services as adapter on top of twin api.
    /// </summary>
    public sealed class TwinApiAdapter : INodeServices<ConnectionModel> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinApiAdapter(ITwinApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(
            ConnectionModel endpoint, BrowseFirstRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeBrowseFirstAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            ConnectionModel endpoint, BrowseNextRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeBrowseNextAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(
            ConnectionModel endpoint, BrowsePathRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeBrowsePathAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(
            ConnectionModel endpoint, ValueReadRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeValueReadAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(
            ConnectionModel endpoint, ValueWriteRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeValueWriteAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(
            ConnectionModel endpoint, MethodMetadataRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(
            ConnectionModel endpoint, MethodCallRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeMethodCallAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(
            ConnectionModel endpoint, ReadRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeReadAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(
            ConnectionModel endpoint, WriteRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeWriteAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(ConnectionModel connection,
            CancellationToken ct) {
            var result = await _client.GetServerCapabilitiesAsync(connection, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(ConnectionModel connection,
            NodeMetadataRequestModel request, CancellationToken ct) {
            var result = await _client.GetMetadataAsync(connection, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection, CancellationToken ct) {
            var result = await _client.HistoryGetServerCapabilitiesAsync(connection, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel connection, HistoryConfigurationRequestModel request, CancellationToken ct) {
            var result = await _client.HistoryGetConfigurationAsync(connection, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct) {
            var result = await _client.HistoryReadAsync(connection, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request,
            CancellationToken ct) {
            var result = await _client.HistoryReadNextAsync(connection, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct) {
            var result = await _client.HistoryUpdateAsync(connection, request, ct);
            return result;
        }

        private readonly ITwinApi _client;
    }
}
