// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// node model for twin module
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
            Id = model.Id;
            HasChildren = model.HasChildren;
            IsAbstract = model.IsAbstract;
            DisplayName = model.DisplayName;
            Description = model.Description;
            NodeClass = model.NodeClass;
            AccessLevel = model.AccessLevel;
            EventNotifier = model.EventNotifier;
            Executable = model.Executable;
            DataType = model.DataType;
            ValueRank = model.ValueRank;
            AccessRestrictions = model.AccessRestrictions;
            ArrayDimensions = model.ArrayDimensions;
            ContainsNoLoops = model.ContainsNoLoops;
            DataTypeDefinition = model.DataTypeDefinition;
            DefaultValue = model.DefaultValue;
            Historizing = model.Historizing;
            InverseName = model.InverseName;
            MinimumSamplingInterval = model.MinimumSamplingInterval;
            Symmetric = model.Symmetric;
            UserAccessLevel = model.UserAccessLevel;
            UserExecutable = model.UserExecutable;
            UserWriteMask = model.UserWriteMask;
            WriteMask = model.WriteMask;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public NodeModel ToServiceModel() {
            return new NodeModel {
                Id = Id,
                HasChildren = HasChildren,
                IsAbstract = IsAbstract,
                DisplayName = DisplayName,
                Description = Description,
                NodeClass = NodeClass,
                AccessLevel = AccessLevel,
                EventNotifier = EventNotifier,
                Executable = Executable,
                DataType = DataType,
                ValueRank = ValueRank,
                AccessRestrictions = AccessRestrictions,
                ArrayDimensions = ArrayDimensions,
                ContainsNoLoops = ContainsNoLoops,
                DataTypeDefinition = DataTypeDefinition,
                DefaultValue = DefaultValue,
                Historizing = Historizing,
                InverseName = InverseName,
                MinimumSamplingInterval = MinimumSamplingInterval,
                Symmetric = Symmetric,
                UserAccessLevel = UserAccessLevel,
                UserExecutable = UserExecutable,
                UserWriteMask = UserWriteMask,
                WriteMask = WriteMask
            };
        }

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
        public string Id { get; set; }

        /// <summary>
        /// Description if any
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether node has children which are defined as
        /// any forward hierarchical references.
        /// (default: unknown)
        /// </summary>
        public bool? HasChildren { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// (default: none)
        /// </summary>
        public uint? AccessRestrictions { get; set; }

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
        /// (default: 0)
        /// </summary>
        public byte? EventNotifier { get; set; }

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
        /// Data type definition in case node is a
        /// data type node and definition is available,
        /// otherwise null.
        /// </summary>
        public JToken DataTypeDefinition { get; set; }

        /// <summary>
        /// Default access level for variable node.
        /// (default: 0)
        /// </summary>
        public uint? AccessLevel { get; set; }

        /// <summary>
        /// User access level for variable node or null.
        /// (default: 0)
        /// </summary>
        public uint? UserAccessLevel { get; set; }

        /// <summary>
        /// If variable the datatype of the variable.
        /// (default: null)
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable
        /// or variable type, otherwise null.
        /// (default: scalar = -1)
        /// </summary>
        public int? ValueRank { get; set; }

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
        /// Default value of the variable in case node
        /// is a variable type, otherwise null..
        /// </summary>
        public JToken DefaultValue { get; set; }

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
    }
}
