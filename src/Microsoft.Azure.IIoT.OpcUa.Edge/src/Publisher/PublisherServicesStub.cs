// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin as peer of publisher module
    /// </summary>
    public class PublisherServicesStub : IPublisherServices {

        /// <summary>
        /// Current endpoint or null if not yet provisioned
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// Update endpoint information
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public Task SetEndpointAsync(EndpointModel endpoint) {
            Endpoint = endpoint;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process publish request coming from method call
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<PublishResultModel> NodePublishAsync(PublishRequestModel request) {
            throw new NotSupportedException("Publisher not supported in Edge module");
        }

        /// <summary>
        /// Enable or disable publishing based on desired property
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        public Task NodePublishAsync(string nodeId, bool? enable) {
            throw new NotSupportedException("Publisher not supported in Edge module");
        }
    }
}
