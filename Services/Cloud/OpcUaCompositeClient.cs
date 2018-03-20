// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Client;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Exceptions;

    /// <summary>
    /// The cloud router service is a composite of edge twin micro service +
    /// adhoc proxy or supervisor based command control (for endpoints not
    /// managed by iot hub as a device identity). Cloud router routes to
    /// the applicable service based on the endpoint model's edge controller
    /// setting which enables the edge service to select the device object
    /// that provides the micro services.
    /// </summary>
    public sealed class OpcUaCompositeClient : IOpcUaTwinBrowseServices, IOpcUaTwinNodeServices,
        IOpcUaTwinPublishServices, IOpcUaAdhocBrowseServices, IOpcUaAdhocPublishServices,
        IOpcUaAdhocNodeServices {

        /// <summary>
        /// Create cloud router.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="proxy"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        /// <param name="endpoints"></param>
        public OpcUaCompositeClient(IOpcUaTwinRegistry endpoints, IIoTHubTwinServices twin,
            IOpcUaVariantCodec codec, IOpcUaClient proxy, ILogger logger) {

            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create composite of v2 (Twin + supervisor) and v1 (Publisher + Proxy) services

            // V2 services
            _twin = new OpcUaTwinClient(twin, logger);
            _supervisor = new OpcUaSupervisorClient(twin, logger);

            // V1 services
            _publisher = new OpcUaPublishServices(proxy, logger);
            _proxy = new OpcUaNodeServices(proxy, codec, logger);
        }

        /// <summary>
        /// Create cloud router.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        /// <param name="endpoints"></param>
        public OpcUaCompositeClient(IOpcUaTwinRegistry endpoints, IIoTHubTwinServices twin,
            ILogger logger) {

            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create composite of v2 (Twin+supervisor)

            // V2 services
            _twin = new OpcUaTwinClient(twin, logger);
            _supervisor = new OpcUaSupervisorClient(twin, logger);
        }

        /// <summary>
        /// Browse nodes
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseResultModel> NodeBrowseAsync(
            EndpointModel endpoint, BrowseRequestModel request) {
            if (endpoint.TwinId == null &&
                endpoint.SupervisorId == null) {
                endpoint = await _endpoints.FindEndpointAsync(endpoint);
            }
            if (endpoint.TwinId != null) {
                try {
                    return await _twin.NodeBrowseAsync(endpoint.TwinId, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed browse on edge twin. Trying supervisor",
                        () => e);
                }
            }
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeBrowseAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed browse on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeBrowseAsync(endpoint, request);
        }

        /// <summary>
        /// Browse nodes
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseResultModel> NodeBrowseAsync(string twinId,
            BrowseRequestModel request) {
            try {
                return await _twin.NodeBrowseAsync(twinId, request);
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                _logger.Error("Failed browse on edge twin. Trying adhoc",
                    () => e);
            }
            var endpoint = await _endpoints.GetEndpointAsync(twinId);
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeBrowseAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed browse on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeBrowseAsync(endpoint, request);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            EndpointModel endpoint, MethodCallRequestModel request) {
            if (endpoint.TwinId == null &&
                endpoint.SupervisorId == null) {
                endpoint = await _endpoints.FindEndpointAsync(endpoint);
            }
            if (endpoint.TwinId != null) {
                try {
                    return await _twin.NodeMethodCallAsync(endpoint.TwinId, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    // use proxy instead
                    _logger.Error("Failed method call on edge twin. Trying supervisor",
                        () => e);
                }
            }
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeMethodCallAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    // use proxy instead
                    _logger.Error("Failed method call on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeMethodCallAsync(endpoint, request);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(string twinId,
            MethodCallRequestModel request) {
            try {
                return await _twin.NodeMethodCallAsync(twinId, request);
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                _logger.Error("Failed method call on edge twin. Trying adhoc",
                    () => e);
            }
            var endpoint = await _endpoints.GetEndpointAsync(twinId);
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeMethodCallAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed method call on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeMethodCallAsync(endpoint, request);
        }

        /// <summary>
        /// Get meta data
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            EndpointModel endpoint, MethodMetadataRequestModel request) {
            if (endpoint.TwinId == null &&
                endpoint.SupervisorId == null) {
                endpoint = await _endpoints.FindEndpointAsync(endpoint);
            }
            if (endpoint.TwinId != null) {
                try {
                    return await _twin.NodeMethodGetMetadataAsync(endpoint.TwinId,
                        request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed metadata get on edge twin. Trying supervisor",
                        () => e);
                }
            }
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeMethodGetMetadataAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed metadata get on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeMethodGetMetadataAsync(endpoint, request);
        }

        /// <summary>
        /// Get meta data
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(string twinId,
            MethodMetadataRequestModel request) {
            try {
                return await _twin.NodeMethodGetMetadataAsync(twinId, request);
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                _logger.Error("Failed metadata get on edge twin. Trying adhoc",
                    () => e);
            }
            var endpoint = await _endpoints.GetEndpointAsync(twinId);
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeMethodGetMetadataAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed metadata get on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeMethodGetMetadataAsync(endpoint, request);
        }

        /// <summary>
        /// Publish node
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishResultModel> NodePublishAsync(
            EndpointModel endpoint, PublishRequestModel request) {
            if (endpoint.TwinId == null &&
                endpoint.SupervisorId == null) {
                endpoint = await _endpoints.FindEndpointAsync(endpoint);
            }
            if (endpoint.TwinId != null) {
                try {
                    return await _twin.NodePublishAsync(endpoint.TwinId, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed publish call on edge twin. Trying proxy",
                        () => e);
                }
            }
            if (_publisher == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _publisher.NodePublishAsync(endpoint, request);
        }

        /// <summary>
        /// Publish node
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishResultModel> NodePublishAsync(string twinId,
            PublishRequestModel request) {
            try {
                return await _twin.NodePublishAsync(twinId, request);
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                _logger.Error("Failed public on edge twin. Trying proxy",
                    () => e);
            }
            if (_publisher == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            var endpoint = await _endpoints.GetEndpointAsync(twinId);
            return await _publisher.NodePublishAsync(endpoint, request);
        }

        /// <summary>
        /// Get published nodes on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<PublishedNodeListModel> ListPublishedNodesAsync(
            string continuation, EndpointModel endpoint) {
            if (endpoint.TwinId == null &&
                endpoint.SupervisorId == null) {
                endpoint = await _endpoints.FindEndpointAsync(endpoint);
            }
            if (endpoint.TwinId != null) {
                try {
                    return await _twin.ListPublishedNodesAsync(continuation, endpoint.TwinId);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed get published node ids from edge twin. Trying proxy",
                        () => e);
                }
            }
            if (_publisher == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _publisher.ListPublishedNodesAsync(continuation, endpoint);
        }

        /// <summary>
        /// Get published nodes
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns></returns>
        public async Task<PublishedNodeListModel> ListPublishedNodesAsync(
            string continuation, string twinId) {
            try {
                return await _twin.ListPublishedNodesAsync(continuation, twinId);
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                _logger.Error("Failed get published node ids from edge twin. Trying proxy",
                    () => e);
            }
            if (_publisher == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            var endpoint = await _endpoints.GetEndpointAsync(twinId);
            return await _publisher.ListPublishedNodesAsync(continuation, endpoint);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            EndpointModel endpoint, ValueReadRequestModel request) {
            if (endpoint.TwinId == null &&
                endpoint.SupervisorId == null) {
                endpoint = await _endpoints.FindEndpointAsync(endpoint);
            }
            if (endpoint.TwinId != null) {
                try {
                    return await _twin.NodeValueReadAsync(endpoint.TwinId, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed value read call on edge twin. Trying supervisor",
                        () => e);
                }
            }
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeValueReadAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed value read call on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeValueReadAsync(endpoint, request);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResultModel> NodeValueReadAsync(string twinId,
            ValueReadRequestModel request) {
            try {
                return await _twin.NodeValueReadAsync(twinId, request);
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                _logger.Error("Failed value read on edge twin. Trying adhoc",
                    () => e);
            }
            var endpoint = await _endpoints.GetEndpointAsync(twinId);
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeValueReadAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed value read on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeValueReadAsync(endpoint, request);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            EndpointModel endpoint, ValueWriteRequestModel request) {
            if (endpoint.TwinId == null &&
                endpoint.SupervisorId == null) {
                endpoint = await _endpoints.FindEndpointAsync(endpoint);
            }
            if (endpoint.TwinId != null) {
                try {
                    return await _twin.NodeValueWriteAsync(endpoint.TwinId, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed value write call on edge twin. Trying supervisor",
                        () => e);
                }
            }
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeValueWriteAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed value write call on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeValueWriteAsync(endpoint, request);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(string twinId,
            ValueWriteRequestModel request) {
            try {
                return await _twin.NodeValueWriteAsync(twinId, request);
            }
            catch (MethodCallStatusException) {
                throw;
            }
            catch (Exception e) {
                _logger.Error("Failed value write on edge twin. Trying adhoc",
                    () => e);
            }
            var endpoint = await _endpoints.GetEndpointAsync(twinId);
            if (endpoint.SupervisorId != null) {
                try {
                    return await _supervisor.NodeValueWriteAsync(endpoint, request);
                }
                catch (MethodCallStatusException) {
                    throw;
                }
                catch (Exception e) {
                    _logger.Error("Failed value write on edge supervisor. Trying proxy",
                        () => e);
                }
            }
            if (_proxy == null) {
                throw new ConnectionException("Failed to connect to twin.");
            }
            return await _proxy.NodeValueWriteAsync(endpoint, request);
        }

        private readonly OpcUaTwinClient _twin;
        private readonly OpcUaSupervisorClient _supervisor;
        private readonly OpcUaNodeServices _proxy;
        private readonly IOpcUaTwinRegistry _endpoints;
        private readonly OpcUaPublishServices _publisher;
        private readonly ILogger _logger;
    }
}
