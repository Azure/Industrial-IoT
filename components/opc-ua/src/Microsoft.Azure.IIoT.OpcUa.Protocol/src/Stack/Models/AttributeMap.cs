// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents validity and default value map for attributes
    /// </summary>
    public class AttributeMap {
        private const int Object = 0;
        private const int Variable = 1;
        private const int Method = 2;
        private const int ObjectType = 3;
        private const int VariableType = 4;
        private const int ReferenceType = 5;
        private const int DataType = 6;
        private const int View = 7;

        /// <summary>
        /// Get built in type of attribute
        /// </summary>
        /// <param name="attributeId"></param>
        /// <returns></returns>
        public static BuiltInType GetBuiltInType(uint attributeId) {
            switch (attributeId) {
                case Attributes.Value:
                    return BuiltInType.Variant;
                case Attributes.DisplayName:
                    return BuiltInType.LocalizedText;
                case Attributes.Description:
                    return BuiltInType.LocalizedText;
                case Attributes.WriteMask:
                    return BuiltInType.UInt32;
                case Attributes.UserWriteMask:
                    return BuiltInType.UInt32;
                case Attributes.NodeId:
                    return BuiltInType.NodeId;
                case Attributes.NodeClass:
                    return BuiltInType.Int32;
                case Attributes.BrowseName:
                    return BuiltInType.QualifiedName;
                case Attributes.IsAbstract:
                    return BuiltInType.Boolean;
                case Attributes.Symmetric:
                    return BuiltInType.Boolean;
                case Attributes.InverseName:
                    return BuiltInType.LocalizedText;
                case Attributes.ContainsNoLoops:
                    return BuiltInType.Boolean;
                case Attributes.EventNotifier:
                    return BuiltInType.Byte;
                case Attributes.DataType:
                    return BuiltInType.NodeId;
                case Attributes.ValueRank:
                    return BuiltInType.Int32;
                case Attributes.AccessLevel:
                    return BuiltInType.Byte;
                case Attributes.UserAccessLevel:
                    return BuiltInType.Byte;
                case Attributes.MinimumSamplingInterval:
                    return BuiltInType.Double;
                case Attributes.Historizing:
                    return BuiltInType.Boolean;
                case Attributes.Executable:
                    return BuiltInType.Boolean;
                case Attributes.UserExecutable:
                    return BuiltInType.Boolean;
                case Attributes.ArrayDimensions:
                    return BuiltInType.UInt32;
                case Attributes.DataTypeDefinition:
                    return BuiltInType.ExtensionObject;
                case Attributes.AccessLevelEx:
                    return BuiltInType.UInt32;
                case Attributes.AccessRestrictions:
                    return BuiltInType.UInt16;
                case Attributes.RolePermissions:
                    return BuiltInType.ExtensionObject;
                case Attributes.UserRolePermissions:
                    return BuiltInType.ExtensionObject;
                default:
                    System.Diagnostics.Debug.Assert(false, "Unknown attribute");
                    return BuiltInType.Null;
            }
        }

        /// <summary>
        /// Returns the data type id for the attribute.
        /// </summary>
        /// <param name="attributeId"></param>
        /// <returns></returns>
        public static NodeId GetDataTypeId(uint attributeId) {
            switch (attributeId) {
                case Attributes.Value:
                    return DataTypes.BaseDataType;
                case Attributes.DisplayName:
                    return DataTypes.LocalizedText;
                case Attributes.Description:
                    return DataTypes.LocalizedText;
                case Attributes.WriteMask:
                    return DataTypes.UInt32;
                case Attributes.UserWriteMask:
                    return DataTypes.UInt32;
                case Attributes.NodeId:
                    return DataTypes.NodeId;
                case Attributes.NodeClass:
                    return DataTypes.Enumeration;
                case Attributes.BrowseName:
                    return DataTypes.QualifiedName;
                case Attributes.IsAbstract:
                    return DataTypes.Boolean;
                case Attributes.Symmetric:
                    return DataTypes.Boolean;
                case Attributes.InverseName:
                    return DataTypes.LocalizedText;
                case Attributes.ContainsNoLoops:
                    return DataTypes.Boolean;
                case Attributes.EventNotifier:
                    return DataTypes.Byte;
                case Attributes.DataType:
                    return DataTypes.NodeId;
                case Attributes.ValueRank:
                    return DataTypes.Int32;
                case Attributes.AccessLevel:
                    return DataTypes.Byte;
                case Attributes.UserAccessLevel:
                    return DataTypes.Byte;
                case Attributes.MinimumSamplingInterval:
                    return DataTypes.Duration;
                case Attributes.Historizing:
                    return DataTypes.Boolean;
                case Attributes.Executable:
                    return DataTypes.Boolean;
                case Attributes.UserExecutable:
                    return DataTypes.Boolean;
                case Attributes.ArrayDimensions:
                    return DataTypes.UInt32;
                case Attributes.DataTypeDefinition:
                    return DataTypes.StructureDefinition;
                case Attributes.AccessLevelEx:
                    return DataTypes.UInt32;
                case Attributes.AccessRestrictions:
                    return DataTypes.UInt16;
                case Attributes.RolePermissions:
                    return DataTypes.RolePermissionType;
                case Attributes.UserRolePermissions:
                    return DataTypes.RolePermissionType;
                default:
                    System.Diagnostics.Debug.Assert(false, "Unknown attribute");
                    return null;
            }
        }

        /// <summary>
        /// Get browse name of attribute - speedier than in stack which uses
        /// reflection.
        /// </summary>
        /// <param name="attributeId"></param>
        /// <returns></returns>
        public static string GetBrowseName(uint attributeId) {
            if (TypeMaps.Attributes.Value.TryGetBrowseName(attributeId,
                out var value)) {
                return value;
            }
            System.Diagnostics.Debug.Assert(false, "Unknown attribute");
            return attributeId.ToString();
        }

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
                var field = GetBrowseName(attributeId);
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
                        return decoder.ReadEnumerated<NodeClass>(field);
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
                        var extensionObjects = decoder.ReadExtensionObjectArray(field);
                        if (extensionObjects != null && extensionObjects.Count > 0) {
                            return extensionObjects;
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
                var field = GetBrowseName(attribute);
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
                        encoder.WriteExtensionObjectArray(field, (ExtensionObject[])value);
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
                            encoder.WriteVariant(field,
                                value == null ? Variant.Null : new Variant(value));
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
                new MapEntry(Array.Empty<uint>(), true);
            _map[Variable, Attributes.BrowseName] = new
                MapEntry(QualifiedName.Null);
            _map[Variable, Attributes.DataType] = new
                MapEntry(NodeId.Null);
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
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[Variable, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[Variable, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[Variable, Attributes.Value] = new
                MapEntry(Variant.Null);
            _map[Variable, Attributes.ValueRank] =
                new MapEntry(ValueRanks.Scalar);
            _map[Variable, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            _map[VariableType, Attributes.ArrayDimensions] =
                new MapEntry(Array.Empty<uint>(), true);
            _map[VariableType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            _map[VariableType, Attributes.DataType] =
                new MapEntry(NodeId.Null);
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
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[VariableType, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
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
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[Object, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
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
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[ObjectType, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
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
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[ReferenceType, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[ReferenceType, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            _map[ReferenceType, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            _map[DataType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            _map[DataType, Attributes.DataTypeDefinition] =
                new MapEntry(new ExtensionObject(), true);
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
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[DataType, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
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
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[Method, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
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
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            _map[View, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
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

        private class MapEntry {

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

        private readonly MapEntry[,] _map = new MapEntry[8, 32];
    }
}
