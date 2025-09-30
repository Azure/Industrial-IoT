// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Node model
    /// </summary>
    [DataContract]
    public sealed record class NodeModel
    {
        /// <summary>
        /// Type of node
        /// </summary>
        [DataMember(Name = "nodeClass", Order = 0,
            EmitDefaultValue = false)]
        public NodeClass? NodeClass { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [DataMember(Name = "displayName", Order = 1,
            EmitDefaultValue = false)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Id of node.
        /// (Mandatory).
        /// </summary>
        [DataMember(Name = "nodeId", Order = 2)]
        [Required]
        public required string NodeId { get; set; }

        /// <summary>
        /// Description if any
        /// </summary>
        [DataMember(Name = "description", Order = 3,
            EmitDefaultValue = false)]
        public string? Description { get; set; }

        /// <summary>
        /// Browse name
        /// </summary>
        [DataMember(Name = "browseName", Order = 4,
            EmitDefaultValue = false)]
        public string? BrowseName { get; set; }

        /// <summary>
        /// Value of variable or default value of the
        /// subtyped variable in case node is a variable
        /// type, otherwise null.
        /// </summary>
        [DataMember(Name = "value", Order = 5,
            EmitDefaultValue = false)]
        [SkipValidation]
        public VariantValue? Value { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at source.
        /// </summary>
        [DataMember(Name = "sourcePicoseconds", Order = 6,
            EmitDefaultValue = false)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at source.
        /// </summary>
        [DataMember(Name = "sourceTimestamp", Order = 7,
            EmitDefaultValue = false)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at server.
        /// </summary>
        [DataMember(Name = "serverPicoseconds", Order = 8,
            EmitDefaultValue = false)]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at server.
        /// </summary>
        [DataMember(Name = "serverTimestamp", Order = 9,
            EmitDefaultValue = false)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Service result in case of error reading the value
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 10,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// (default: none)
        /// </summary>
        [DataMember(Name = "accessRestrictions", Order = 11,
            EmitDefaultValue = false)]
        public NodeAccessRestrictions? AccessRestrictions { get; set; }

        /// <summary>
        /// Default write mask for the node
        /// (default: 0)
        /// </summary>
        [DataMember(Name = "writeMask", Order = 12,
            EmitDefaultValue = false)]
        public uint? WriteMask { get; set; }

        /// <summary>
        /// User write mask for the node
        /// (default: 0)
        /// </summary>
        [DataMember(Name = "userWriteMask", Order = 13,
            EmitDefaultValue = false)]
        public uint? UserWriteMask { get; set; }

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// (default: false)
        /// </summary>
        [DataMember(Name = "isAbstract", Order = 14,
            EmitDefaultValue = false)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Whether a view contains loops. Null if
        /// not a view.
        /// </summary>
        [DataMember(Name = "containsNoLoops", Order = 15,
            EmitDefaultValue = false)]
        public bool? ContainsNoLoops { get; set; }

        /// <summary>
        /// If object or view and eventing, event notifier
        /// to subscribe to.
        /// (default: no events supported)
        /// </summary>
        [DataMember(Name = "eventNotifier", Order = 16,
            EmitDefaultValue = false)]
        public NodeEventNotifier? EventNotifier { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called.
        /// </summary>
        [DataMember(Name = "executable", Order = 17,
            EmitDefaultValue = false)]
        public bool? Executable { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called by current user.
        /// (default: false if not executable)
        /// </summary>
        [DataMember(Name = "userExecutable", Order = 18,
            EmitDefaultValue = false)]
        public bool? UserExecutable { get; set; }

        /// <summary>
        /// Data type definition in case node is a
        /// data type node and definition is available,
        /// otherwise null.
        /// </summary>
        [DataMember(Name = "dataTypeDefinition", Order = 19,
            EmitDefaultValue = false)]
        [SkipValidation]
        public VariantValue? DataTypeDefinition { get; set; }

        /// <summary>
        /// Default access level for value in variable
        /// node or null if not a variable.
        /// (default: No access)
        /// </summary>
        [DataMember(Name = "accessLevel", Order = 20,
            EmitDefaultValue = false)]
        public NodeAccessLevel? AccessLevel { get; set; }

        /// <summary>
        /// User access level for value in variable node
        /// or null.
        /// (default: No access)
        /// </summary>
        [DataMember(Name = "userAccessLevel", Order = 21,
            EmitDefaultValue = false)]
        public NodeAccessLevel? UserAccessLevel { get; set; }

        /// <summary>
        /// If variable the datatype of the variable.
        /// (default: null)
        /// </summary>
        [DataMember(Name = "dataType", Order = 22,
            EmitDefaultValue = false)]
        public string? DataType { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable
        /// or variable type, otherwise null.
        /// (default: scalar = -1)
        /// </summary>
        [DataMember(Name = "valueRank", Order = 23,
            EmitDefaultValue = false)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of variable or variable type.
        /// (default: empty array)
        /// </summary>
        [DataMember(Name = "arrayDimensions", Order = 24,
            EmitDefaultValue = false)]
        public IReadOnlyList<uint>? ArrayDimensions { get; set; }

        /// <summary>
        /// Whether the value of a variable is historizing.
        /// (default: false)
        /// </summary>
        [DataMember(Name = "historizing", Order = 25,
            EmitDefaultValue = false)]
        public bool? Historizing { get; set; }

        /// <summary>
        /// Minimum sampling interval for the variable
        /// value, otherwise null if not a variable node.
        /// (default: null)
        /// </summary>
        [DataMember(Name = "minimumSamplingInterval", Order = 26,
            EmitDefaultValue = false)]
        public double? MinimumSamplingInterval { get; set; }

        /// <summary>
        /// Inverse name of the reference if the node is
        /// a reference type, otherwise null.
        /// </summary>
        [DataMember(Name = "inverseName", Order = 27,
            EmitDefaultValue = false)]
        public string? InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric in case
        /// the node is a reference type, otherwise
        /// null.
        /// </summary>
        [DataMember(Name = "symmetric", Order = 28,
            EmitDefaultValue = false)]
        public bool? Symmetric { get; set; }

        /// <summary>
        /// Role permissions
        /// </summary>
        [DataMember(Name = "rolePermissions", Order = 29,
            EmitDefaultValue = false)]
        public IReadOnlyList<RolePermissionModel>? RolePermissions { get; set; }

        /// <summary>
        /// User Role permissions
        /// </summary>
        [DataMember(Name = "userRolePermissions", Order = 30,
            EmitDefaultValue = false)]
        public IReadOnlyList<RolePermissionModel>? UserRolePermissions { get; set; }

        /// <summary>
        /// Optional type definition of the node
        /// </summary>
        [DataMember(Name = "typeDefinitionId", Order = 31,
            EmitDefaultValue = false)]
        public string? TypeDefinitionId { get; set; }

        /// <summary>
        /// Whether node has children which are defined as
        /// any forward hierarchical references.
        /// (default: unknown)
        /// </summary>
        [DataMember(Name = "children", Order = 32,
            EmitDefaultValue = false)]
        public bool? Children { get; set; }
    }
}
