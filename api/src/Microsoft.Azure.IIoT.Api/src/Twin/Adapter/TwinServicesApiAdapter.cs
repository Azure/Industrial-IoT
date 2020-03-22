// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
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
        /// <param name="serializer"></param>
        public TwinServicesApiAdapter(ITwinServiceApi client, ISerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(
            string endpoint, BrowseRequestModel request) {
            var result = await _client.NodeBrowseFirstAsync(endpoint,
                _serializer.Map<BrowseRequestApiModel>(request));
            return _serializer.Map<BrowseResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            string endpoint, BrowseNextRequestModel request) {
            var result = await _client.NodeBrowseNextAsync(endpoint,
                _serializer.Map<BrowseNextRequestInternalApiModel>(request));
            return _serializer.Map<BrowseNextResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(
            string endpoint, BrowsePathRequestModel request) {
            var result = await _client.NodeBrowsePathAsync(endpoint,
                _serializer.Map<BrowsePathRequestApiModel>(request));
            return _serializer.Map<BrowsePathResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            string endpoint, ValueReadRequestModel request) {
            var result = await _client.NodeValueReadAsync(endpoint,
                _serializer.Map<ValueReadRequestApiModel>(request));
            return _serializer.Map<ValueReadResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            string endpoint, ValueWriteRequestModel request) {
            var result = await _client.NodeValueWriteAsync(endpoint,
                _serializer.Map<ValueWriteRequestApiModel>(request));
            return _serializer.Map<ValueWriteResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string endpoint, MethodMetadataRequestModel request) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint,
                _serializer.Map<MethodMetadataRequestApiModel>(request));
            return _serializer.Map<MethodMetadataResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            string endpoint, MethodCallRequestModel request) {
            var result = await _client.NodeMethodCallAsync(endpoint,
                _serializer.Map<MethodCallRequestApiModel>(request));
            return _serializer.Map<MethodCallResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ReadResultModel> NodeReadAsync(
            string endpoint, ReadRequestModel request) {
            var result = await _client.NodeReadAsync(endpoint,
                _serializer.Map<ReadRequestApiModel>(request));
            return _serializer.Map<ReadResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<WriteResultModel> NodeWriteAsync(
            string endpoint, WriteRequestModel request) {
            var result = await _client.NodeWriteAsync(endpoint,
                _serializer.Map<WriteRequestApiModel>(request));
            return _serializer.Map<WriteResultModel>(result);
        }

        private readonly ISerializer _serializer;
        private readonly ITwinServiceApi _client;
    }
}
