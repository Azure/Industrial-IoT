// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Implements node and publish services through command control against
    /// the OPC twin module receiving service requests via device method calls.
    /// </summary>
    public sealed class TwinClient : IBrowseServices<string>, IHistoricAccessServices<string>,
        INodeServices<string>, IPublishServices<string>, IUploadServices<string> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public TwinClient(IMethodClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(string endpointId,
            PublishStartRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnTwin<PublishStartRequestModel, PublishStartResultModel>(
                "PublishStart_V1", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(string endpointId,
            PublishStopRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnTwin<PublishStopRequestModel, PublishStopResultModel>(
                "PublishStop_V1", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResultModel> NodePublishListAsync(
            string endpointId, PublishedItemListRequestModel request) {
            var result = await CallServiceOnTwin<PublishedItemListRequestModel, PublishedItemListResultModel>(
                "PublishList_V1", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ModelUploadStartResultModel> ModelUploadStartAsync(
            string endpointId, ModelUploadStartRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnTwin<ModelUploadStartRequestModel, ModelUploadStartResultModel>(
                "ModelUploadStart_V1", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(string endpointId,
            BrowseRequestModel request) {
            return await CallServiceOnTwin<BrowseRequestModel, BrowseResultModel>(
                "Browse_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            return await CallServiceOnTwin<BrowseNextRequestModel, BrowseNextResultModel>(
                "BrowseNext_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.BrowsePaths == null || request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Length == 0)) {
                throw new ArgumentNullException(nameof(request.BrowsePaths));
            }
            return await CallServiceOnTwin<BrowsePathRequestModel, BrowsePathResultModel>(
                "BrowsePath_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnTwin<ValueReadRequestModel, ValueReadResultModel>(
                "ValueRead_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Value == null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            return await CallServiceOnTwin<ValueWriteRequestModel, ValueWriteResultModel>(
                "ValueWrite_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnTwin<MethodMetadataRequestModel, MethodMetadataResultModel>(
                "MethodMetadata_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnTwin<MethodCallRequestModel, MethodCallResultModel>(
                "MethodCall_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<ReadResultModel> NodeReadAsync(
            string endpointId, ReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0) {
                throw new ArgumentNullException(nameof(request.Attributes));
            }
            if (request.Attributes.Any(r => string.IsNullOrEmpty(r.NodeId))) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            return await CallServiceOnTwin<ReadRequestModel, ReadResultModel>(
                "NodeRead_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<WriteResultModel> NodeWriteAsync(
            string endpointId, WriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0) {
                throw new ArgumentNullException(nameof(request.Attributes));
            }
            if (request.Attributes.Any(r => string.IsNullOrEmpty(r.NodeId))) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            return await CallServiceOnTwin<WriteRequestModel, WriteResultModel>(
                "NodeWrite_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<JToken>> HistoryReadAsync(
            string endpointId, HistoryReadRequestModel<JToken> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnTwin<HistoryReadRequestModel<JToken>, HistoryReadResultModel<JToken>>(
                "HistoryRead_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<JToken>> HistoryReadNextAsync(
            string endpointId, HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            return await CallServiceOnTwin<HistoryReadNextRequestModel, HistoryReadNextResultModel<JToken>>(
                "HistoryReadNext_V1", endpointId, request);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryUpdateAsync(
            string endpointId, HistoryUpdateRequestModel<JToken> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            return await CallServiceOnTwin<HistoryUpdateRequestModel<JToken>, HistoryUpdateResultModel>(
                "HistoryUpdate_V1", endpointId, request);
        }

        /// <summary>
        /// helper to invoke service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="service"></param>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<R> CallServiceOnTwin<T, R>(string service,
            string endpointId, T request) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var sw = Stopwatch.StartNew();
            var result = await _client.CallMethodAsync(endpointId, null, service,
                JsonConvertEx.SerializeObject(request));
            _logger.Debug("Twin call '{service}' took {elapsed} ms)!",
                service, sw.ElapsedMilliseconds);
            return JsonConvertEx.DeserializeObject<R>(result);
        }

        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
