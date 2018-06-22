// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// node model for webservice api
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
            HasChildren = model.HasChildren;
            IsPublished = model.IsPublished;
            DisplayName = model.DisplayName;
            Description = model.Description;
            NodeClass = model.NodeClass;
            IsAbstract = model.IsAbstract;
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
                HasChildren = HasChildren,
                IsPublished = IsPublished,
                DisplayName = DisplayName,
                Description = Description,
                NodeClass = NodeClass,
                IsAbstract = IsAbstract,
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
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Whether node has children
        /// </summary>
        [JsonProperty(PropertyName = "children",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasChildren { get; set; }

        /// <summary>
        /// Whether currently subscribed
        /// </summary>
        [JsonProperty(PropertyName = "publishedNode",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPublished { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [JsonProperty(PropertyName = "displayName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [JsonProperty(PropertyName = "description",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary>
        /// Type of node
        /// </summary>
        [JsonProperty(PropertyName = "nodeClass",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeClass? NodeClass { get; set; }

        /// <summary>
        /// Whether type is abstract
        /// </summary>
        [JsonProperty(PropertyName = "isAbstract",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// User access level for node
        /// </summary>
        [JsonProperty(PropertyName = "accessLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AccessLevel { get; set; }

        /// <summary>
        /// If eventing, event notifier to subscribe to.
        /// </summary>
        [JsonProperty(PropertyName = "eventNotifier",
            NullValueHandling = NullValueHandling.Ignore)]
        public string EventNotifier { get; set; }

        /// <summary>
        /// If method node, whether method can be called.
        /// </summary>
        [JsonProperty(PropertyName = "executable",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Executable { get; set; }

        /// <summary>
        /// If variable, the node id of the variable's data type.
        /// </summary>
        [JsonProperty(PropertyName = "dataType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataType { get; internal set; }

        /// <summary>
        /// If variable, value rank of variable.  Default value
        /// is scalar (-1).
        /// </summary>
        [JsonProperty(PropertyName = "valueRank",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(-1)]
        public int? ValueRank { get; internal set; }
    }
}
