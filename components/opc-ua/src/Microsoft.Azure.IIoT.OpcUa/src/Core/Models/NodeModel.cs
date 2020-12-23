// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Node model
    /// </summary>
    public class NodeModel {

        /// <summary>
        /// Type of node
        /// </summary>
        public NodeClass? NodeClass { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Id of node.
        /// (Mandatory).
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Description if any
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Browse name
        /// </summary>
        public string BrowseName { get; set; }

        /// <summary>
        /// Value of variable or default value of the
        /// subtyped variable in case node is a variable
        /// type, otherwise null.
        /// </summary>
        public VariantValue Value { get; set; }

        /// <summary>
        /// Value source time stamp
        /// </summary>
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Value Source picoseconds
        /// </summary>
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Value server time stamp
        /// </summary>
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Value server picoseconds
        /// </summary>
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Value read result
        /// </summary>
        public ServiceResultModel ErrorInfo { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// (default: null)
        /// </summary>
        public NodeAccessRestrictions? AccessRestrictions { get; set; }

        /// <summary>
        /// Default write mask for the node
        /// (default: 0)
        /// </summary>
        public uint? WriteMask { get; set; }

        /// <summary>
        /// User write mask for the node
        /// (default: 0)
        /// </summary>
        public uint? UserWriteMask { get; set; }

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// (default: false)
        /// </summary>
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Whether a view contains loops. Null if
        /// not a view.
        /// </summary>
        public bool? ContainsNoLoops { get; set; }

        /// <summary>
        /// If object or view and eventing, event notifier
        /// to subscribe to.
        /// (default: no events supported)
        /// </summary>
        public NodeEventNotifier? EventNotifier { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called.
        /// </summary>
        public bool? Executable { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called by current user.
        /// (default: false if not executable)
        /// </summary>
        public bool? UserExecutable { get; set; }

        /// <summary>
        /// Data type definition as extension object
        /// in case node is a data type node and definition
        /// is available, otherwise null.
        /// </summary>
        public VariantValue DataTypeDefinition { get; set; }

        /// <summary>
        /// Default access level for value in variable
        /// node or null if not a variable.
        /// (default: No access)
        /// </summary>
        public NodeAccessLevel? AccessLevel { get; set; }

        /// <summary>
        /// User access level for value in variable node
        /// or null.
        /// (default: No access)
        /// </summary>
        public NodeAccessLevel? UserAccessLevel { get; set; }

        /// <summary>
        /// If variable the datatype of the variable.
        /// (default: null)
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable
        /// or variable type, otherwise null.
        /// </summary>
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of variable or variable type.
        /// (default: empty array)
        /// </summary>
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Whether the value of a variable is historizing.
        /// (default: false)
        /// </summary>
        public bool? Historizing { get; set; }

        /// <summary>
        /// Minimum sampling interval for the variable
        /// value, otherwise null if not a variable node.
        /// (default: null)
        /// </summary>
        public double? MinimumSamplingInterval { get; set; }

        /// <summary>
        /// Inverse name of the reference if the node is
        /// a reference type, otherwise null.
        /// </summary>
        public string InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric in case
        /// the node is a reference type, otherwise
        /// null.
        /// </summary>
        public bool? Symmetric { get; set; }

        /// <summary>
        /// Role permissions
        /// </summary>
        public List<RolePermissionModel> RolePermissions { get; set; }

        /// <summary>
        /// User Role permissions
        /// </summary>
        public List<RolePermissionModel> UserRolePermissions { get; set; }

        /// <summary>
        /// Optional type definition of the node
        /// </summary>
        public string TypeDefinitionId { get; set; }

        /// <summary>
        /// Whether node has children which are defined as
        /// any forward hierarchical references.
        /// (default: unknown)
        /// </summary>
        public bool? Children { get; set; }
    }
}
