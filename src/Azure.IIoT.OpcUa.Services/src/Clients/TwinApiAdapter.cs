﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter {
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Shared.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node services as adapter on top of twin api.
    /// </summary>
    public sealed class TwinApiAdapter : IBrowseServices<ConnectionModel>,
        INodeServices<ConnectionModel> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinApiAdapter(ITwinApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseModel> NodeBrowseFirstAsync(
            ConnectionModel endpoint, BrowseRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeBrowseFirstAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> NodeBrowseNextAsync(
            ConnectionModel endpoint, BrowseNextRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeBrowseNextAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> NodeBrowsePathAsync(
            ConnectionModel endpoint, BrowsePathRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeBrowsePathAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> NodeValueReadAsync(
            ConnectionModel endpoint, ValueReadRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeValueReadAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> NodeValueWriteAsync(
            ConnectionModel endpoint, ValueWriteRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeValueWriteAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(
            ConnectionModel endpoint, MethodMetadataRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> NodeMethodCallAsync(
            ConnectionModel endpoint, MethodCallRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeMethodCallAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> NodeReadAsync(
            ConnectionModel endpoint, ReadRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeReadAsync(endpoint,
                request, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> NodeWriteAsync(
            ConnectionModel endpoint, WriteRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeWriteAsync(endpoint,
                request, ct);
            return result;
        }

        private readonly ITwinApi _client;
    }
}
