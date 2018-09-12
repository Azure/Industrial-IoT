// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;

    /// <summary>
    /// reference model for twin module
    /// </summary>
    public class NodeReferenceApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public NodeReferenceApiModel() {}

        /// <summary>
        /// Create reference api model
        /// </summary>
        /// <param name="model"></param>
        public NodeReferenceApiModel(NodeReferenceModel model) {
            Id = model.Id;
            BrowseName = model.BrowseName;
            Direction = model.Direction;
            DisplayName = model.DisplayName;
            Target = new NodeApiModel(model.Target);
        }

        /// <summary>
        /// Reference Type id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Browse name of reference
        /// </summary>
        public string BrowseName { get; set; }

        /// <summary>
        /// Browse direction of reference
        /// </summary>
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        public NodeApiModel Target { get; set; }

        /// <summary>
        /// Display name of reference
        /// </summary>
        public string DisplayName { get; set; }
    }
}
