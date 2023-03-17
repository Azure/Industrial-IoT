// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Node path target
    /// </summary>
    [DataContract]
    public sealed record class NodePathTargetModel
    {
        /// <summary>
        /// The target browse path
        /// </summary>
        [DataMember(Name = "browsePath", Order = 0)]
        public IReadOnlyList<string> BrowsePath { get; set; } = null!;

        /// <summary>
        /// Target node
        /// </summary>
        [DataMember(Name = "target", Order = 1)]
        public NodeModel Target { get; set; } = null!;

        /// <summary>
        /// Remaining index in path
        /// </summary>
        [DataMember(Name = "remainingPathIndex", Order = 2,
            EmitDefaultValue = false)]
        public int? RemainingPathIndex { get; set; }
    }
}
