// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node services as adapter on top of api.
    /// </summary>
    public sealed class TwinAdapter : IBrowseServices<string>,
        INodeServices<string> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinAdapter(ITwinServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(
            string endpoint, BrowseRequestModel request) {
            var result = await _client.NodeBrowseFirstAsync(endpoint,
                request.Map<BrowseRequestApiModel>());
            return result.Map<BrowseResultModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            string endpoint, BrowseNextRequestModel request) {
            var result = await _client.NodeBrowseNextAsync(endpoint,
                request.Map<BrowseNextRequestApiModel>());
            return result.Map<BrowseNextResultModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(
            string endpoint, BrowsePathRequestModel request) {
            var result = await _client.NodeBrowsePathAsync(endpoint,
                request.Map<BrowsePathRequestApiModel>());
            return result.Map<BrowsePathResultModel>();
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            string endpoint, ValueReadRequestModel request) {
            var result = await _client.NodeValueReadAsync(endpoint,
                request.Map<ValueReadRequestApiModel>());
            return result.Map<ValueReadResultModel>();
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            string endpoint, ValueWriteRequestModel request) {
            var result = await _client.NodeValueWriteAsync(endpoint,
                request.Map<ValueWriteRequestApiModel>());
            return result.Map<ValueWriteResultModel>();
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string endpoint, MethodMetadataRequestModel request) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint,
                request.Map<MethodMetadataRequestApiModel>());
            return result.Map<MethodMetadataResultModel>();
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            string endpoint, MethodCallRequestModel request) {
            var result = await _client.NodeMethodCallAsync(endpoint,
                request.Map<MethodCallRequestApiModel>());
            return result.Map<MethodCallResultModel>();
        }

        /// <inheritdoc/>
        public async Task<ReadResultModel> NodeReadAsync(
            string endpoint, ReadRequestModel request) {
            var result = await _client.NodeReadAsync(endpoint,
                request.Map<ReadRequestApiModel>());
            return result.Map<ReadResultModel>();
        }

        /// <inheritdoc/>
        public async Task<WriteResultModel> NodeWriteAsync(
            string endpoint, WriteRequestModel request) {
            var result = await _client.NodeWriteAsync(endpoint,
                request.Map<WriteRequestApiModel>());
            return result.Map<WriteResultModel>();
        }

        private readonly ITwinServiceApi _client;
    }
}
