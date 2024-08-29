// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Request node browsing continuation
    /// </summary>
    [DataContract]
    public sealed record class BrowseNextRequestModel
    {
        /// <summary>
        /// Continuation token from previews browse request.
        /// (mandatory)
        /// </summary>
        [DataMember(Name = "continuationToken", Order = 0)]
        [Required]
        public required string ContinuationToken { get; set; }

        /// <summary>
        /// Whether to abort browse and release.
        /// (default: false)
        /// </summary>
        [DataMember(Name = "abort", Order = 1,
            EmitDefaultValue = false)]
        public bool? Abort { get; set; }

        /// <summary>
        /// Whether to collapse all references into a set of
        /// unique target nodes and not show reference
        /// information.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "targetNodesOnly", Order = 2,
            EmitDefaultValue = false)]
        public bool? TargetNodesOnly { get; set; }

        /// <summary>
        /// Whether to read variable values on target nodes.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "readVariableValues", Order = 3,
            EmitDefaultValue = false)]
        public bool? ReadVariableValues { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 4,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }

        /// <summary>
        /// Whether to only return the raw node id
        /// information and not read the target node.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "nodeIdsOnly", Order = 5,
            EmitDefaultValue = false)]
        public bool? NodeIdsOnly { get; set; }
    }
}
