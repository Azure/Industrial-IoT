// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Controllers {
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Filters;
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.Devices.Edge;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin settings controller
    /// </summary>
    [Version(1)]
    [ExceptionsFilter]
    public class OpcUaTwinMethods : IOpcUaTwinMethods, IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browse"></param>
        /// <param name="nodes"></param>
        /// <param name="logger"></param>
        public OpcUaTwinMethods(IOpcUaAdhocBrowseServices browse, IOpcUaAdhocNodeServices nodes,
            IOpcUaTwinServices twin, IEventEmitter events, ILogger logger) {
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
            var result = await _browse.NodeBrowseAsync(
                _twin.Endpoint.ToServiceModel(),
                request.ToServiceModel());
            return new BrowseResponseApiModel(result);
        }

        /// <summary>
        /// Read
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResponseApiModel> ValueReadAsync(
            ValueReadRequestApiModel request) {
            var result = await _nodes.NodeValueReadAsync(
                _twin.Endpoint.ToServiceModel(),
                request.ToServiceModel());
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
                _twin.Endpoint.ToServiceModel(),
                request.ToServiceModel());
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
                _twin.Endpoint.ToServiceModel(),
                request.ToServiceModel());
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
                _twin.Endpoint.ToServiceModel(),
                request.ToServiceModel());
            return new MethodCallResponseApiModel(result);
        }

        private readonly IOpcUaAdhocBrowseServices _browse;
        private readonly IOpcUaAdhocNodeServices _nodes;
        private readonly IOpcUaTwinServices _twin;
        private readonly IEventEmitter _events;
        private readonly ILogger _logger;
    }
}
