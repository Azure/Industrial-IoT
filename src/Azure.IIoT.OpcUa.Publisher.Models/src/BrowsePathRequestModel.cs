// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Browse nodes by path
    /// </summary>
    [DataContract]
    public sealed record class BrowsePathRequestModel
    {
        /// <summary>
        /// Node to browse from (defaults to root folder).
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0,
            EmitDefaultValue = false)]
        public string? NodeId { get; set; }

        /// <summary>
        /// The paths to browse from node.
        /// (mandatory)
        /// </summary>
        [DataMember(Name = "browsePaths", Order = 1)]
        [Required]
        public required IReadOnlyList<IReadOnlyList<string>> BrowsePaths { get; set; }

        /// <summary>
        /// Whether to read variable values on target nodes.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "readVariableValues", Order = 2,
            EmitDefaultValue = false)]
        public bool? ReadVariableValues { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 3,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }

        /// <summary>
        /// Whether to only return the raw node id
        /// information and not read the target node.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "nodeIdsOnly", Order = 4,
            EmitDefaultValue = false)]
        public bool? NodeIdsOnly { get; set; }
    }
}
