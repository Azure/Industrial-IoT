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
    }
}
