// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Client {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge client directly accesses the opc server to create monitored
    /// item subscriptions and publish these to its telemetry endpoint,
    /// corresponding to its device identity.
    /// </summary>
    public class OpcUaEdgeClient : IOpcUaPublisher, IOpcUaPublishServices,
        IOpcUaValidationServices {

        /// <summary>
        /// Create edge client
        /// </summary>
        /// <param name="stack"></param>
        public OpcUaEdgeClient(IOpcUaClient stack, ILogger logger) {
            _stack = stack;
            _logger = logger;

            if (_stack.UsesProxy) {
                _logger.Warn("Bad configuration - client should not be in proxy mode. " +
                    "If you did not intend to test subscriptions through proxy module, " +
                    "this is likely the result of a IoT Hub service connnection string " +
                    "in the services configuration.  Remove and restart.", () => { });
            }
        }

        /// <summary>
        /// Returns the list of published nodes of the OPC UA server
        /// with the given endpoint
        /// </summary>
        public Task<IEnumerable<string>> GetPublishedNodeIds(
            ServerEndpointModel endpoint) {

            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Requests from a edge publisher to publish nodes on
        /// the specified endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<PublishResultModel> NodePublishAsync(
            ServerEndpointModel endpoint, PublishRequestModel request) {

            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate registration request and returns updated one.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<ServerRegistrationRequestModel> ValidateAsync(
            ServerRegistrationRequestModel request) {

            // TODO
            return Task.FromResult(request);
        }

        private readonly IOpcUaClient _stack;
        private readonly ILogger _logger;
    }
}
