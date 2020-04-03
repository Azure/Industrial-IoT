// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Implements node services as adapter on top of supervisor api.
    /// </summary>
    public sealed class TwinSupervisorAdapter : IBrowseServices<EndpointApiModel>,
        INodeServices<EndpointApiModel> {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinSupervisorAdapter(ITwinModuleApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(
            EndpointApiModel endpoint, BrowseRequestModel request) {
            var result = await _client.NodeBrowseFirstAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            EndpointApiModel endpoint, BrowseNextRequestModel request) {
            var result = await _client.NodeBrowseNextAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(
            EndpointApiModel endpoint, BrowsePathRequestModel request) {
            var result = await _client.NodeBrowsePathAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            EndpointApiModel endpoint, ValueReadRequestModel request) {
            var result = await _client.NodeValueReadAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            EndpointApiModel endpoint, ValueWriteRequestModel request) {
            var result = await _client.NodeValueWriteAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            EndpointApiModel endpoint, MethodMetadataRequestModel request) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            EndpointApiModel endpoint, MethodCallRequestModel request) {
            var result = await _client.NodeMethodCallAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ReadResultModel> NodeReadAsync(
            EndpointApiModel endpoint, ReadRequestModel request) {
            var result = await _client.NodeReadAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<WriteResultModel> NodeWriteAsync(
            EndpointApiModel endpoint, WriteRequestModel request) {
            var result = await _client.NodeWriteAsync(endpoint,
                request.ToApiModel());
            return result.ToServiceModel();
        }

        private readonly ITwinModuleApi _client;
    }
}
