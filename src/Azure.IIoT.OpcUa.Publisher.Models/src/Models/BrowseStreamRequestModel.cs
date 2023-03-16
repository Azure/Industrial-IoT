// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Browse stream request model
    /// </summary>
    [DataContract]
    public sealed record class BrowseStreamRequestModel
    {
        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 0,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; init; }

        /// <summary>
        /// Start nodes to browse.
        /// (defaults to root folder).
        /// </summary>
        [DataMember(Name = "nodeId", Order = 1)]
        public IReadOnlyList<string>? NodeIds { get; init; }

        /// <summary>
        /// Direction to browse in
        /// (default: forward)
        /// </summary>
        [DataMember(Name = "direction", Order = 2,
            EmitDefaultValue = false)]
        public BrowseDirection? Direction { get; init; }

        /// <summary>
        /// View to browse
        /// (default: null = new view = All nodes).
        /// </summary>
        [DataMember(Name = "view", Order = 3,
            EmitDefaultValue = false)]
        public BrowseViewModel? View { get; init; }

        /// <summary>
        /// Reference types to browse.
        /// (default: hierarchical).
        /// </summary>
        [DataMember(Name = "referenceTypeId", Order = 4,
            EmitDefaultValue = false)]
        public string? ReferenceTypeId { get; init; }

        /// <summary>
        /// Whether to include subtypes of the reference type.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "noSubtypes", Order = 5,
            EmitDefaultValue = false)]
        public bool? NoSubtypes { get; init; }

        /// <summary>
        /// Whether to read variable values on source nodes.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "readVariableValues", Order = 6,
            EmitDefaultValue = false)]
        public bool? ReadVariableValues { get; init; }

        /// <summary>
        /// Whether to not browse recursively
        /// (default is false)
        /// </summary>
        [DataMember(Name = "noRecurse", Order = 7,
            EmitDefaultValue = false)]
        public bool? NoRecurse { get; init; }

        /// <summary>
        /// Filter returned target nodes by only returning
        /// nodes that have classes defined in this array.
        /// (default: null - all targets are returned)
        /// </summary>
        [DataMember(Name = "nodeClassFilter", Order = 8,
            EmitDefaultValue = false)]
        public IReadOnlyList<NodeClass>? NodeClassFilter { get; init; }
    }
}
