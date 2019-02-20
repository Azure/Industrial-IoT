// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Node class
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeClass {

        /// <summary>
        /// Object class
        /// </summary>
        Object,

        /// <summary>
        /// Variable
        /// </summary>
        Variable,

        /// <summary>
        /// Method class
        /// </summary>
        Method,

        /// <summary>
        /// Object type
        /// </summary>
        ObjectType,

        /// <summary>
        /// Variable type
        /// </summary>
        VariableType,

        /// <summary>
        /// Reference type
        /// </summary>
        ReferenceType,

        /// <summary>
        /// Data type
        /// </summary>
        DataType,

        /// <summary>
        /// View class
        /// </summary>
        View
    }

    /// <summary>
    /// Flags that can be set for the EventNotifier attribute.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeEventNotifier {

        /// <summary>
        /// The Object or View produces event notifications.
        /// </summary>
        SubscribeToEvents = 0x1,

        /// <summary>
        /// The Object has an event history which may
        /// be read.
        /// </summary>
        HistoryRead = 0x4,

        /// <summary>
        /// The Object has an event history which may
        /// be updated.
        /// </summary>
        HistoryWrite = 0x8,
    }

    /// <summary>
    /// Flags that can be set for the AccessLevel attribute.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeAccessLevel {

        /// <summary>
        /// The current value of the Variable may be read.
        /// </summary>
        CurrentRead = 0x1,

        /// <summary>
        /// The current value of the Variable may be written.
        /// </summary>
        CurrentWrite = 0x2,

        /// <summary>
        /// The history for the Variable may be read.
        /// </summary>
        HistoryRead = 0x4,

        /// <summary>
        /// The history for the Variable may be updated.
        /// </summary>
        HistoryWrite = 0x8,

        /// <summary>
        /// Indicates if the Variable generates SemanticChangeEvents
        /// when its value changes.
        /// </summary>
        SemanticChange = 0x10,

        /// <summary>
        /// Indicates if the current StatusCode of the value
        /// is writable.
        /// </summary>
        StatusWrite = 0x20,

        /// <summary>
        /// Indicates if the current SourceTimestamp is
        /// writable.
        /// </summary>
        TimestampWrite = 0x40,

        /// <summary>
        /// Reads are not atomic.
        /// </summary>
        NonatomicRead = 0x100,

        /// <summary>
        /// Writes are not atomic
        /// </summary>
        NonatomicWrite = 0x200,

        /// <summary>
        /// Writes cannot be performed with IndexRange.
        /// </summary>
        WriteFullArrayOnly = 0x400,
    }

    /// <summary>
    /// Constants defined for the ValueRank attribute.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeValueRank {

        /// <summary>
        /// The variable may be a scalar or a one
        /// dimensional array.
        /// </summary>
        ScalarOrOneDimension = -3,

        /// <summary>
        /// The variable may be a scalar or an array of
        /// any dimension.
        /// </summary>
        Any = -2,

        /// <summary>
        /// The variable is always a scalar.
        /// </summary>
        Scalar = -1,

        /// <summary>
        /// The variable is always an array with one or
        /// more dimensions.
        /// </summary>
        OneOrMoreDimensions = 0,

        /// <summary>
        /// The variable is always one dimensional array.
        /// </summary>
        OneDimension = 1,

        /// <summary>
        /// The variable is always an array with two or
        /// more dimensions.
        /// </summary>
        TwoDimensions = 2
    }

    /// <summary>
    /// Flags for use with the AccessRestrictions attribute.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeAccessRestrictions {

        /// <summary>
        /// Requires SecureChannel which digitally signs all messages.
        /// </summary>
        SigningRequired = 0x1,

        /// <summary>
        /// Requires SecureChannel which encrypts all messages.
        /// </summary>
        EncryptionRequired = 0x2,

        /// <summary>
        /// No SessionlessInvoke invocation.
        /// </summary>
        SessionRequired = 0x4
    }

    /// <summary>
    /// Node model
    /// </summary>
    public class NodeApiModel {

        /// <summary>
        /// Type of node
        /// </summary>
        [JsonProperty(PropertyName = "nodeClass",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeClass? NodeClass { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [JsonProperty(PropertyName = "displayName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Id of node (Mandatory).
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Description if any
        /// </summary>
        [JsonProperty(PropertyName = "description",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary>
        /// Browse name
        /// </summary>
        [JsonProperty(PropertyName = "browseName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string BrowseName { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// (default: none)
        /// </summary>
        [JsonProperty(PropertyName = "accessRestrictions",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeAccessRestrictions? AccessRestrictions { get; set; }

        /// <summary>
        /// Default write mask for the node
        /// (default: 0)
        /// </summary>
        [JsonProperty(PropertyName = "writeMask",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? WriteMask { get; set; }

        /// <summary>
        /// User write mask for the node
        /// (default: 0)
        /// </summary>
        [JsonProperty(PropertyName = "userWriteMask",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? UserWriteMask { get; set; }

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// (default: false)
        /// </summary>
        [JsonProperty(PropertyName = "isAbstract",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Whether a view contains loops. Null if
        /// not a view.
        /// </summary>
        [JsonProperty(PropertyName = "containsNoLoops",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? ContainsNoLoops { get; set; }

        /// <summary>
        /// If object or view and eventing, event notifier
        /// to subscribe to.
        /// (default: no events supported)
        /// </summary>
        [JsonProperty(PropertyName = "eventNotifier",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeEventNotifier? EventNotifier { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called.
        /// </summary>
        [JsonProperty(PropertyName = "executable",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Executable { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called by current user.
        /// (default: false if not executable)
        /// </summary>
        [JsonProperty(PropertyName = "userExecutable",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? UserExecutable { get; set; }

        /// <summary>
        /// Data type definition in case node is a
        /// data type node and definition is available,
        /// otherwise null.
        /// </summary>
        [JsonProperty(PropertyName = "dataTypeDefinition",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken DataTypeDefinition { get; set; }

        /// <summary>
        /// Default access level for variable node.
        /// (default: 0)
        /// </summary>
        [JsonProperty(PropertyName = "accessLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeAccessLevel? AccessLevel { get; set; }

        /// <summary>
        /// User access level for variable node or null.
        /// (default: 0)
        /// </summary>
        [JsonProperty(PropertyName = "userAccessLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeAccessLevel? UserAccessLevel { get; set; }

        /// <summary>
        /// If variable the datatype of the variable.
        /// (default: null)
        /// </summary>
        [JsonProperty(PropertyName = "dataType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataType { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable
        /// or variable type, otherwise null.
        /// (default: scalar = -1)
        /// </summary>
        [JsonProperty(PropertyName = "valueRank",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of variable or variable type.
        /// (default: empty array)
        /// </summary>
        [JsonProperty(PropertyName = "arrayDimensions",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Whether the value of a variable is historizing.
        /// (default: false)
        /// </summary>
        [JsonProperty(PropertyName = "historizing",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Historizing { get; set; }

        /// <summary>
        /// Minimum sampling interval for the variable
        /// value, otherwise null if not a variable node.
        /// (default: null)
        /// </summary>
        [JsonProperty(PropertyName = "minimumSamplingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public double? MinimumSamplingInterval { get; set; }

        /// <summary>
        /// Value of variable or default value of the
        /// subtyped variable in case node is a variable
        /// type, otherwise null.
        /// </summary>
        [JsonProperty(PropertyName = "value",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Value { get; set; }

        /// <summary>
        /// Inverse name of the reference if the node is
        /// a reference type, otherwise null.
        /// </summary>
        [JsonProperty(PropertyName = "inverseName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric in case
        /// the node is a reference type, otherwise
        /// null.
        /// </summary>
        [JsonProperty(PropertyName = "symmetric",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Symmetric { get; set; }

        /// <summary>
        /// Role permissions
        /// </summary>
        [JsonProperty(PropertyName = "rolePermissions",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<RolePermissionApiModel> RolePermissions { get; set; }

        /// <summary>
        /// User Role permissions
        /// </summary>
        [JsonProperty(PropertyName = "userRolePermissions",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<RolePermissionApiModel> UserRolePermissions { get; set; }

        /// <summary>
        /// Optional type definition of the node
        /// </summary>
        [JsonProperty(PropertyName = "typeDefinitionId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string TypeDefinitionId { get; set; }

        /// <summary>
        /// Whether node has children which are defined as
        /// any forward hierarchical references.
        /// (default: unknown)
        /// </summary>
        [JsonProperty(PropertyName = "children",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasChildren { get; set; }


        // Legacy

        /// <ignore/>
        [JsonIgnore]
        [Obsolete]
        public string Id => NodeId;

        /// <ignore/>
        [JsonIgnore]
        [Obsolete]
        public string Name => BrowseName;
    }
}
