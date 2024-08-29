// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Browse stream chunk
    /// </summary>
    [DataContract]
    public sealed record class BrowseStreamChunkModel
    {
        /// <summary>
        /// Source node id
        /// </summary>
        [DataMember(Name = "sourceId", Order = 0)]
        public required string SourceId { get; init; }

        /// <summary>
        /// Source node attributes if this chunk contains
        /// the source node attributes that were read.
        /// This can be null, then reference is not
        /// null.
        /// </summary>
        [DataMember(Name = "attributes", Order = 1,
            EmitDefaultValue = false)]
        public NodeModel? Attributes { get; init; }

        /// <summary>
        /// References read from the source node to a target
        /// node. This can be null, then attributes is not
        /// null.
        /// </summary>
        [DataMember(Name = "reference", Order = 2,
            EmitDefaultValue = false)]
        public NodeReferenceModel? Reference { get; init; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 3,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; init; }
    }
}
