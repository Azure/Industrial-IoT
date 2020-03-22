// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Node path target
    /// </summary>
    [DataContract]
    public class NodePathTargetApiModel {

        /// <summary>
        /// The target browse path
        /// </summary>
        [DataMember(Name = "browsePath")]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        [DataMember(Name = "target")]
        public NodeApiModel Target { get; set; }

        /// <summary>
        /// Remaining index in path
        /// </summary>
        [DataMember(Name = "remainingPathIndex",
            EmitDefaultValue = false)]
        public int? RemainingPathIndex { get; set; }
    }
}
