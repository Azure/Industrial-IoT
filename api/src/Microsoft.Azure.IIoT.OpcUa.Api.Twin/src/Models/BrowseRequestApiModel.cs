// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Browse request model
    /// </summary>
    [DataContract]
    public class BrowseRequestApiModel {

        /// <summary>
        /// Node to browse.
        /// (defaults to root folder).
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0)]
        public string NodeId { get; set; }

        /// <summary>
        /// Direction to browse in
        /// (default: forward)
        /// </summary>
        [DataMember(Name = "direction", Order = 1,
            EmitDefaultValue = false)]
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// View to browse
        /// (default: null = new view = All nodes).
        /// </summary>
        [DataMember(Name = "view", Order = 2,
            EmitDefaultValue = false)]
        public BrowseViewApiModel View { get; set; }

        /// <summary>
        /// Reference types to browse.
        /// (default: hierarchical).
        /// </summary>
        [DataMember(Name = "referenceTypeId", Order = 3,
            EmitDefaultValue = false)]
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Whether to include subtypes of the reference type.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "noSubtypes", Order = 4,
            EmitDefaultValue = false)]
        public bool? NoSubtypes { get; set; }

        /// <summary>
        /// Max number of references to return. There might
        /// be less returned as this is up to the client
        /// restrictions.  Set to 0 to return no references
        /// or target nodes.
        /// (default is decided by client e.g. 60)
        /// </summary>
        [DataMember(Name = "maxReferencesToReturn", Order = 5,
            EmitDefaultValue = false)]
        public uint? MaxReferencesToReturn { get; set; }

        /// <summary>
        /// Whether to collapse all references into a set of
        /// unique target nodes and not show reference
        /// information.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "targetNodesOnly", Order = 6,
           EmitDefaultValue = false)]
        public bool? TargetNodesOnly { get; set; }

        /// <summary>
        /// Whether to read variable values on target nodes.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "readVariableValues", Order = 7,
            EmitDefaultValue = false)]
        public bool? ReadVariableValues { get; set; }

        /// <summary>
        /// Filter returned target nodes by only returning
        /// nodes that have classes defined in this array.
        /// (default: null - all targets are returned)
        /// </summary>
        [DataMember(Name = "nodeClassFilter", Order = 8,
            EmitDefaultValue = false)]
        public List<NodeClass> NodeClassFilter { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 9,
            EmitDefaultValue = false)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
