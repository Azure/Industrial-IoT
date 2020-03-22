// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Node model
    /// </summary>
    [DataContract]
    public class NodeApiModel {

        /// <summary>
        /// Type of node
        /// </summary>
        [DataMember(Name = "nodeClass",
            EmitDefaultValue = false)]
        public NodeClass? NodeClass { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [DataMember(Name = "displayName",
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Id of node.
        /// (Mandatory).
        /// </summary>
        [DataMember(Name = "nodeId")]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Description if any
        /// </summary>
        [DataMember(Name = "description",
            EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Browse name
        /// </summary>
        [DataMember(Name = "browseName",
            EmitDefaultValue = false)]
        public string BrowseName { get; set; }

        /// <summary>
        /// Value of variable or default value of the
        /// subtyped variable in case node is a variable
        /// type, otherwise null.
        /// </summary>
        [DataMember(Name = "value",
            EmitDefaultValue = false)]
        public VariantValue Value { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at source.
        /// </summary>
        [DataMember(Name = "sourcePicoseconds",
            EmitDefaultValue = false)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at source.
        /// </summary>
        [DataMember(Name = "sourceTimestamp",
            EmitDefaultValue = false)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at server.
        /// </summary>
        [DataMember(Name = "serverPicoseconds",
            EmitDefaultValue = false)]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at server.
        /// </summary>
        [DataMember(Name = "serverTimestamp",
            EmitDefaultValue = false)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Service result in case of error reading the value
        /// </summary>
        [DataMember(Name = "errorInfo",
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// (default: none)
        /// </summary>
        [DataMember(Name = "accessRestrictions",
            EmitDefaultValue = false)]
        public NodeAccessRestrictions? AccessRestrictions { get; set; }

        /// <summary>
        /// Default write mask for the node
        /// (default: 0)
        /// </summary>
        [DataMember(Name = "writeMask",
            EmitDefaultValue = false)]
        public uint? WriteMask { get; set; }

        /// <summary>
        /// User write mask for the node
        /// (default: 0)
        /// </summary>
        [DataMember(Name = "userWriteMask",
            EmitDefaultValue = false)]
        public uint? UserWriteMask { get; set; }

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// (default: false)
        /// </summary>
        [DataMember(Name = "isAbstract",
            EmitDefaultValue = false)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Whether a view contains loops. Null if
        /// not a view.
        /// </summary>
        [DataMember(Name = "containsNoLoops",
            EmitDefaultValue = false)]
        public bool? ContainsNoLoops { get; set; }

        /// <summary>
        /// If object or view and eventing, event notifier
        /// to subscribe to.
        /// (default: no events supported)
        /// </summary>
        [DataMember(Name = "eventNotifier",
            EmitDefaultValue = false)]
        public NodeEventNotifier? EventNotifier { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called.
        /// </summary>
        [DataMember(Name = "executable",
            EmitDefaultValue = false)]
        public bool? Executable { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called by current user.
        /// (default: false if not executable)
        /// </summary>
        [DataMember(Name = "userExecutable",
            EmitDefaultValue = false)]
        public bool? UserExecutable { get; set; }

        /// <summary>
        /// Data type definition in case node is a
        /// data type node and definition is available,
        /// otherwise null.
        /// </summary>
        [DataMember(Name = "dataTypeDefinition",
            EmitDefaultValue = false)]
        public VariantValue DataTypeDefinition { get; set; }

        /// <summary>
        /// Default access level for variable node.
        /// (default: 0)
        /// </summary>
        [DataMember(Name = "accessLevel",
            EmitDefaultValue = false)]
        public NodeAccessLevel? AccessLevel { get; set; }

        /// <summary>
        /// User access level for variable node or null.
        /// (default: 0)
        /// </summary>
        [DataMember(Name = "userAccessLevel",
            EmitDefaultValue = false)]
        public NodeAccessLevel? UserAccessLevel { get; set; }

        /// <summary>
        /// If variable the datatype of the variable.
        /// (default: null)
        /// </summary>
        [DataMember(Name = "dataType",
            EmitDefaultValue = false)]
        public string DataType { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable
        /// or variable type, otherwise null.
        /// (default: scalar = -1)
        /// </summary>
        [DataMember(Name = "valueRank",
            EmitDefaultValue = false)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of variable or variable type.
        /// (default: empty array)
        /// </summary>
        [DataMember(Name = "arrayDimensions",
            EmitDefaultValue = false)]
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Whether the value of a variable is historizing.
        /// (default: false)
        /// </summary>
        [DataMember(Name = "historizing",
            EmitDefaultValue = false)]
        public bool? Historizing { get; set; }

        /// <summary>
        /// Minimum sampling interval for the variable
        /// value, otherwise null if not a variable node.
        /// (default: null)
        /// </summary>
        [DataMember(Name = "minimumSamplingInterval",
            EmitDefaultValue = false)]
        public double? MinimumSamplingInterval { get; set; }

        /// <summary>
        /// Inverse name of the reference if the node is
        /// a reference type, otherwise null.
        /// </summary>
        [DataMember(Name = "inverseName",
            EmitDefaultValue = false)]
        public string InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric in case
        /// the node is a reference type, otherwise
        /// null.
        /// </summary>
        [DataMember(Name = "symmetric",
            EmitDefaultValue = false)]
        public bool? Symmetric { get; set; }

        /// <summary>
        /// Role permissions
        /// </summary>
        [DataMember(Name = "rolePermissions",
            EmitDefaultValue = false)]
        public List<RolePermissionApiModel> RolePermissions { get; set; }

        /// <summary>
        /// User Role permissions
        /// </summary>
        [DataMember(Name = "userRolePermissions",
            EmitDefaultValue = false)]
        public List<RolePermissionApiModel> UserRolePermissions { get; set; }

        /// <summary>
        /// Optional type definition of the node
        /// </summary>
        [DataMember(Name = "typeDefinitionId",
            EmitDefaultValue = false)]
        public string TypeDefinitionId { get; set; }

        /// <summary>
        /// Whether node has children which are defined as
        /// any forward hierarchical references.
        /// (default: unknown)
        /// </summary>
        [DataMember(Name = "children",
            EmitDefaultValue = false)]
        public bool? Children { get; set; }


        // Legacy

        /// <ignore/>
        [IgnoreDataMember]
        [Obsolete]
        public string Id => NodeId;
        /// <ignore/>
        [IgnoreDataMember]
        [Obsolete]
        public bool? HasChildren => Children;
        /// <ignore/>
        [IgnoreDataMember]
        [Obsolete]
        public string Name => BrowseName;
    }
}
