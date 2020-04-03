// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request node browsing continuation
    /// </summary>
    [DataContract]
    public class BrowseNextRequestApiModel {

        /// <summary>
        /// Continuation token from previews browse request.
        /// (mandatory)
        /// </summary>
        [DataMember(Name = "continuationToken", Order = 0)]
        [Required]
        public string ContinuationToken { get; set; }

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
        public RequestHeaderApiModel Header { get; set; }
    }
}
