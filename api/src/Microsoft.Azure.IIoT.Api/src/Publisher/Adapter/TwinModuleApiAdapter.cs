// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Publisher.Adapter {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node services as adapter on top of twin api.
    /// </summary>
    public sealed class TwinModuleApiAdapter : IBrowseServices<ConnectionApiModel>,
        INodeServices<ConnectionApiModel> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinModuleApiAdapter(ITwinModuleApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(
            ConnectionApiModel endpoint, BrowseRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeBrowseFirstAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            ConnectionApiModel endpoint, BrowseNextRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeBrowseNextAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(
            ConnectionApiModel endpoint, BrowsePathRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeBrowsePathAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            ConnectionApiModel endpoint, ValueReadRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeValueReadAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            ConnectionApiModel endpoint, ValueWriteRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeValueWriteAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            ConnectionApiModel endpoint, MethodMetadataRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            ConnectionApiModel endpoint, MethodCallRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeMethodCallAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ReadResultModel> NodeReadAsync(
            ConnectionApiModel endpoint, ReadRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeReadAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<WriteResultModel> NodeWriteAsync(
            ConnectionApiModel endpoint, WriteRequestModel request,
            CancellationToken ct) {
            var result = await _client.NodeWriteAsync(endpoint,
                request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        private readonly ITwinModuleApi _client;
    }
}
