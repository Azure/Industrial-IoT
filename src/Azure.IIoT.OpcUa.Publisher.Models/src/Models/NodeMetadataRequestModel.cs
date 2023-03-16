// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Node metadata request model
    /// </summary>
    [DataContract]
    public sealed record class NodeMetadataRequestModel
    {
        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 0,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }

        /// <summary>
        /// Node id of the type.
        /// (Required)
        /// </summary>
        [DataMember(Name = "nodeId", Order = 1)]
        public string? NodeId { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// NodeId to the actual node.
        /// </summary>
        [DataMember(Name = "browsePath", Order = 2,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? BrowsePath { get; set; }
    }
}
