// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Node vertex
    /// </summary>
    [DataContract]
    public abstract class BaseNodeVertexModel : AddressSpaceVertexModel {

        /// <summary>
        /// Node Class - also a vertex type discriminator
        /// </summary>
        [DataMember(Name = "nodeClass",
            EmitDefaultValue = false)]
        public NodeClass? NodeClass { get; set; }

        /// <summary>
        /// Modelling rule
        /// </summary>
        [DataMember(Name = "modellingRule",
            EmitDefaultValue = false)]
        public string ModellingRule { get; set; }

        /// <summary>
        /// Browse name of the node
        /// </summary>
        [DataMember(Name = "browseName",
            EmitDefaultValue = false)]
        public string BrowseName { get; set; }

        /// <summary>
        /// Display name .
        /// </summary>
        [DataMember(Name = "displayName",
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Description .
        /// </summary>
        [DataMember(Name = "description",
            EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// </summary>
        [DataMember(Name = "accessRestrictions",
            EmitDefaultValue = false)]
        public NodeAccessRestrictions? AccessRestrictions { get; set; }

        /// <summary>
        /// Default write mask for the node (default: 0)
        /// </summary>
        [DataMember(Name = "writeMask",
            EmitDefaultValue = false)]
        public uint? WriteMask { get; set; }

        /// <summary>
        /// Default user write mask for the node (default: 0)
        /// </summary>
        [DataMember(Name = "userWriteMask",
            EmitDefaultValue = false)]
        public uint? UserWriteMask { get; set; }

        /// <summary>
        /// Symbolic name
        /// </summary>
        [DataMember(Name = "symbolicName",
            EmitDefaultValue = false)]
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
