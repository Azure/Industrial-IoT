// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Twin settings controller
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
        /// <param name="logger"></param>
        public NodeSettingsController(IBrowseServices<EndpointModel> browse,
            INodeServices<EndpointModel> nodes, IPublisherServices twin,
            ILogger logger) {
            _browse = browse ?? throw new ArgumentNullException(nameof(browse));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
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
            var result = await _browse.NodeBrowseFirstAsync(
                _twin.Endpoint,
                new BrowseRequestModel {
                    NodeId = nodeId,
                    MaxReferencesToReturn = 0
                });
            await _nodes.NodeValueWriteAsync(
                _twin.Endpoint,
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

        private readonly Dictionary<string, JToken> _action;
        private readonly IPublisherServices _twin;
        private readonly IBrowseServices<EndpointModel> _browse;
        private readonly INodeServices<EndpointModel> _nodes;
        private readonly ILogger _logger;
    }
}
