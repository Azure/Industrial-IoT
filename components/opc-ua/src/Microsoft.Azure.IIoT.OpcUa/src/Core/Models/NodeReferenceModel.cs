// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Node reference
    /// </summary>
    public class NodeReferenceModel {

        /// <summary>
        /// Reference Type identifier
        /// </summary>
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Browse direction of reference
        /// </summary>
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        public NodeModel Target { get; set; }
    }
}
