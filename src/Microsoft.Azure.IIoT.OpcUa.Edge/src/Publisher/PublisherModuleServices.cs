// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin as peer of publisher module
    /// </summary>
    public class PublisherModuleServices : IPublisherServices {

        /// <summary>
        /// Current endpoint or null if not yet provisioned
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// Create publisher peer service
        /// </summary>
        /// <param name="publisher"></param>
        /// <param name="events"></param>
        /// <param name="logger"></param>
        public PublisherModuleServices(IPublishServices<EndpointModel> publisher,
            IEventEmitter events, ILogger logger) {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Update endpoint information
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public Task SetEndpointAsync(EndpointModel endpoint) {
            if (Endpoint != endpoint) {
                _logger.Info("Updating endpoint", () => new {
                    Old = Endpoint,
                    New = endpoint
                });
                Endpoint = endpoint;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process publish request coming from method call
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishResultModel> NodePublishAsync(
            PublishRequestModel request) {

            var result = await _publisher.NodePublishAsync(
                Endpoint, request);
            // Update reported property
            await _events.SendAsync(request.NodeId, request.Enabled);
            return result;
        }

        /// <summary>
        /// Enable or disable publishing based on desired property
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        public Task NodePublishAsync(string nodeId, bool? enable) {
            return _publisher.NodePublishAsync(Endpoint,
                new PublishRequestModel {
                    NodeId = nodeId,
                    Enabled = enable
                });
        }

        private readonly IPublishServices<EndpointModel> _publisher;
        private readonly IEventEmitter _events;
        private readonly ILogger _logger;
    }
}
