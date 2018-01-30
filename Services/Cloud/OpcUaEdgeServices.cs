// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node and publish services through edge command control against
    /// the OPC UA edge device module receiving service requests via device method
    /// call.
    /// </summary>
    public class OpcUaEdgeServices : IOpcUaBrowseServices, IOpcUaNodeServices,
        IOpcUaPublishServices, IOpcUaValidationServices {

        /// <summary>
        /// Create using rpc mechanism
        /// </summary>
        /// <param name="twin"></param>
        public OpcUaEdgeServices(IIoTHubTwinServices twin, ILogger logger) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Browse a tree node, returns node properties and all child nodes if not excluded.
        /// </summary>
        /// <param name="endpoint">Endpoint url of the server to talk to</param>
        /// <param name="request">browse node and filters</param>
        /// <returns></returns>
        public async Task<BrowseResultModel> NodeBrowseAsync(ServerEndpointModel endpoint,
            BrowseRequestModel request) {
            return await CallServiceOnEndpoint<BrowseRequestModel, BrowseResultModel>(
                "Browse_V1", endpoint, request);
        }

        /// <summary>
        /// Read a variable value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request">Read nodes</param>
        /// <returns></returns>
        public async Task<ValueReadResultModel> NodeValueReadAsync(ServerEndpointModel endpoint,
            ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return await CallServiceOnEndpoint<ValueReadRequestModel, ValueReadResultModel>(
                "ValueRead_V1", endpoint, request);
        }

        /// <summary>
        /// Write variable value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(ServerEndpointModel endpoint,
            ValueWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Node == null) {
                throw new ArgumentNullException(nameof(request.Node));
            }
            if (request.Value == null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            if (string.IsNullOrEmpty(request.Node.Id)) {
                throw new ArgumentException(nameof(request.Node.Id));
            }
            if (string.IsNullOrEmpty(request.Node.DataType)) {
                throw new ArgumentException(nameof(request.Node.DataType));
            }
            return await CallServiceOnEndpoint<ValueWriteRequestModel, ValueWriteResultModel>(
                "ValueWrite_V1", endpoint, request);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            ServerEndpointModel endpoint, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            return await CallServiceOnEndpoint<MethodMetadataRequestModel, MethodMetadataResultModel>(
                "MethodMetadata_V1", endpoint, request);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            ServerEndpointModel endpoint, MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            return await CallServiceOnEndpoint<MethodCallRequestModel, MethodCallResultModel>(
                "MethodCall_V1", endpoint, request);
        }

        /// <summary>
        /// Publish node values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishResultModel> NodePublishAsync(ServerEndpointModel endpoint,
            PublishRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentNullException(nameof(request.NodeId));
            }
            return await CallServiceOnEndpoint<PublishRequestModel, PublishResultModel>(
                "Publish_V1", endpoint, request);
        }

        /// <summary>
        /// Validate request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ServerRegistrationRequestModel> ValidateAsync(
            ServerRegistrationRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnEndpoint<ServerRegistrationRequestModel,
                ServerRegistrationRequestModel>("Validate_V1", request.Endpoint, request);
        }

        /// <summary>
        /// helper to invoke service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="service"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<R> CallServiceOnEndpoint<T, R>(string service,
            ServerEndpointModel endpoint, T request) {
            if (string.IsNullOrEmpty(endpoint.EdgeController)) {
                throw new ArgumentNullException(nameof(endpoint.EdgeController));
            }
            var result = await _twin.CallMethodAsync(endpoint.EdgeController,
                new MethodParameterModel {
                    Name = service,
                    JsonPayload = JsonConvert.SerializeObject(new {
                        endpoint,
                        request
                    })
                });
            if (result.Status != 200) {
                throw new MethodCallStatusException(result.Status, result.JsonPayload);
            }
            return JsonConvert.DeserializeObject<R>(result.JsonPayload);
        }

        private readonly IIoTHubTwinServices _twin;
        private readonly ILogger _logger;
    }
}
