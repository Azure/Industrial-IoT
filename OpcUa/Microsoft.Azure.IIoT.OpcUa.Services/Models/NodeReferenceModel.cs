// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Models {
    /// <summary>
    /// Node reference
    /// </summary>
    public class NodeReferenceModel {

        /// <summary>
        /// Reference Type id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Browse name of reference
        /// </summary>
        public string BrowseName { get; set; }

        /// <summary>
        /// Display name of reference
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        public NodeModel Target { get; set; }
    }
}
