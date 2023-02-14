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
    /// Implements node services as adapter on top of api.
    /// </summary>
    public sealed class TwinServicesApiAdapter : IBrowseServices<string>,
        INodeServices<string> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinServicesApiAdapter(ITwinServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(
            string endpoint, BrowseRequestModel request, CancellationToken ct) {
            var result = await _client.NodeBrowseFirstAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            string endpoint, BrowseNextRequestModel request, CancellationToken ct) {
            var result = await _client.NodeBrowseNextAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(
            string endpoint, BrowsePathRequestModel request, CancellationToken ct) {
            var result = await _client.NodeBrowsePathAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            string endpoint, ValueReadRequestModel request, CancellationToken ct) {
            var result = await _client.NodeValueReadAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            string endpoint, ValueWriteRequestModel request, CancellationToken ct) {
            var result = await _client.NodeValueWriteAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string endpoint, MethodMetadataRequestModel request, CancellationToken ct) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            string endpoint, MethodCallRequestModel request, CancellationToken ct) {
            var result = await _client.NodeMethodCallAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ReadResultModel> NodeReadAsync(
            string endpoint, ReadRequestModel request, CancellationToken ct) {
            var result = await _client.NodeReadAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<WriteResultModel> NodeWriteAsync(
            string endpoint, WriteRequestModel request, CancellationToken ct) {
            var result = await _client.NodeWriteAsync(endpoint, request.ToApiModel(), ct);
            return result.ToServiceModel();
        }

        private readonly ITwinServiceApi _client;
    }
}
