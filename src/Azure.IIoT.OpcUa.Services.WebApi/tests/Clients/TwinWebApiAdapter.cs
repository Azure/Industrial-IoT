// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter {
    using Azure.IIoT.OpcUa.Shared.Models;
    using Azure.IIoT.OpcUa.Services.Sdk;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Implements node services as adapter on top of api.
    /// </summary>
    public sealed class TwinWebApiAdapter : INodeServices<string> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinWebApiAdapter(ITwinServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            string endpoint, CancellationToken ct) {
            var result = await _client.GetServerCapabilitiesAsync(endpoint, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(
            string endpoint, BrowseFirstRequestModel request, CancellationToken ct) {
            var result = await _client.NodeBrowseFirstAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            string endpoint, BrowseNextRequestModel request, CancellationToken ct) {
            var result = await _client.NodeBrowseNextAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(
            string endpoint, BrowsePathRequestModel request, CancellationToken ct) {
            var result = await _client.NodeBrowsePathAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(string endpoint,
            NodeMetadataRequestModel request, CancellationToken ct) {
            var result = await _client.GetMetadataAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(
            string endpoint, ValueReadRequestModel request, CancellationToken ct) {
            var result = await _client.NodeValueReadAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(
            string endpoint, ValueWriteRequestModel request, CancellationToken ct) {
            var result = await _client.NodeValueWriteAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(
            string endpoint, MethodMetadataRequestModel request, CancellationToken ct) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(
            string endpoint, MethodCallRequestModel request, CancellationToken ct) {
            var result = await _client.NodeMethodCallAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(
            string endpoint, ReadRequestModel request, CancellationToken ct) {
            var result = await _client.NodeReadAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(
            string endpoint, WriteRequestModel request, CancellationToken ct) {
            var result = await _client.NodeWriteAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            string endpoint, CancellationToken ct) {
            var result = await _client.HistoryGetServerCapabilitiesAsync(endpoint, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            string endpoint, HistoryConfigurationRequestModel request, CancellationToken ct) {
            var result = await _client.HistoryGetConfigurationAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(string endpoint,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct) {
            var result = await _client.HistoryReadAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct) {
            var result = await _client.HistoryReadNextAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(string endpoint,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct) {
            var result = await _client.HistoryUpdateAsync(endpoint, request, ct);
            return result;
        }

        private readonly ITwinServiceApi _client;
    }
}
