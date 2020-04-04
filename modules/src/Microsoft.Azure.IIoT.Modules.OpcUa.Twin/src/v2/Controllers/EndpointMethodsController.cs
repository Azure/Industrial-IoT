// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Filters;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using System;
    using System.Threading.Tasks;
    using Prometheus;

    /// <summary>
    /// Endpoint methods controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    [ExceptionsFilter]
    public class EndpointMethodsController : IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browse"></param>
        /// <param name="nodes"></param>
        /// <param name="historian"></param>
        /// <param name="export"></param>
        /// <param name="twin"></param>
        public EndpointMethodsController(IBrowseServices<EndpointModel> browse,
            INodeServices<EndpointModel> nodes, IHistoricAccessServices<EndpointModel> historian,
            IUploadServices<EndpointModel> export, ITwinServices twin) {
            _browse = browse ?? throw new ArgumentNullException(nameof(browse));
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _export = export ?? throw new ArgumentNullException(nameof(export));
        }

        /// <summary>
        /// Start model upload
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ModelUploadStartResponseApiModel> ModelUploadStartAsync(
            ModelUploadStartRequestApiModel request) {
            _modelUploadStartAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _export.ModelUploadStartAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new ModelUploadStartResponseApiModel(result);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseResponseApiModel> BrowseAsync(
            BrowseRequestApiModel request) {
            _browseAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _browse.NodeBrowseFirstAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new BrowseResponseApiModel(result);
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseNextResponseApiModel> BrowseNextAsync(
            BrowseNextRequestApiModel request) {
            _browseNextAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _browse.NodeBrowseNextAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new BrowseNextResponseApiModel(result);
        }

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowsePathResponseApiModel> BrowsePathAsync(
            BrowsePathRequestApiModel request) {
            _browsePathAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _browse.NodeBrowsePathAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new BrowsePathResponseApiModel(result);
        }

        /// <summary>
        /// Read
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResponseApiModel> ValueReadAsync(
            ValueReadRequestApiModel request) {
            _valueReadAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeValueReadAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new ValueReadResponseApiModel(result);
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResponseApiModel> ValueWriteAsync(
            ValueWriteRequestApiModel request) {
            _valueWriteAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeValueWriteAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new ValueWriteResponseApiModel(result);
        }

        /// <summary>
        /// Get meta data
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResponseApiModel> MethodMetadataAsync(
            MethodMetadataRequestApiModel request) {
            _methodMetadataAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeMethodGetMetadataAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new MethodMetadataResponseApiModel(result);
        }

        /// <summary>
        /// Call
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResponseApiModel> MethodCallAsync(
            MethodCallRequestApiModel request) {
            _methodCallAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeMethodCallAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new MethodCallResponseApiModel(result);
        }

        /// <summary>
        /// Read attributes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ReadResponseApiModel> NodeReadAsync(
            ReadRequestApiModel request) {
            _nodeReadAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeReadAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new ReadResponseApiModel(result);
        }

        /// <summary>
        /// Write attributes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<WriteResponseApiModel> NodeWriteAsync(
            WriteRequestApiModel request) {
            _nodeWriteAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeWriteAsync(
                await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new WriteResponseApiModel(result);
        }

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponseApiModel> HistoryReadAsync(
            HistoryReadRequestApiModel request) {
            _historyReadAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryReadAsync(
               await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new HistoryReadResponseApiModel(result);
        }

        /// <summary>
        /// Read next history
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadNextResponseApiModel> HistoryReadNextAsync(
            HistoryReadNextRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryReadNextAsync(
               await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new HistoryReadNextResponseApiModel(result);
        }

        /// <summary>
        /// Update history
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateAsync(
            HistoryUpdateRequestApiModel request) {
            _historyUpdateAsync.Inc();
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryUpdateAsync(
               await _twin.GetEndpointAsync(), request.ToServiceModel());
            return new HistoryUpdateResponseApiModel(result);
        }

        private readonly IBrowseServices<EndpointModel> _browse;
        private readonly IHistoricAccessServices<EndpointModel> _historian;
        private readonly INodeServices<EndpointModel> _nodes;
        private readonly ITwinServices _twin;
        private readonly IUploadServices<EndpointModel> _export;
        private static readonly String _PREFIX = "iiot_edge_twin_";
        private static readonly Counter _browseAsync = Metrics
    .CreateCounter(_PREFIX + "browse_async", "call to browseAsync");
        private static readonly Counter _browseNextAsync = Metrics
    .CreateCounter(_PREFIX + "browse_next_async", "call to browseNextAsync");
        private static readonly Counter _browsePathAsync = Metrics
    .CreateCounter(_PREFIX + "browse_path_async", "call to browsePathAsync");
        private static readonly Counter _historyReadAsync = Metrics
    .CreateCounter(_PREFIX + "history_read_async", "call to historyReadAsync");
        private static readonly Counter _historyUpdateAsync = Metrics
    .CreateCounter(_PREFIX + "history_update_async", "call to historyUpdateAsync");
        private static readonly Counter _nodeReadAsync = Metrics
    .CreateCounter(_PREFIX + "node_read_async", "call to _nodeReadAsync");
        private static readonly Counter _nodeWriteAsync = Metrics
    .CreateCounter(_PREFIX + "node_write_async", "call to _nodeWriteAsync");
        private static readonly Counter _valueReadAsync = Metrics
    .CreateCounter(_PREFIX + "value_read_async", "call to _valueReadAsync");
        private static readonly Counter _valueWriteAsync = Metrics
    .CreateCounter(_PREFIX + "value_write_async", "call to _valueWriteAsync");
        private static readonly Counter _modelUploadStartAsync = Metrics
    .CreateCounter(_PREFIX + "model_upload_start_async", "call to _modelUploadStartrAsync");
        private static readonly Counter _methodCallAsync = Metrics
    .CreateCounter(_PREFIX + "method_call_async", "call to _methodCallAsync");
        private static readonly Counter _methodMetadataAsync = Metrics
    .CreateCounter(_PREFIX + "method_metadata_async", "call to _methodMetadataAsync");
    }
}
