// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Controllers {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Filters;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.Serializers;
    using Prometheus;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint methods controller
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [ExceptionsFilter]
    public class EndpointMethodsController : IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browse"></param>
        /// <param name="nodes"></param>
        /// <param name="historian"></param>
        /// <param name="twin"></param>
        public EndpointMethodsController(IBrowseServices<EndpointModel> browse,
            INodeServices<EndpointModel> nodes, IHistoricAccessServices<EndpointModel> historian,
            ITwinServices twin) {
            _browse = browse ?? throw new ArgumentNullException(nameof(browse));
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseResponseApiModel> BrowseAsync(
            BrowseRequestInternalApiModel request) {
            kBrowseAsync.Inc();
            BrowseResponseApiModel model;
            using (kBrowseHistAsync.NewTimer()) {
                if (request == null) {
                    throw new ArgumentNullException(nameof(request));
                }
                var result = await _browse.NodeBrowseFirstAsync(
                    await _twin.GetEndpointAsync(), request.ToServiceModel());
                model = result.ToApiModel();
            }
            return model;
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseNextResponseApiModel> BrowseNextAsync(
            BrowseNextRequestInternalApiModel request) {
            kBrowseNextAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _browse.NodeBrowseNextAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowsePathResponseApiModel> BrowsePathAsync(
            BrowsePathRequestInternalApiModel request) {
            kBrowsePathAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _browse.NodeBrowsePathAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Read
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResponseApiModel> ValueReadAsync(
            ValueReadRequestApiModel request) {
            kValueReadAsync.Inc();
            ValueReadResponseApiModel model;
            using (kValueReadHistAsync.NewTimer()) {
                if (request == null) {
                    throw new ArgumentNullException(nameof(request));
                }
                var result = await _nodes.NodeValueReadAsync(
                    await _twin.GetEndpointAsync(), request.ToServiceModel());
                model = result.ToApiModel();
            }
            return model;
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResponseApiModel> ValueWriteAsync(
            ValueWriteRequestApiModel request) {
            kValueWriteAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeValueWriteAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Get meta data
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResponseApiModel> MethodMetadataAsync(
            MethodMetadataRequestApiModel request) {
            kMethodMetadataAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeMethodGetMetadataAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Call
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResponseApiModel> MethodCallAsync(
            MethodCallRequestApiModel request) {
            kMethodCallAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeMethodCallAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Read attributes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ReadResponseApiModel> NodeReadAsync(
            ReadRequestApiModel request) {
            kNodeReadAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeReadAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Write attributes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<WriteResponseApiModel> NodeWriteAsync(
            WriteRequestApiModel request) {
            kNodeWriteAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeWriteAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponseApiModel<VariantValue>> HistoryReadAsync(
            HistoryReadRequestApiModel<VariantValue> request) {
            kHistoryReadAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryReadAsync(
               await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Read next history
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadNextResponseApiModel<VariantValue>> HistoryReadNextAsync(
            HistoryReadNextRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryReadNextAsync(
               await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Update history
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateAsync(
            HistoryUpdateRequestApiModel<VariantValue> request) {
            kHistoryUpdateAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryUpdateAsync(
               await _twin.GetEndpointAsync(), request.ToServiceModel());
            return result.ToApiModel();
        }

        private readonly IBrowseServices<EndpointModel> _browse;
        private readonly IHistoricAccessServices<EndpointModel> _historian;
        private readonly INodeServices<EndpointModel> _nodes;
        private readonly ITwinServices _twin;
        private static readonly string kTwinMetricsPrefix = "iiot_edge_twin_";
        private static readonly Counter kBrowseAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "browse", "call to browseAsync");
        private static readonly Histogram kBrowseHistAsync = Metrics
            .CreateHistogram(kTwinMetricsPrefix + "browse_hist", "call to browseAsync");
        private static readonly Counter kBrowseNextAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "browse_next", "call to browseNextAsync");
        private static readonly Counter kBrowsePathAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "browse_path", "call to browsePathAsync");
        private static readonly Counter kHistoryReadAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "history_read", "call to historyReadAsync");
        private static readonly Counter kHistoryUpdateAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "history_update", "call to historyUpdateAsync");
        private static readonly Counter kNodeReadAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "node_read", "call to nodeReadAsync");
        private static readonly Counter kNodeWriteAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "node_write", "call to nodeWriteAsync");
        private static readonly Counter kValueReadAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "value_read", "call to valueReadAsync");
        private static readonly Histogram kValueReadHistAsync = Metrics
            .CreateHistogram(kTwinMetricsPrefix + "value_read_hist", "call to valueReadAsync");
        private static readonly Counter kValueWriteAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "value_write", "call to valueWriteAsync");
        private static readonly Counter kModelUploadStartAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "model_upload_start", "call to modelUploadStartrAsync");
        private static readonly Counter kMethodCallAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "method_call", "call to methodCallAsync");
        private static readonly Counter kMethodMetadataAsync = Metrics
            .CreateCounter(kTwinMetricsPrefix + "method_metadata", "call to methodMetadataAsync");
    }
}
