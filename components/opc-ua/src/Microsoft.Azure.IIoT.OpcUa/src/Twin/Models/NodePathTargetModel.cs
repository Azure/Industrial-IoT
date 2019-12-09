// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Node path target
    /// </summary>
    public class NodePathTargetModel {

        /// <summary>
        /// The target browse path
        /// </summary>
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        public NodeModel Target { get; set; }

        /// <summary>
        /// Remaining index in path
        /// </summary>
        public int? RemainingPathIndex { get; set; }
    }
}
