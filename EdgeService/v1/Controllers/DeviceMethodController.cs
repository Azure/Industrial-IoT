// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.v1.Controllers {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.v1.Models;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services;
    using System.Threading.Tasks;

    /// <summary>
    /// Browse controller
    /// </summary>
    public class DeviceMethodController : IModuleMethodsV1 {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browse"></param>
        /// <param name="validate"></param>
        /// <param name="nodes"></param>
        /// <param name="publish"></param>
        public DeviceMethodController(IOpcUaBrowseServices browse,
            IOpcUaValidationServices validate, IOpcUaNodeServices nodes, 
            IOpcUaPublishServices publish) {
            _browse = browse;
            _validate = validate;
            _nodes = nodes;
            _publish = publish;
        }

        /// <summary>
        /// Validate request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ServerRegistrationRequestApiModel> ValidateAsync(
            ServerRegistrationRequestApiModel request) {
            var result = await _validate.ValidateAsync(request.ToServiceModel());
            return new ServerRegistrationRequestApiModel(result);
        }

        /// <summary>
        /// Publish
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishResponseApiModel> NodePublishAsync(
            ServerEndpointApiModel endpoint, PublishRequestApiModel request) {
            var result = await _publish.NodePublishAsync(
                endpoint.ToServiceModel(), request.ToServiceModel());
            return new PublishResponseApiModel(result);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseResponseApiModel> NodeBrowseAsync(
            ServerEndpointApiModel endpoint, BrowseRequestApiModel request) {
            var result = await _browse.NodeBrowseAsync(
                endpoint.ToServiceModel(), request.ToServiceModel());
            return new BrowseResponseApiModel(result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResponseApiModel> NodeValueReadAsync(
            ServerEndpointApiModel endpoint, ValueReadRequestApiModel request) {
            var result = await _nodes.NodeValueReadAsync(
                endpoint.ToServiceModel(), request.ToServiceModel());
            return new ValueReadResponseApiModel(result);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResponseApiModel> NodeValueWriteAsync(
            ServerEndpointApiModel endpoint, ValueWriteRequestApiModel request) {
            var result = await _nodes.NodeValueWriteAsync(
                endpoint.ToServiceModel(), request.ToServiceModel());
            return new ValueWriteResponseApiModel(result);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            ServerEndpointApiModel endpoint, MethodMetadataRequestApiModel request) {
            var result = await _nodes.NodeMethodGetMetadataAsync(
                endpoint.ToServiceModel(), request.ToServiceModel());
            return new MethodMetadataResponseApiModel(result);
        }

        /// <summary>
        /// For the call
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            ServerEndpointApiModel endpoint, MethodCallRequestApiModel request) {
            var result = await _nodes.NodeMethodCallAsync(
                endpoint.ToServiceModel(), request.ToServiceModel());
            return new MethodCallResponseApiModel(result);
        }

        private readonly IOpcUaBrowseServices _browse;
        private readonly IOpcUaValidationServices _validate;
        private readonly IOpcUaNodeServices _nodes;
        private readonly IOpcUaPublishServices _publish;
    }
}
