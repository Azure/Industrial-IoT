// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Services.Adapter {
    using Azure.IIoT.OpcUa.Api.Models;
    using Azure.IIoT.OpcUa.Services.Sdk;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node services as adapter on top of api.
    /// </summary>
    public sealed class TwinWebApiAdapter : IBrowseServices<string>,
        INodeServices<string> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinWebApiAdapter(ITwinServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseModel> NodeBrowseFirstAsync(
            string endpoint, BrowseRequestModel request, CancellationToken ct) {
            var result = await _client.NodeBrowseFirstAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> NodeBrowseNextAsync(
            string endpoint, BrowseNextRequestModel request, CancellationToken ct) {
            var result = await _client.NodeBrowseNextAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> NodeBrowsePathAsync(
            string endpoint, BrowsePathRequestModel request, CancellationToken ct) {
            var result = await _client.NodeBrowsePathAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> NodeValueReadAsync(
            string endpoint, ValueReadRequestModel request, CancellationToken ct) {
            var result = await _client.NodeValueReadAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> NodeValueWriteAsync(
            string endpoint, ValueWriteRequestModel request, CancellationToken ct) {
            var result = await _client.NodeValueWriteAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(
            string endpoint, MethodMetadataRequestModel request, CancellationToken ct) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> NodeMethodCallAsync(
            string endpoint, MethodCallRequestModel request, CancellationToken ct) {
            var result = await _client.NodeMethodCallAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> NodeReadAsync(
            string endpoint, ReadRequestModel request, CancellationToken ct) {
            var result = await _client.NodeReadAsync(endpoint, request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> NodeWriteAsync(
            string endpoint, WriteRequestModel request, CancellationToken ct) {
            var result = await _client.NodeWriteAsync(endpoint, request, ct);
            return result;
        }

        private readonly ITwinServiceApi _client;
    }
}
