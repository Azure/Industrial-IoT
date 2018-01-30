// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Client;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The cloud router service is a composite of edge micro service +
    /// proxy based command control (for endpoints not managed by iot hub
    /// as a device identity). Cloud router routes to the applicable
    /// service based on the endpoint model's edge controller setting which
    /// enables the edge service to select the device object that provides
    /// the micro services.
    /// </summary>
    public class OpcUaCloudRouter : IOpcUaBrowseServices, IOpcUaNodeServices,
        IOpcUaPublishServices, IOpcUaValidationServices {

        /// <summary>
        /// Create cloud router.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="proxy"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        public OpcUaCloudRouter(IIoTHubTwinServices twin, IOpcUaClient proxy,
            IOpcUaVariantCodec codec, ILogger logger) {

            // Create composite of edge micro service + proxy control
            _edge = new OpcUaEdgeServices(twin, logger);
            _proxy = new OpcUaEdgeProxy(proxy, logger);
            _server = new OpcUaServerNodes(proxy, codec, _proxy, logger);
            _logger = logger;
        }

        /// <summary>
        /// Browse nodes
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<BrowseResultModel> NodeBrowseAsync(
            ServerEndpointModel endpoint, BrowseRequestModel request) {
            if (endpoint.EdgeController != null) {
                try {
                    return _edge.NodeBrowseAsync(endpoint, request);
                }
                catch(Exception e) {
                    // use proxy instead
                    _logger.Error("Failed browse on edge service. Trying proxy",
                        () => e);
                }
            }
            return _server.NodeBrowseAsync(endpoint, request);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<MethodCallResultModel> NodeMethodCallAsync(
            ServerEndpointModel endpoint, MethodCallRequestModel request) {
            if (endpoint.EdgeController != null) {
                try {
                    return _edge.NodeMethodCallAsync(endpoint, request);
                }
                catch (Exception e) {
                    // use proxy instead
                    _logger.Error("Failed method call on edge service. Trying proxy",
                        () => e);
                }
            }
            return _server.NodeMethodCallAsync(endpoint, request);
        }

        /// <summary>
        /// Get meta data
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            ServerEndpointModel endpoint, MethodMetadataRequestModel request) {
            if (endpoint.EdgeController != null) {
                try {
                    return _edge.NodeMethodGetMetadataAsync(endpoint, request);
                }
                catch (Exception e) {
                    // use proxy instead
                    _logger.Error("Failed metadata get on edge service. Trying proxy",
                        () => e);
                }
            }
            return _server.NodeMethodGetMetadataAsync(endpoint, request);
        }

        /// <summary>
        /// Publish node
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<PublishResultModel> NodePublishAsync(
            ServerEndpointModel endpoint, PublishRequestModel request) {
            if (endpoint.EdgeController != null) {
                try {
                    return _edge.NodePublishAsync(endpoint, request);
                }
                catch (Exception e) {
                    // use proxy instead
                    _logger.Error("Failed publish call on edge service. Trying proxy",
                        () => e);
                }
            }
            return _proxy.NodePublishAsync(endpoint, request);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<ValueReadResultModel> NodeValueReadAsync(
            ServerEndpointModel endpoint, ValueReadRequestModel request) {
            if (endpoint.EdgeController != null) {
                try {
                    return _edge.NodeValueReadAsync(endpoint, request);
                }
                catch (Exception e) {
                    // use proxy instead
                    _logger.Error("Failed value read call on edge service. Trying proxy",
                        () => e);
                }
            }
            return _server.NodeValueReadAsync(endpoint, request);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<ValueWriteResultModel> NodeValueWriteAsync(
            ServerEndpointModel endpoint, ValueWriteRequestModel request) {
            if (endpoint.EdgeController != null) {
                try {
                    return _edge.NodeValueWriteAsync(endpoint, request);
                }
                catch (Exception e) {
                    // use proxy instead
                    _logger.Error("Failed value write call on edge service. Trying proxy",
                        () => e);
                }
            }
            return _server.NodeValueWriteAsync(endpoint, request);
        }

        /// <summary>
        /// Validate
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<ServerRegistrationRequestModel> ValidateAsync(
            ServerRegistrationRequestModel request) {
            if (request.Endpoint.EdgeController != null) {
                try {
                    return _edge.ValidateAsync(request);
                }
                catch (Exception e) {
                    // use proxy instead
                    _logger.Error("Failed validation call on edge service. Trying proxy",
                        () => e);
                }
            }
            return _proxy.ValidateAsync(request);
        }

        private readonly OpcUaEdgeServices _edge;
        private readonly OpcUaServerNodes _server;
        private readonly OpcUaEdgeProxy _proxy;
        private readonly ILogger _logger;
    }
}
