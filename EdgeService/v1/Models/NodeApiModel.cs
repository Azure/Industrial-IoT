// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;

    /// <summary>
    /// node model for edge service api
    /// </summary>
    public class NodeApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public NodeApiModel() { }

        /// <summary>
        /// Create node api model from service model
        /// </summary>
        /// <param name="model"></param>
        public NodeApiModel(NodeModel model) {
            Id = model.Id;
            ParentNode = model.ParentNode;
            HasChildren = model.HasChildren;
            IsPublished = model.IsPublished;
            Text = model.Text;
            NodeClass = model.NodeClass;
            AccessLevel = model.AccessLevel;
            EventNotifier = model.EventNotifier;
            Executable = model.Executable;
            DataType = model.DataType;
            ValueRank = model.ValueRank;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public NodeModel ToServiceModel() {
            return new NodeModel {
                Id = Id,
                ParentNode = ParentNode,
                HasChildren = HasChildren,
                IsPublished = IsPublished,
                Text = Text,
                NodeClass = NodeClass,
                AccessLevel = AccessLevel,
                EventNotifier = EventNotifier,
                Executable = Executable,
                DataType = DataType,
                ValueRank = ValueRank,
            };
        }

        /// <summary>
        /// Id of node and parent id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The parent node of the node, or null if not available.
        /// </summary>
        public string ParentNode { get; set; }

        /// <summary>
        /// Whether node has children
        /// </summary>
        public bool? HasChildren { get; set; }

        /// <summary>
        /// Whether currently subscribed
        /// </summary>
        public bool? IsPublished { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Type of node
        /// </summary>
        public string NodeClass { get; set; }

        /// <summary>
        /// User access level for node
        /// </summary>
        public string AccessLevel { get; set; }

        /// <summary>
        /// If eventing, event notifier to subscribe to.
        /// </summary>
        public string EventNotifier { get; set; }

        /// <summary>
        /// If method node, whether method can be called.
        /// </summary>
        public bool? Executable { get; set; }

        /// <summary>
        /// If variable, the node id of the variable's data type.  
        /// </summary>
        public string DataType { get; internal set; }

        /// <summary>
        /// If variable, value rank of variable.  Default value
        /// is scalar (-1).
        /// </summary>
        public int? ValueRank { get; internal set; }
    }
}
