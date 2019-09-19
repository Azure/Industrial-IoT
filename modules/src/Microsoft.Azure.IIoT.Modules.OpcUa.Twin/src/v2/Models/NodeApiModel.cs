// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Node model for module
    /// </summary>
    public class NodeApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public NodeApiModel() { }

        /// <summary>
        /// Create node api model from service model
        /// </summary>
        /// <param name="model"></param>
        public NodeApiModel(NodeModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NodeId = model.NodeId;
            Children = model.Children;
            BrowseName = model.BrowseName;
            DisplayName = model.DisplayName;
            Description = model.Description;
            NodeClass = model.NodeClass;
            IsAbstract = model.IsAbstract;
            AccessLevel = model.AccessLevel;
            EventNotifier = model.EventNotifier;
            Executable = model.Executable;
            DataType = model.DataType;
            ValueRank = model.ValueRank;
            AccessRestrictions = model.AccessRestrictions;
            ArrayDimensions = model.ArrayDimensions;
            ContainsNoLoops = model.ContainsNoLoops;
            DataTypeDefinition = model.DataTypeDefinition;
            Value = model.Value;
            Historizing = model.Historizing;
            InverseName = model.InverseName;
            MinimumSamplingInterval = model.MinimumSamplingInterval;
            Symmetric = model.Symmetric;
            UserAccessLevel = model.UserAccessLevel;
            UserExecutable = model.UserExecutable;
            UserWriteMask = model.UserWriteMask;
            WriteMask = model.WriteMask;
            RolePermissions = model.RolePermissions?
                .Select(p => p == null ? null : new RolePermissionApiModel(p))
                .ToList();
            UserRolePermissions = model.UserRolePermissions?
                .Select(p => p == null ? null : new RolePermissionApiModel(p))
                .ToList();
            TypeDefinitionId = model.TypeDefinitionId;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public NodeModel ToServiceModel() {
            return new NodeModel {
                NodeId = NodeId,
                Children = Children,
                BrowseName = BrowseName,
                DisplayName = DisplayName,
                Description = Description,
                NodeClass = NodeClass,
                IsAbstract = IsAbstract,
                AccessLevel = AccessLevel,
                EventNotifier = EventNotifier,
                Executable = Executable,
                DataType = DataType,
                ValueRank = ValueRank,
                AccessRestrictions = AccessRestrictions,
                ArrayDimensions = ArrayDimensions,
                ContainsNoLoops = ContainsNoLoops,
                DataTypeDefinition = DataTypeDefinition,
                Value = Value,
                Historizing = Historizing,
                InverseName = InverseName,
                MinimumSamplingInterval = MinimumSamplingInterval,
                Symmetric = Symmetric,
                UserAccessLevel = UserAccessLevel,
                UserExecutable = UserExecutable,
                UserWriteMask = UserWriteMask,
                WriteMask = WriteMask,
                RolePermissions = RolePermissions?
                    .Select(p => p.ToServiceModel())
                    .ToList(),
                UserRolePermissions = UserRolePermissions?
                    .Select(p => p.ToServiceModel())
                    .ToList(),
                TypeDefinitionId = TypeDefinitionId
            };
        }

        /// <summary>
        /// Type of node
        /// </summary>
        [JsonProperty(PropertyName = "NodeClass",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeClass? NodeClass { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [JsonProperty(PropertyName = "DisplayName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Id of node.
        /// (Mandatory).
        /// </summary>
        [JsonProperty(PropertyName = "NodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Description if any
        /// </summary>
        [JsonProperty(PropertyName = "Description",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary>
        /// Browse name
        /// </summary>
        [JsonProperty(PropertyName = "BrowseName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string BrowseName { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// (default: none)
        /// </summary>
        [JsonProperty(PropertyName = "AccessRestrictions",
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
        [JsonProperty(PropertyName = "UserWriteMask",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? UserWriteMask { get; set; }

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// (default: false)
        /// </summary>
        [JsonProperty(PropertyName = "IsAbstract",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Whether a view contains loops. Null if
        /// not a view.
        /// </summary>
        [JsonProperty(PropertyName = "ContainsNoLoops",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? ContainsNoLoops { get; set; }

        /// <summary>
        /// If object or view and eventing, event notifier
        /// to subscribe to.
        /// (default: no events supported)
        /// </summary>
        [JsonProperty(PropertyName = "EventNotifier",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeEventNotifier? EventNotifier { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called.
        /// </summary>
        [JsonProperty(PropertyName = "Executable",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Executable { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called by current user.
        /// (default: false if not executable)
        /// </summary>
        [JsonProperty(PropertyName = "UserExecutable",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? UserExecutable { get; set; }

        /// <summary>
        /// Data type definition in case node is a
        /// data type node and definition is available,
        /// otherwise null.
        /// </summary>
        [JsonProperty(PropertyName = "DataTypeDefinition",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken DataTypeDefinition { get; set; }

        /// <summary>
        /// Default access level for variable node.
        /// (default: 0)
        /// </summary>
        [JsonProperty(PropertyName = "AccessLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeAccessLevel? AccessLevel { get; set; }

        /// <summary>
        /// User access level for variable node or null.
        /// (default: 0)
        /// </summary>
        [JsonProperty(PropertyName = "UserAccessLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeAccessLevel? UserAccessLevel { get; set; }

        /// <summary>
        /// If variable the datatype of the variable.
        /// (default: null)
        /// </summary>
        [JsonProperty(PropertyName = "DataType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataType { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable
        /// or variable type, otherwise null.
        /// (default: scalar = -1)
        /// </summary>
        [JsonProperty(PropertyName = "ValueRank",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of variable or variable type.
        /// (default: empty array)
        /// </summary>
        [JsonProperty(PropertyName = "ArrayDimensions",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Whether the value of a variable is historizing.
        /// (default: false)
        /// </summary>
        [JsonProperty(PropertyName = "Historizing",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Historizing { get; set; }

        /// <summary>
        /// Minimum sampling interval for the variable
        /// value, otherwise null if not a variable node.
        /// (default: null)
        /// </summary>
        [JsonProperty(PropertyName = "MinimumSamplingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public double? MinimumSamplingInterval { get; set; }

        /// <summary>
        /// Value of variable or default value of the
        /// subtyped variable in case node is a variable
        /// type, otherwise null.
        /// </summary>
        [JsonProperty(PropertyName = "Value",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Value { get; set; }

        /// <summary>
        /// Inverse name of the reference if the node is
        /// a reference type, otherwise null.
        /// </summary>
        [JsonProperty(PropertyName = "InverseName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric in case
        /// the node is a reference type, otherwise
        /// null.
        /// </summary>
        [JsonProperty(PropertyName = "Symmetric",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Symmetric { get; set; }

        /// <summary>
        /// Role permissions
        /// </summary>
        [JsonProperty(PropertyName = "RolePermissions",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<RolePermissionApiModel> RolePermissions { get; set; }

        /// <summary>
        /// User Role permissions
        /// </summary>
        [JsonProperty(PropertyName = "UserRolePermissions",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<RolePermissionApiModel> UserRolePermissions { get; set; }

        /// <summary>
        /// Optional type definition of the node
        /// </summary>
        [JsonProperty(PropertyName = "TypeDefinitionId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string TypeDefinitionId { get; set; }

        /// <summary>
        /// Whether node has children which are defined as
        /// any forward hierarchical references.
        /// (default: unknown)
        /// </summary>
        [JsonProperty(PropertyName = "Children",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Children { get; set; }
    }
}
