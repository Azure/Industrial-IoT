// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Controllers {
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.Devices.Edge;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin settings controller
    /// </summary>
    [Version(1)]
    public class OpcUaTwinSettings : IOpcUaTwinSettings, ISettingsController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="logger"></param>
        public OpcUaTwinSettings(IOpcUaAdhocBrowseServices browse,
            IOpcUaAdhocNodeServices nodes, IOpcUaTwinServices twin, ILogger logger) {
            _browse = browse ?? throw new ArgumentNullException(nameof(browse));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Endpoint was updated
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public Task SetEndpointAsync(TwinEndpointModel endpoint) {

            // Update endpoint in twin state
            return _twin.SetEndpointAsync(endpoint);
        }

        /// <summary>
        /// Set publish/unpublish
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task SetAsync(string key, JToken value) {
            switch (value.Type) {
                case JTokenType.Boolean:
                    await PublishAsync(key, (bool)value);
                    break;
                case JTokenType.Null:
                    await PublishAsync(key, null);
                    break;
                case JTokenType.String:
                    await WriteAsync(key, value);
                    break;
                default:
                    throw new NotSupportedException("Value not supported");
            }
        }

        /// <summary>
        /// Write a variable node value
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private async Task WriteAsync(string nodeId, JToken value) {
            var result = await _browse.NodeBrowseAsync(
                _twin.Endpoint.ToServiceModel(),
                new BrowseRequestModel {
                    NodeId = nodeId,
                    ExcludeReferences = true
                });
            await _nodes.NodeValueWriteAsync(
                _twin.Endpoint.ToServiceModel(),
                new ValueWriteRequestModel {
                    Node = result.Node,
                    Value = (string)value
                });
        }

        /// <summary>
        /// Publish the node
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        private async Task PublishAsync(string nodeId, bool? enabled) {
            await _twin.NodePublishAsync(nodeId, enabled);
        }

        private readonly IOpcUaTwinServices _twin;
        private readonly IOpcUaAdhocBrowseServices _browse;
        private readonly IOpcUaAdhocNodeServices _nodes;
        private readonly ILogger _logger;
    }
}
