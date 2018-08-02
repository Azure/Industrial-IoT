// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin settings controller
    /// </summary>
    [Version(1)]
    [ExceptionsFilter]
    public class OpcUaTwinMethods : IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browse"></param>
        /// <param name="nodes"></param>
        /// <param name="logger"></param>
        public OpcUaTwinMethods(IOpcUaBrowseServices<EndpointModel> browse,
            IOpcUaNodeServices<EndpointModel> nodes, IOpcUaTwinServices twin,
            IEventEmitter events, ILogger logger) {
            _browse = browse ?? throw new ArgumentNullException(nameof(browse));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publish
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishResponseApiModel> PublishAsync(
            PublishRequestApiModel request) {
            var result = await _twin.NodePublishAsync(request.ToServiceModel());
            return new PublishResponseApiModel(result);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseResponseApiModel> BrowseAsync(
            BrowseRequestApiModel request) {

            // Limit returned references to fit into 128k response
            const int kMaxReferences = 430;
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
            var result = await _browse.NodeBrowseNextAsync(
                _twin.Endpoint, request.ToServiceModel());
            return new BrowseNextResponseApiModel(result);
        }

        /// <summary>
        /// Read
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResponseApiModel> ValueReadAsync(
            ValueReadRequestApiModel request) {
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
            var result = await _nodes.NodeMethodCallAsync(
                _twin.Endpoint, request.ToServiceModel());
            return new MethodCallResponseApiModel(result);
        }

        private readonly IOpcUaBrowseServices<EndpointModel> _browse;
        private readonly IOpcUaNodeServices<EndpointModel> _nodes;
        private readonly IOpcUaTwinServices _twin;
        private readonly IEventEmitter _events;
        private readonly ILogger _logger;
    }
}
