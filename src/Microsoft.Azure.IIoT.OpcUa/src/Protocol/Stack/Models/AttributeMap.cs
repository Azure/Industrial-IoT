// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents validity and default value map for attributes
    /// </summary>
    public class AttributeMap {

        const int Object = 0;
        const int Variable = 1;
        const int Method = 2;
        const int ObjectType = 3;
        const int VariableType = 4;
        const int ReferenceType = 5;
        const int DataType = 6;
        const int View = 7;

        /// <summary>
        /// Get all valid attributes for the node class
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <returns></returns>
        public IEnumerable<uint> GetNodeClassAttributes(NodeClass nodeClass) {
            for (uint i = 0; i < 32; i++) {
                if (_map[NodeClassId(nodeClass), i] != null) {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Returns default value
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <param name="attributeId"></param>
        /// <param name="optional"></param>
        /// <returns></returns>
        public object GetDefault(NodeClass nodeClass, uint attributeId, ref bool optional) {
            if (attributeId > 32) {
                return null;
            }
            var entry = _map[NodeClassId(nodeClass), attributeId];
            if (entry != null) {
                optional = entry.Optional;
                return entry.Value;
            }
            return null;
        }

        /// <summary>
        /// Validate attributes for nodeclass
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <param name="attributeId"></param>
        /// <returns></returns>
        public void Validate(NodeClass nodeClass, uint attributeId) {
            if (attributeId > 32 || _map[NodeClassId(nodeClass), attributeId] == null) {
                throw new ServiceResultException(StatusCodes.BadNodeAttributesInvalid);
            }
        }

        /// <summary>
        /// Read and decode a node attribute
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="attributeId"></param>
        public object Decode(IDecoder decoder, uint attributeId) {
            decoder.PushNamespace(Namespaces.OpcUa);
            try {
                var field = Attributes.GetBrowseName(attributeId);
                switch (attributeId) {
                    case Attributes.DisplayName:
                    case Attributes.InverseName:
                    case Attributes.Description:
                        return decoder.ReadLocalizedText(field);
                    case Attributes.WriteMask:
                    case Attributes.UserWriteMask:
                    case Attributes.AccessLevelEx:
                        var uint32Value = decoder.ReadUInt32(field);
                        if (uint32Value != 0) {
                            return 0;
                        }
                        break;
                    case Attributes.NodeId:
                    case Attributes.DataType:
                        var nodeIdValue = decoder.ReadNodeId(field);
                        if (nodeIdValue != null) {
                            return nodeIdValue;
                        }
                        break;
                    case Attributes.NodeClass:
                        return decoder.ReadEnumerated(field, typeof(NodeClass));
                    case Attributes.ValueRank:
                        var int32Value = decoder.ReadInt32(field);
                        if (int32Value != 0) {
                            return int32Value;
                        }
                        break;
                    case Attributes.BrowseName:
                        var qualifiedName = decoder.ReadQualifiedName(field);
                        if (qualifiedName != null) {
                            return qualifiedName;
                        }
                        break;
                    case Attributes.Historizing:
                    case Attributes.Executable:
                    case Attributes.UserExecutable:
                    case Attributes.IsAbstract:
                    case Attributes.Symmetric:
                    case Attributes.ContainsNoLoops:
                        var booleanValue = decoder.ReadBoolean(field);
                        if (booleanValue != false) {
                            return booleanValue;
                        }
                        break;
                    case Attributes.EventNotifier:
                    case Attributes.AccessLevel:
                    case Attributes.UserAccessLevel:
                        var byteValue = decoder.ReadByte(field);
                        if (byteValue != 0) {
                            return byteValue;
                        }
                        break;
                    case Attributes.MinimumSamplingInterval:
                        var doubleValue = decoder.ReadDouble(field);
                        if ((ulong)doubleValue != 0) {
                            return doubleValue;
                        }
                        break;
                    case Attributes.ArrayDimensions:
                        var uint32array = decoder.ReadUInt32Array(field);
                        if (uint32array != null && uint32array.Count > 0) {
                            return uint32array;
                        }
                        break;
                    case Attributes.AccessRestrictions:
                        var uint16Value = decoder.ReadUInt16(field);
                        if (uint16Value != 0) {
                            return uint16Value;
                        }
                        break;
                    case Attributes.RolePermissions:
                    case Attributes.UserRolePermissions:
                        var encodableArray = decoder.ReadEncodeableArray(field,
                            typeof(RolePermissionType));
                        if (encodableArray != null && encodableArray.Length > 0) {
                            return encodableArray;
                        }
                        break;
                    case Attributes.DataTypeDefinition:
                        var extensionObject = decoder.ReadExtensionObject(field);
                        if (extensionObject != null) {
                            return extensionObject;
                        }
                        break;
                    case Attributes.Value:
                    default:
                        var variant = decoder.ReadVariant(field);
                        if (variant != Variant.Null) {
                            return new DataValue(variant);
                        }
                        break;
                }
                return null;
            }
            finally {
                decoder.PopNamespace();
            }
        }

        /// <summary>
        /// Encode value for attribute
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        public void Encode(IEncoder encoder, uint attribute, object value) {
            encoder.PushNamespace(Namespaces.OpcUa);
            try {
                var field = Attributes.GetBrowseName(attribute);
                switch (attribute) {
                    case Attributes.DisplayName:
                    case Attributes.InverseName:
                    case Attributes.Description:
                        encoder.WriteLocalizedText(field, (LocalizedText)value);
                        break;
                    case Attributes.WriteMask:
                    case Attributes.UserWriteMask:
                    case Attributes.AccessLevelEx:
                        encoder.WriteUInt32(field, (uint)value);
                        break;
                    case Attributes.NodeId:
                    case Attributes.DataType:
                        encoder.WriteNodeId(field, (NodeId)value);
                        break;
                    case Attributes.NodeClass:
                        encoder.WriteEnumerated(field, (NodeClass)value);
                        break;
                    case Attributes.ValueRank:
                        encoder.WriteInt32(field, (int)value);
                        break;
                    case Attributes.BrowseName:
                        encoder.WriteQualifiedName(field, (QualifiedName)value);
                        break;
                    case Attributes.Historizing:
                    case Attributes.Executable:
                    case Attributes.UserExecutable:
                    case Attributes.IsAbstract:
                    case Attributes.Symmetric:
                    case Attributes.ContainsNoLoops:
                        encoder.WriteBoolean(field, (bool)value);
                        break;
                    case Attributes.EventNotifier:
                    case Attributes.AccessLevel:
                    case Attributes.UserAccessLevel:
                        encoder.WriteByte(field, (byte)value);
                        break;
                    case Attributes.MinimumSamplingInterval:
                        encoder.WriteDouble(field, (double)value);
                        break;
                    case Attributes.ArrayDimensions:
                        encoder.WriteUInt32Array(field, (uint[])value);
                        break;
                    case Attributes.AccessRestrictions:
                        encoder.WriteUInt16(field, (ushort)value);
                        break;
                    case Attributes.RolePermissions:
                    case Attributes.UserRolePermissions:
                        // Always optional
                        encoder.WriteEncodeableArray(field, (IEncodeable[])value,
                            typeof(RolePermissionType));
                        break;
                    case Attributes.DataTypeDefinition:
                        // Always optional
                        encoder.WriteExtensionObject(field, (ExtensionObject)value);
                        break;
                    case Attributes.Value:
                        if (value is Variant v) {
                            encoder.WriteVariant(field, v);
                        }
                        else {
                            encoder.WriteVariant(field, new Variant(value));
                        }
                        break;
                    default:
                        throw new ArgumentException(nameof(attribute));
                }
            }
            finally {
                encoder.PopNamespace();
            }
        }

        /// <summary>
        /// Initialize attribute map
        /// See Part 3 Table 20 – Overview of Attributes
        /// </summary>
        public AttributeMap() {

            _map[Variable, Attributes.AccessLevel] =
                new MapEntry((byte)1);
            _map[Variable, Attributes.ArrayDimensions] =
                new MapEntry(new uint[0], true);
            _map[Variable, Attributes.BrowseName] = new
                MapEntry(QualifiedName.Null);
            _map[Variable, Attributes.DataType] = new
                MapEntry(Opc.Ua.NodeId.Null);
            _map[Variable, Attributes.Description] = new
                MapEntry(LocalizedText.Null, true);
            _map[Variable, Attributes.DisplayName] = new
                MapEntry(LocalizedText.Null);
            _map[Variable, Attributes.Historizing] = new
                MapEntry(false);
            _map[Variable, Attributes.MinimumSamplingInterval] =
                new MapEntry((double)-1, true);
            _map[Variable, Attributes.NodeClass] =
                new MapEntry(NodeClass.Variable);
            _map[Variable, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            _map[Variable, Attributes.UserAccessLevel] =
                new MapEntry((byte)1, true);
            _map[Variable, Attributes.AccessLevelEx] =
                new MapEntry((uint)0, true);
            _map[Variable, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            _map[Variable, Attributes.RolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[Variable, Attributes.UserRolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[Variable, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[Variable, Attributes.Value] = new
                MapEntry(Variant.Null);
            _map[Variable, Attributes.ValueRank] =
                new MapEntry(ValueRanks.Scalar);
            _map[Variable, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            _map[VariableType, Attributes.ArrayDimensions] =
                new MapEntry(new uint[0], true);
            _map[VariableType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            _map[VariableType, Attributes.DataType] =
                new MapEntry(Opc.Ua.NodeId.Null);
            _map[VariableType, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            _map[VariableType, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            _map[VariableType, Attributes.IsAbstract] =
                new MapEntry(true);
            _map[VariableType, Attributes.NodeClass] =
                new MapEntry(NodeClass.VariableType);
            _map[VariableType, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            _map[VariableType, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            _map[VariableType, Attributes.RolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[VariableType, Attributes.UserRolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[VariableType, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[VariableType, Attributes.Value] =
                new MapEntry(Variant.Null, true);
            _map[VariableType, Attributes.ValueRank] =
                new MapEntry(ValueRanks.Scalar);
            _map[VariableType, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            _map[Object, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            _map[Object, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            _map[Object, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            _map[Object, Attributes.EventNotifier] =
                new MapEntry((byte)0);
            _map[Object, Attributes.NodeClass] =
                new MapEntry(NodeClass.Object);
            _map[Object, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            _map[Object, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            _map[Object, Attributes.RolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[Object, Attributes.UserRolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[Object, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[Object, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            _map[ObjectType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            _map[ObjectType, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            _map[ObjectType, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            _map[ObjectType, Attributes.IsAbstract] =
                new MapEntry(true);
            _map[ObjectType, Attributes.NodeClass] =
                new MapEntry(NodeClass.ObjectType);
            _map[ObjectType, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            _map[ObjectType, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            _map[ObjectType, Attributes.RolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[ObjectType, Attributes.UserRolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[ObjectType, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[ObjectType, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            _map[ReferenceType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            _map[ReferenceType, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            _map[ReferenceType, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            _map[ReferenceType, Attributes.InverseName] =
                new MapEntry(LocalizedText.Null, true);
            _map[ReferenceType, Attributes.IsAbstract] =
                new MapEntry(true);
            _map[ReferenceType, Attributes.NodeClass] =
                new MapEntry(NodeClass.ReferenceType);
            _map[ReferenceType, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            _map[ReferenceType, Attributes.Symmetric] =
                new MapEntry(true);
            _map[ReferenceType, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            _map[ReferenceType, Attributes.RolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[ReferenceType, Attributes.UserRolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[ReferenceType, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[ReferenceType, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            _map[DataType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            _map[DataType, Attributes.DataTypeDefinition] =
                new MapEntry(new DataTypeDefinition(), true);
            _map[DataType, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            _map[DataType, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            _map[DataType, Attributes.IsAbstract] =
                new MapEntry(true);
            _map[DataType, Attributes.NodeClass] =
                new MapEntry(NodeClass.DataType);
            _map[DataType, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            _map[DataType, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            _map[DataType, Attributes.RolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[DataType, Attributes.UserRolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[DataType, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[DataType, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            _map[Method, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            _map[Method, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            _map[Method, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            _map[Method, Attributes.Executable] =
                new MapEntry(false);
            _map[Method, Attributes.NodeClass] =
                new MapEntry(NodeClass.Method);
            _map[Method, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            _map[Method, Attributes.UserExecutable] =
                new MapEntry(false);
            _map[Method, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            _map[Method, Attributes.RolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[Method, Attributes.UserRolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[Method, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[Method, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            _map[View, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            _map[View, Attributes.ContainsNoLoops] =
                new MapEntry(true);
            _map[View, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            _map[View, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            _map[View, Attributes.EventNotifier] =
                new MapEntry((byte)0);
            _map[View, Attributes.NodeClass] =
                new MapEntry(NodeClass.View);
            _map[View, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            _map[View, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            _map[View, Attributes.RolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[View, Attributes.UserRolePermissions] =
                new MapEntry(new RolePermissionType[0], true);
            _map[View, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[View, Attributes.WriteMask] =
                new MapEntry((uint)0, true);
        }

        /// <summary>
        /// Convert nodeclass to index
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <returns></returns>
        private int NodeClassId(NodeClass nodeClass) {
            switch (nodeClass) {
                case NodeClass.Object:
                    return Object;
                case NodeClass.Variable:
                    return Variable;
                case NodeClass.Method:
                    return Method;
                case NodeClass.ObjectType:
                    return ObjectType;
                case NodeClass.VariableType:
                    return VariableType;
                case NodeClass.ReferenceType:
                    return ReferenceType;
                case NodeClass.DataType:
                    return DataType;
                case NodeClass.View:
                    return View;
            }
            throw new ServiceResultException(StatusCodes.BadNodeClassInvalid);
        }

        class MapEntry {

            /// <summary>
            /// Attribute map entry
            /// </summary>
            /// <param name="value"></param>
            /// <param name="optional"></param>
            public MapEntry(object value, bool optional = false) {
                Value = value;
                Optional = optional;
            }

            /// <summary>
            /// Default value
            /// </summary>
            public object Value { get; }

            /// <summary>
            /// Whether the attribute is optional
            /// </summary>
            public bool Optional { get; }
        }

        private MapEntry[,] _map = new MapEntry[8, 32];
    }
}
