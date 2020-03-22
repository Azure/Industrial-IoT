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
        /// <param name="serializer"></param>
        public TwinSupervisorAdapter(ITwinModuleApi client, ISerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(
            EndpointApiModel endpoint, BrowseRequestModel request) {
            var result = await _client.NodeBrowseFirstAsync(endpoint,
                _serializer.Map<BrowseRequestApiModel>(request));
            return _serializer.Map<BrowseResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            EndpointApiModel endpoint, BrowseNextRequestModel request) {
            var result = await _client.NodeBrowseNextAsync(endpoint,
                _serializer.Map<BrowseNextRequestInternalApiModel>(request));
            return _serializer.Map<BrowseNextResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(
            EndpointApiModel endpoint, BrowsePathRequestModel request) {
            var result = await _client.NodeBrowsePathAsync(endpoint,
                _serializer.Map<BrowsePathRequestApiModel>(request));
            return _serializer.Map<BrowsePathResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            EndpointApiModel endpoint, ValueReadRequestModel request) {
            var result = await _client.NodeValueReadAsync(endpoint,
                _serializer.Map<ValueReadRequestApiModel>(request));
            return _serializer.Map<ValueReadResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            EndpointApiModel endpoint, ValueWriteRequestModel request) {
            var result = await _client.NodeValueWriteAsync(endpoint,
                _serializer.Map<ValueWriteRequestApiModel>(request));
            return _serializer.Map<ValueWriteResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            EndpointApiModel endpoint, MethodMetadataRequestModel request) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint,
                _serializer.Map<MethodMetadataRequestApiModel>(request));
            return _serializer.Map<MethodMetadataResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            EndpointApiModel endpoint, MethodCallRequestModel request) {
            var result = await _client.NodeMethodCallAsync(endpoint,
                _serializer.Map<MethodCallRequestApiModel>(request));
            return _serializer.Map<MethodCallResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ReadResultModel> NodeReadAsync(
            EndpointApiModel endpoint, ReadRequestModel request) {
            var result = await _client.NodeReadAsync(endpoint,
                _serializer.Map<ReadRequestApiModel>(request));
            return _serializer.Map<ReadResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<WriteResultModel> NodeWriteAsync(
            EndpointApiModel endpoint, WriteRequestModel request) {
            var result = await _client.NodeWriteAsync(endpoint,
                _serializer.Map<WriteRequestApiModel>(request));
            return _serializer.Map<WriteResultModel>(result);
        }

        private readonly ISerializer _serializer;
        private readonly ITwinModuleApi _client;
    }
}
