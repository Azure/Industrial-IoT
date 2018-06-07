// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Module.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.Edge.Module.Twin.v1.Filters;
    using Microsoft.Azure.IIoT.Edge.Module.Twin.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Edge;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor method controller
    /// </summary>
    [Version(1)]
    [ExceptionsFilter]
    public class OpcUaSupervisorMethods : IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="browse"></param>
        /// <param name="validate"></param>
        /// <param name="nodes"></param>
        /// <param name="logger"></param>
        public OpcUaSupervisorMethods(IOpcUaValidationServices validate,
            IOpcUaBrowseServices<EndpointModel> browse, IOpcUaNodeServices<EndpointModel> nodes,
            ILogger logger) {
            _browse = browse ?? throw new ArgumentNullException(nameof(browse));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _validate = validate ?? throw new ArgumentNullException(nameof(validate));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validate endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationApiModel> ValidateEndpointAsync(
            EndpointApiModel endpoint) {
            var result = await _validate.ValidateEndpointAsync(endpoint.ToServiceModel());
            return new ApplicationRegistrationApiModel(result);
        }

        /// <summary>
        /// Validate application
        /// </summary>
        /// <param name="discoveryUri"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationApiModel> DiscoverApplicationAsync(
            Uri discoveryUri) {
            var result = await _validate.DiscoverApplicationAsync(discoveryUri);
            return new ApplicationRegistrationApiModel(result);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseResponseApiModel> BrowseAsync(
            EndpointApiModel endpoint, BrowseRequestApiModel request) {
            var result = await _browse.NodeBrowseFirstAsync(
                endpoint.ToServiceModel(), request.ToServiceModel());
            return new BrowseResponseApiModel(result);
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseNextResponseApiModel> BrowseNextAsync(
            EndpointApiModel endpoint, BrowseNextRequestApiModel request) {
            var result = await _browse.NodeBrowseNextAsync(
                endpoint.ToServiceModel(), request.ToServiceModel());
            return new BrowseNextResponseApiModel(result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResponseApiModel> ValueReadAsync(
            EndpointApiModel endpoint, ValueReadRequestApiModel request) {
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
        public async Task<ValueWriteResponseApiModel> ValueWriteAsync(
            EndpointApiModel endpoint, ValueWriteRequestApiModel request) {
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
        public async Task<MethodMetadataResponseApiModel> MethodMetadataAsync(
            EndpointApiModel endpoint, MethodMetadataRequestApiModel request) {
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
        public async Task<MethodCallResponseApiModel> MethodCallAsync(
            EndpointApiModel endpoint, MethodCallRequestApiModel request) {
            var result = await _nodes.NodeMethodCallAsync(
                endpoint.ToServiceModel(), request.ToServiceModel());
            return new MethodCallResponseApiModel(result);
        }

        private readonly ILogger _logger;
        private readonly IOpcUaBrowseServices<EndpointModel> _browse;
        private readonly IOpcUaNodeServices<EndpointModel> _nodes;
        private readonly IOpcUaValidationServices _validate;
    }
}
