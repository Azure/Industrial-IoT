// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node and publish services as adapter on top of api.
    /// </summary>
    public sealed class TwinAdapter : IBrowseServices<string>,
        INodeServices<string>, IPublishServices<string> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        public TwinAdapter(ITwinServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(
            string endpoint, BrowseRequestModel request) {
            var result = await _client.NodeBrowseFirstAsync(endpoint,
                Map<BrowseRequestApiModel>(request));
            return Map<BrowseResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            string endpoint, BrowseNextRequestModel request) {
            var result = await _client.NodeBrowseNextAsync(endpoint,
                Map<BrowseNextRequestApiModel>(request));
            return Map<BrowseNextResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(
            string endpoint, BrowsePathRequestModel request) {
            var result = await _client.NodeBrowsePathAsync(endpoint,
                Map<BrowsePathRequestApiModel>(request));
            return Map<BrowsePathResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            string endpoint, ValueReadRequestModel request) {
            var result = await _client.NodeValueReadAsync(endpoint,
                Map<ValueReadRequestApiModel>(request));
            return Map<ValueReadResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            string endpoint, ValueWriteRequestModel request) {
            var result = await _client.NodeValueWriteAsync(endpoint,
                Map<ValueWriteRequestApiModel>(request));
            return Map<ValueWriteResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string endpoint, MethodMetadataRequestModel request) {
            var result = await _client.NodeMethodGetMetadataAsync(endpoint,
                Map<MethodMetadataRequestApiModel>(request));
            return Map<MethodMetadataResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            string endpoint, MethodCallRequestModel request) {
            var result = await _client.NodeMethodCallAsync(endpoint,
                Map<MethodCallRequestApiModel>(request));
            return Map<MethodCallResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ReadResultModel> NodeReadAsync(
            string endpoint, ReadRequestModel request) {
            var result = await _client.NodeReadAsync(endpoint,
                Map<ReadRequestApiModel>(request));
            return Map<ReadResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<WriteResultModel> NodeWriteAsync(
            string endpoint, WriteRequestModel request) {
            var result = await _client.NodeWriteAsync(endpoint,
                Map<WriteRequestApiModel>(request));
            return Map<WriteResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            string endpoint, PublishStartRequestModel request) {
            var result = await _client.NodePublishStartAsync(endpoint,
                Map<PublishStartRequestApiModel>(request));
            return Map<PublishStartResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(
            string endpoint, PublishStopRequestModel request) {
            var result = await _client.NodePublishStopAsync(endpoint,
                Map<PublishStopRequestApiModel>(request));
            return Map<PublishStopResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResultModel> NodePublishListAsync(
            string endpoint, PublishedItemListRequestModel request) {
            var result = await _client.NodePublishListAsync(endpoint,
                Map<PublishedItemListRequestApiModel>(request));
            return Map<PublishedItemListResultModel>(result);
        }

        /// <summary>
        /// Convert from to
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        private static T Map<T>(object model) {
            return JsonConvertEx.DeserializeObject<T>(
                JsonConvertEx.SerializeObject(model));
        }

        private readonly ITwinServiceApi _client;
    }
}
