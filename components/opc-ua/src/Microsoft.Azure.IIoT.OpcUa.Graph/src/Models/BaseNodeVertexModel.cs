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
    public abstract class BaseNodeVertexModel : AddressSpaceVertexModel {

        /// <summary>
        /// Node Class - also a vertex type discriminator
        /// </summary>
        [JsonProperty(PropertyName = "nodeClass",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeClass? NodeClass { get; set; }

        /// <summary>
        /// Modelling rule
        /// </summary>
        [JsonProperty(PropertyName = "modellingRule",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ModellingRule { get; set; }

        /// <summary>
        /// Browse name of the node
        /// </summary>
        [JsonProperty(PropertyName = "browseName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string BrowseName { get; set; }

        /// <summary>
        /// Display name .
        /// </summary>
        [JsonProperty(PropertyName = "displayName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Description .
        /// </summary>
        [JsonProperty(PropertyName = "description",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// </summary>
        [JsonProperty(PropertyName = "accessRestrictions",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeAccessRestrictions? AccessRestrictions { get; set; }

        /// <summary>
        /// Default write mask for the node (default: 0)
        /// </summary>
        [JsonProperty(PropertyName = "writeMask",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? WriteMask { get; set; }

        /// <summary>
        /// Default user write mask for the node (default: 0)
        /// </summary>
        [JsonProperty(PropertyName = "userWriteMask",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? UserWriteMask { get; set; }

        /// <summary>
        /// Symbolic name
        /// </summary>
        [JsonProperty(PropertyName = "symbolicName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SymbolicName { get; set; }

        /// <summary>
        /// Get all role permissions
        /// </summary>
        public RolePermissionEdgeModel RolePermissions { get; set; }

        /// <summary>
        /// Get all user role permissions
        /// </summary>
        public UserRolePermissionEdgeModel UserRolePermissions { get; set; }

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
