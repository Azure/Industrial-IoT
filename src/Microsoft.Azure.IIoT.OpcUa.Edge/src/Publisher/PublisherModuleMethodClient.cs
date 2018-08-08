// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Access the publisher module and configure it to publish to its
    /// device identity endpoint. (V1 functionality)
    /// </summary>
    public class PublisherModuleMethodClient : IPublishServices<EndpointModel> {

        /// <summary>
        /// Create client to control publisher
        /// </summary>
        public PublisherModuleMethodClient(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Returns the list of published nodes of the OPC UA server
        /// with the given endpointUrl
        /// </summary>
        public Task<PublishedNodeListModel> ListPublishedNodesAsync(
            EndpointModel endpoint, string continuation) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Requests from a edge publisher to publish nodes on
        /// the specified station.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<PublishResultModel> NodePublishAsync(EndpointModel endpoint,
            PublishRequestModel request) {
            throw new NotImplementedException();
        }

        private readonly ILogger _logger;
    }
}
