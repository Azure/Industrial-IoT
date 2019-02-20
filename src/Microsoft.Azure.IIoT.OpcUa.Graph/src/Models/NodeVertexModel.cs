// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Node vertex
    /// </summary>
    public abstract class NodeVertexModel : AddressSpaceVertexModel {

        /// <summary>
        /// Node Class - also a vertex type discriminator
        /// </summary>
        [JsonProperty(PropertyName = "nodeClass")]
        public NodeClass? NodeClass { get; set; }

        /// <summary>
        /// Modelling rule
        /// </summary>
        [JsonProperty(PropertyName = "modellingRule")]
        public string ModellingRule { get; set; }

        /// <summary>
        /// Browse name of the node
        /// </summary>
        [JsonProperty(PropertyName = "browseName")]
        public string BrowseName { get; set; }

        /// <summary>
        /// Display name .
        /// </summary>
        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Description .
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// </summary>
        [JsonProperty(PropertyName = "accessRestrictions")]
        public NodeAccessRestrictions? AccessRestrictions { get; set; }

        /// <summary>
        /// Default write mask for the node (default: 0)
        /// </summary>
        [JsonProperty(PropertyName = "writeMask")]
        public uint? WriteMask { get; set; }

        /// <summary>
        /// Get all role permissions
        /// </summary>
        public RolePermissionEdgeModel RolePermissions { get; set; }

        /// <summary>
        /// Get all forward references
        /// </summary>
        public OriginEdgeModel Forward { get; set; }

        /// <summary>
        /// Get all backward references
        /// </summary>
        public TargetEdgeModel Backward { get; set; }
    }
}
