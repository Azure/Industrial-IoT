// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Implements node and publish services through command control against
    /// the OPC twin module receiving service requests via device method calls.
    /// </summary>
    public sealed class TwinModuleControlClient : IBrowseServices<string>, IHistoricAccessServices<string>,
        INodeServices<string> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public TwinModuleControlClient(IMethodClient client, IJsonSerializer serializer,
            ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(string endpointId,
            BrowseRequestModel request) {
            var result = await CallServiceOnTwinAsync<BrowseRequestModel, BrowseResultModel>(
                "Browse_V2", endpointId, request);
            return result;
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
            var result = await CallServiceOnTwinAsync<BrowseNextRequestModel, BrowseNextResultModel>(
                "BrowseNext_V2", endpointId, request);
            return result;
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
            var result = await CallServiceOnTwinAsync<BrowsePathRequestModel, BrowsePathResultModel>(
                "BrowsePath_V2", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnTwinAsync<ValueReadRequestModel, ValueReadResultModel>(
                "ValueRead_V2", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Value is null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            var result = await CallServiceOnTwinAsync<ValueWriteRequestModel, ValueWriteResultModel>(
                "ValueWrite_V2", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnTwinAsync<MethodMetadataRequestModel, MethodMetadataResultModel>(
                "MethodMetadata_V2", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnTwinAsync<MethodCallRequestModel, MethodCallResultModel>(
                "MethodCall_V2", endpointId, request);
            return result;
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
            var result = await CallServiceOnTwinAsync<ReadRequestModel, ReadResultModel>(
                "NodeRead_V2", endpointId, request);
            return result;
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
            var result = await CallServiceOnTwinAsync<WriteRequestModel, WriteResultModel>(
                "NodeWrite_V2", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<VariantValue>> HistoryReadAsync(
            string endpointId, HistoryReadRequestModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnTwinAsync<HistoryReadRequestModel<VariantValue>, HistoryReadResultModel<VariantValue>>(
                "HistoryRead_V2", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<VariantValue>> HistoryReadNextAsync(
            string endpointId, HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var result = await CallServiceOnTwinAsync<HistoryReadNextRequestModel, HistoryReadNextResultModel<VariantValue>>(
                "HistoryReadNext_V2", endpointId, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryUpdateAsync(
            string endpointId, HistoryUpdateRequestModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            var result = await CallServiceOnTwinAsync<HistoryUpdateRequestModel<VariantValue>, HistoryUpdateResultModel>(
                "HistoryUpdate_V2", endpointId, request);
            return result;
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
        private async Task<R> CallServiceOnTwinAsync<T, R>(string service,
            string endpointId, T request) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var sw = Stopwatch.StartNew();
            var result = await _client.CallMethodAsync(endpointId, null, service,
                _serializer.SerializeToString(request));
            _logger.Debug("Twin call '{service}' took {elapsed} ms)!",
                service, sw.ElapsedMilliseconds);
            return _serializer.Deserialize<R>(result);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
