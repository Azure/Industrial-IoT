// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Represents validity and default value map for attributes
    /// </summary>
    public static class AttributeMap
    {
        internal const int Object = 0;
        internal const int Variable = 1;
        internal const int Method = 2;
        internal const int ObjectType = 3;
        internal const int VariableType = 4;
        internal const int ReferenceType = 5;
        internal const int DataType = 6;
        internal const int View = 7;

        /// <summary>
        /// Get built in type of attribute
        /// </summary>
        /// <param name="attributeId"></param>
        /// <returns></returns>
        public static BuiltInType GetBuiltInType(uint attributeId)
        {
            switch (attributeId)
            {
                case Attributes.Value:
                    return BuiltInType.Variant;
                case Attributes.DisplayName:
                case Attributes.Description:
                    return BuiltInType.LocalizedText;
                case Attributes.WriteMask:
                case Attributes.UserWriteMask:
                    return BuiltInType.UInt32;
                case Attributes.NodeId:
                    return BuiltInType.NodeId;
                case Attributes.NodeClass:
                    return BuiltInType.Int32;
                case Attributes.BrowseName:
                    return BuiltInType.QualifiedName;
                case Attributes.IsAbstract:
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
                case Attributes.UserAccessLevel:
                    return BuiltInType.Byte;
                case Attributes.MinimumSamplingInterval:
                    return BuiltInType.Double;
                case Attributes.Historizing:
                case Attributes.Executable:
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
                case Attributes.UserRolePermissions:
                    return BuiltInType.ExtensionObject;
                default:
                    System.Diagnostics.Debug.Fail("Unknown attribute");
                    return BuiltInType.Null;
            }
        }

        /// <summary>
        /// Get browse name of attribute - speedier than in stack which uses
        /// reflection.
        /// </summary>
        /// <param name="attributeId"></param>
        /// <returns></returns>
        public static string GetBrowseName(uint attributeId)
        {
            if (TypeMaps.Attributes.Value.TryGetBrowseName(attributeId,
                out var value))
            {
                return value;
            }
            System.Diagnostics.Debug.Fail("Unknown attribute");
            return attributeId.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get all valid attributes for the node class
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <returns></returns>
        public static IEnumerable<uint> GetNodeClassAttributes(NodeClass nodeClass)
        {
            for (uint i = 0; i < 32; i++)
            {
                if (kMap[NodeClassId(nodeClass), i] != null)
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Returns default value
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <param name="attributeId"></param>
        /// <param name="returnNullIfOptional"></param>
        /// <returns></returns>
        public static object? GetDefaultValue(NodeClass nodeClass,
            uint attributeId, bool returnNullIfOptional)
        {
            if (attributeId > 32)
            {
                return null;
            }
            var entry = kMap[NodeClassId(nodeClass), attributeId];
            if (entry != null)
            {
                if (!entry.Optional || !returnNullIfOptional)
                {
                    return entry.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Initialize attribute map
        /// See Part 3 Table 20 – Overview of Attributes
        /// </summary>
        static AttributeMap()
        {
            kMap[Variable, Attributes.AccessLevel] =
                new MapEntry((byte)1);
            kMap[Variable, Attributes.ArrayDimensions] =
                new MapEntry(Array.Empty<uint>(), true);
            kMap[Variable, Attributes.BrowseName] = new
                MapEntry(QualifiedName.Null);
            kMap[Variable, Attributes.DataType] = new
                MapEntry(NodeId.Null);
            kMap[Variable, Attributes.Description] = new
                MapEntry(LocalizedText.Null, true);
            kMap[Variable, Attributes.DisplayName] = new
                MapEntry(LocalizedText.Null);
            kMap[Variable, Attributes.Historizing] = new
                MapEntry(false);
            kMap[Variable, Attributes.MinimumSamplingInterval] =
                new MapEntry((double)-1, true);
            kMap[Variable, Attributes.NodeClass] =
                new MapEntry(NodeClass.Variable);
            kMap[Variable, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            kMap[Variable, Attributes.UserAccessLevel] =
                new MapEntry((byte)1, true);
            kMap[Variable, Attributes.AccessLevelEx] =
                new MapEntry((uint)0, true);
            kMap[Variable, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            kMap[Variable, Attributes.RolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[Variable, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[Variable, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            kMap[Variable, Attributes.Value] = new
                MapEntry(Variant.Null);
            kMap[Variable, Attributes.ValueRank] =
                new MapEntry(ValueRanks.Scalar);
            kMap[Variable, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            kMap[VariableType, Attributes.ArrayDimensions] =
                new MapEntry(Array.Empty<uint>(), true);
            kMap[VariableType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            kMap[VariableType, Attributes.DataType] =
                new MapEntry(NodeId.Null);
            kMap[VariableType, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            kMap[VariableType, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            kMap[VariableType, Attributes.IsAbstract] =
                new MapEntry(true);
            kMap[VariableType, Attributes.NodeClass] =
                new MapEntry(NodeClass.VariableType);
            kMap[VariableType, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            kMap[VariableType, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            kMap[VariableType, Attributes.RolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[VariableType, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[VariableType, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            kMap[VariableType, Attributes.Value] =
                new MapEntry(Variant.Null, true);
            kMap[VariableType, Attributes.ValueRank] =
                new MapEntry(ValueRanks.Scalar);
            kMap[VariableType, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            kMap[Object, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            kMap[Object, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            kMap[Object, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            kMap[Object, Attributes.EventNotifier] =
                new MapEntry((byte)0);
            kMap[Object, Attributes.NodeClass] =
                new MapEntry(NodeClass.Object);
            kMap[Object, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            kMap[Object, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            kMap[Object, Attributes.RolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[Object, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[Object, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            kMap[Object, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            kMap[ObjectType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            kMap[ObjectType, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            kMap[ObjectType, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            kMap[ObjectType, Attributes.IsAbstract] =
                new MapEntry(true);
            kMap[ObjectType, Attributes.NodeClass] =
                new MapEntry(NodeClass.ObjectType);
            kMap[ObjectType, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            kMap[ObjectType, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            kMap[ObjectType, Attributes.RolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[ObjectType, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[ObjectType, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            kMap[ObjectType, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            kMap[ReferenceType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            kMap[ReferenceType, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            kMap[ReferenceType, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            kMap[ReferenceType, Attributes.InverseName] =
                new MapEntry(LocalizedText.Null, true);
            kMap[ReferenceType, Attributes.IsAbstract] =
                new MapEntry(true);
            kMap[ReferenceType, Attributes.NodeClass] =
                new MapEntry(NodeClass.ReferenceType);
            kMap[ReferenceType, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            kMap[ReferenceType, Attributes.Symmetric] =
                new MapEntry(true);
            kMap[ReferenceType, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            kMap[ReferenceType, Attributes.RolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[ReferenceType, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[ReferenceType, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            kMap[ReferenceType, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            kMap[DataType, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            kMap[DataType, Attributes.DataTypeDefinition] =
                new MapEntry(new ExtensionObject(), true);
            kMap[DataType, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            kMap[DataType, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            kMap[DataType, Attributes.IsAbstract] =
                new MapEntry(true);
            kMap[DataType, Attributes.NodeClass] =
                new MapEntry(NodeClass.DataType);
            kMap[DataType, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            kMap[DataType, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            kMap[DataType, Attributes.RolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[DataType, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[DataType, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            kMap[DataType, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            kMap[Method, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            kMap[Method, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            kMap[Method, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            kMap[Method, Attributes.Executable] =
                new MapEntry(false);
            kMap[Method, Attributes.NodeClass] =
                new MapEntry(NodeClass.Method);
            kMap[Method, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            kMap[Method, Attributes.UserExecutable] =
                new MapEntry(false);
            kMap[Method, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            kMap[Method, Attributes.RolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[Method, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[Method, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            kMap[Method, Attributes.WriteMask] =
                new MapEntry((uint)0, true);

            kMap[View, Attributes.BrowseName] =
                new MapEntry(QualifiedName.Null);
            kMap[View, Attributes.ContainsNoLoops] =
                new MapEntry(true);
            kMap[View, Attributes.Description] =
                new MapEntry(LocalizedText.Null, true);
            kMap[View, Attributes.DisplayName] =
                new MapEntry(LocalizedText.Null);
            kMap[View, Attributes.EventNotifier] =
                new MapEntry((byte)0);
            kMap[View, Attributes.NodeClass] =
                new MapEntry(NodeClass.View);
            kMap[View, Attributes.NodeId] =
                new MapEntry(NodeId.Null);
            kMap[View, Attributes.AccessRestrictions] =
                new MapEntry((ushort)0, true);
            kMap[View, Attributes.RolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[View, Attributes.UserRolePermissions] =
                new MapEntry(Array.Empty<ExtensionObject>(), true);
            kMap[View, Attributes.UserWriteMask] =
                new MapEntry((uint)0, true);
            kMap[View, Attributes.WriteMask] =
                new MapEntry((uint)0, true);
        }

        /// <summary>
        /// Convert nodeclass to index
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private static int NodeClassId(NodeClass nodeClass)
        {
            switch (nodeClass)
            {
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

        private class MapEntry
        {
            /// <summary>
            /// Attribute map entry
            /// </summary>
            /// <param name="value"></param>
            /// <param name="optional"></param>
            public MapEntry(object value, bool optional = false)
            {
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

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
        private static readonly MapEntry[,] kMap = new MapEntry[8, 32];
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
    }
}
