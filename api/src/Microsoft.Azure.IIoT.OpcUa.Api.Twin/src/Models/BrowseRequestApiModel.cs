// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Browse request model
    /// </summary>
    public class BrowseRequestApiModel {

        /// <summary>
        /// Node to browse.
        /// (defaults to root folder).
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Direction to browse in
        /// (default: forward)
        /// </summary>
        [JsonProperty(PropertyName = "direction",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// View to browse
        /// (default: null = new view = All nodes).
        /// </summary>
        [JsonProperty(PropertyName = "view",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public BrowseViewApiModel View { get; set; }

        /// <summary>
        /// Reference types to browse.
        /// (default: hierarchical).
        /// </summary>
        [JsonProperty(PropertyName = "referenceTypeId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Whether to include subtypes of the reference type.
        /// (default is false)
        /// </summary>
        [JsonProperty(PropertyName = "noSubtypes",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? NoSubtypes { get; set; }

        /// <summary>
        /// Max number of references to return. There might
        /// be less returned as this is up to the client
        /// restrictions.  Set to 0 to return no references
        /// or target nodes.
        /// (default is decided by client e.g. 60)
        /// </summary>
        [JsonProperty(PropertyName = "maxReferencesToReturn",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public uint? MaxReferencesToReturn { get; set; }

        /// <summary>
        /// Whether to collapse all references into a set of
        /// unique target nodes and not show reference
        /// information.
        /// (default is false)
        /// </summary>
        [JsonProperty(PropertyName = "targetNodesOnly",
           NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? TargetNodesOnly { get; set; }

        /// <summary>
        /// Whether to read variable values on target nodes.
        /// (default is false)
        /// </summary>
        [JsonProperty(PropertyName = "readVariableValues",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? ReadVariableValues { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
