// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Node settings controller
    /// </summary>
    [Version(1)]
    public class NodeSettingsController : ISettingsController {

        /// <summary>
        /// Called to perform an action on the twin
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public JToken this[string nodeId] {
            set {
                switch (value.Type) {
                    case JTokenType.Boolean:
                    case JTokenType.Null:
                    case JTokenType.String:
                        break;
                    default:
                        throw new NotSupportedException("Value not supported");
                }
                if (!_action.ContainsKey(nodeId)) {
                    _action.Add(nodeId, value);
                }
                else {
                    _action[nodeId] = value;
                }
            }
        }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="publisher"></param>
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        public NodeSettingsController(
            INodeServices<EndpointModel> nodes, IPublishServices<EndpointModel> publisher,
            IEndpointServices twin, ILogger logger) {
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _action = new Dictionary<string, JToken>();
        }

        /// <summary>
        /// Apply publish/unpublish
        /// </summary>
        /// <returns></returns>
        public async Task ApplyAsync() {
            foreach (var action in _action) {
                switch (action.Value.Type) {
                    case JTokenType.Boolean:
                        await PublishAsync(action.Key, (bool)action.Value);
                        break;
                    case JTokenType.Null:
                        await PublishAsync(action.Key, null);
                        break;
                    case JTokenType.String:
                        await WriteAsync(action.Key, action.Value);
                        break;
                    default:
                        throw new NotSupportedException("Value not supported");
                }
            }
            _action.Clear();
        }

        /// <summary>
        /// Write a variable node value
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private async Task WriteAsync(string nodeId, JToken value) {
            await _nodes.NodeValueWriteAsync(_twin.Endpoint,
                new ValueWriteRequestModel {
                    NodeId = nodeId,
                    Value = value
                });
        }

        /// <summary>
        /// Publish or unpublish a node
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        private async Task PublishAsync(string nodeId, bool? enabled) {
            if (enabled ?? true) {
                await _publisher.NodePublishStartAsync(_twin.Endpoint,
                    new PublishStartRequestModel {
                        Node = new PublishedNodeModel {
                            NodeId = nodeId
                        }
                    });
            }
            else {
                await _publisher.NodePublishStopAsync(_twin.Endpoint,
                    new PublishStopRequestModel {
                        NodeId = nodeId
                    });

            }
        }

        private readonly Dictionary<string, JToken> _action;
        private readonly IEndpointServices _twin;
        private readonly IPublishServices<EndpointModel> _publisher;
        private readonly INodeServices<EndpointModel> _nodes;
        private readonly ILogger _logger;
    }
}
