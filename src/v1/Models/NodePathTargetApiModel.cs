// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// Node path target
    /// </summary>
    public class NodePathTargetApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public NodePathTargetApiModel() { }

        /// <summary>
        /// Create reference api model
        /// </summary>
        /// <param name="model"></param>
        public NodePathTargetApiModel(NodePathTargetModel model) {
            RemainingPathIndex = model.RemainingPathIndex;
            Target = new NodeApiModel(model.Target);
        }

        /// <summary>
        /// Target node
        /// </summary>
        public NodeApiModel Target { get; set; }

        /// <summary>
        /// Remaining index in path
        /// </summary>
        public int? RemainingPathIndex { get; set; }
    }
}
