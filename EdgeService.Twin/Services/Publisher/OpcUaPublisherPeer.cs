// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.EdgeService.Twin {
    using Microsoft.Azure.IIoT.OpcTwin.EdgeService.Models;
    using Microsoft.Azure.IIoT.OpcTwin.Services;
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using Microsoft.Azure.IIoT.Common.Diagnostics;
    using Microsoft.Azure.Devices.Edge;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin as peer of publisher module
    /// </summary>
    public class OpcUaPublisherPeer : IOpcUaTwinServices {

        /// <summary>
        /// Current endpoint or null if not yet provisioned
        /// </summary>
        public TwinEndpointModel Endpoint { get; set; }

        /// <summary>
        /// Create publisher peer service
        /// </summary>
        /// <param name="publisher"></param>
        public OpcUaPublisherPeer(IOpcUaAdhocPublishServices publisher,
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
        public Task SetEndpointAsync(TwinEndpointModel endpoint) {
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
                Endpoint.ToServiceModel(), request);
            // Update reported property
            await _events.SendAsync(request.NodeId, request.Enabled);
            return result;
        }

        /// <summary>
        /// Enable or disable publishing based on desired property
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public Task NodePublishAsync(string nodeId, bool? enable) {
            return _publisher.NodePublishAsync(Endpoint.ToServiceModel(),
                new PublishRequestModel {
                    NodeId = nodeId,
                    Enabled = enable
                });
        }

        private readonly IOpcUaAdhocPublishServices _publisher;
        private readonly IEventEmitter _events;
        private readonly ILogger _logger;
    }
}
