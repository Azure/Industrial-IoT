// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Node class
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeClass {

        Object,
        Variable,
        Method,
        ObjectType,
        VariableType,
        ReferenceType,
        DataType,
        View
    }

    /// <summary>
    /// node model for webservice api
    /// </summary>
    public class NodeApiModel {

        /// <summary>
        /// Id of node and parent id.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
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
        /// If method node class, whether method can be called.
        /// </summary>
        [JsonProperty(PropertyName = "executable",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Executable { get; set; }

        /// <summary>
        /// If variable, datatype and value rank
        /// </summary>
        [JsonProperty(PropertyName = "dataType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataType { get; set; }

        /// <summary>
        /// Value rank
        /// </summary>
        [JsonProperty(PropertyName = "valueRank",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? ValueRank { get; set; }
    }
}
