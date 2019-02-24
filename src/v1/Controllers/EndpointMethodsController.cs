// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Supervisor {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint methods controller
    /// </summary>
    [Version(1)]
    [ExceptionsFilter]
    public class EndpointMethodsController : IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browse"></param>
        /// <param name="nodes"></param>
        /// <param name="historian"></param>
        /// <param name="publisher"></param>
        /// <param name="export"></param>
        /// <param name="twin"></param>
        /// <param name="events"></param>
        /// <param name="logger"></param>
        public EndpointMethodsController(IBrowseServices<EndpointModel> browse,
            INodeServices<EndpointModel> nodes, IHistoricAccessServices<EndpointModel> historian,
            IPublishServices<EndpointModel> publisher, IUploadServices<EndpointModel> export,
            ITwinServices twin, IEventEmitter events, ILogger logger) {
            _browse = browse ?? throw new ArgumentNullException(nameof(browse));
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _export = export ?? throw new ArgumentNullException(nameof(export));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publish
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishStartResponseApiModel> PublishStartAsync(
            PublishStartRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishStartAsync(
                _twin.Endpoint, request.ToServiceModel());
            return new PublishStartResponseApiModel(result);
        }

        /// <summary>
        /// Unpublish
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishStopResponseApiModel> PublishStopAsync(
            PublishStopRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishStopAsync(
                _twin.Endpoint, request.ToServiceModel());
            return new PublishStopResponseApiModel(result);
        }

        /// <summary>
        /// List published nodes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishedItemListResponseApiModel> PublishListAsync(
            PublishedItemListRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _publisher.NodePublishListAsync(
                _twin.Endpoint, request.ToServiceModel());
            return new PublishedItemListResponseApiModel(result);
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
                _twin.Endpoint, request.ToServiceModel());
            return new ModelUploadStartResponseApiModel(result);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseResponseApiModel> BrowseAsync(
            BrowseRequestApiModel request) {

            // Limit returned references to fit into 128k response
            const int kMaxReferences = 100;
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (!request.MaxReferencesToReturn.HasValue ||
                request.MaxReferencesToReturn.Value > kMaxReferences) {
                request.MaxReferencesToReturn = kMaxReferences;
            }
            var result = await _browse.NodeBrowseFirstAsync(
                _twin.Endpoint, request.ToServiceModel());
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
                _twin.Endpoint, request.ToServiceModel());
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
                _twin.Endpoint, request.ToServiceModel());
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
                _twin.Endpoint, request.ToServiceModel());
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
                _twin.Endpoint, request.ToServiceModel());
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
                _twin.Endpoint, request.ToServiceModel());
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
                _twin.Endpoint, request.ToServiceModel());
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
                _twin.Endpoint, request.ToServiceModel());
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
                _twin.Endpoint, request.ToServiceModel());
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
               _twin.Endpoint, request.ToServiceModel());
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
               _twin.Endpoint, request.ToServiceModel());
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
               _twin.Endpoint, request.ToServiceModel());
            return new HistoryUpdateResponseApiModel(result);
        }

        private readonly IBrowseServices<EndpointModel> _browse;
        private readonly IHistoricAccessServices<EndpointModel> _historian;
        private readonly INodeServices<EndpointModel> _nodes;
        private readonly ITwinServices _twin;
        private readonly IPublishServices<EndpointModel> _publisher;
        private readonly IUploadServices<EndpointModel> _export;
        private readonly IEventEmitter _events;
        private readonly ILogger _logger;
    }
}
