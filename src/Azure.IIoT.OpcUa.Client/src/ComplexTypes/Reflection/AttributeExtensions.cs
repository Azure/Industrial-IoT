/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client.ComplexTypes.Reflection
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;

    /// <summary>
    /// Extensions to build attributes for the complex type builder.
    /// </summary>
    public static class AttributeExtensions
    {
        /// <summary>
        /// Get the return type of an item in a collection.
        /// </summary>
        /// <param name="collectionType"></param>
        public static Type? GetItemType(this Type collectionType)
        {
            return collectionType.GetMethod("get_Item")?.ReturnType;
        }

        /// <summary>
        /// Build the DataContract attribute for a complex type.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="Namespace"></param>
        public static void DataContractAttribute(this TypeBuilder builder,
            string Namespace)
        {
            var attribute = DataContractAttributeBuilder(Namespace);
            builder.SetCustomAttribute(attribute);
        }

        /// <summary>
        /// Build the DataContract attribute for an enumeration type.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="Namespace"></param>
        public static void DataContractAttribute(this EnumBuilder builder,
            string Namespace)
        {
            var attribute = DataContractAttributeBuilder(Namespace);
            builder.SetCustomAttribute(attribute);
        }

        /// <summary>
        /// Build the DataMember attribute for a complex type.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="name"></param>
        /// <param name="isRequired"></param>
        /// <param name="order"></param>
        public static void DataMemberAttribute(this PropertyBuilder typeBuilder,
            string name, bool isRequired, int order)
        {
            var attribute = DataMemberAttributeBuilder(name, isRequired, order);
            typeBuilder.SetCustomAttribute(attribute);
        }

        /// <summary>
        /// Build the StructureDefinition attribute for a complex type.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="structureDefinition"></param>
        public static void StructureDefinitionAttribute(this TypeBuilder typeBuilder,
            StructureDefinition structureDefinition)
        {
            var attributeType = typeof(StructureDefinitionAttribute);
            var baseDataType = ComplexTypes.Reflection.StructureDefinitionAttribute.FromBaseType(
                structureDefinition.BaseDataType);
            var ctorInfo = attributeType.GetConstructor(Type.EmptyTypes)!;
            var builder = new CustomAttributeBuilder(
                ctorInfo,
                Array.Empty<object>(),  // constructor arguments
                new []           // properties to assign
                {
                    attributeType.GetProperty("DefaultEncodingId")!,
                    attributeType.GetProperty("BaseDataType")!,
                    attributeType.GetProperty("StructureType")!
                },
                new object?[]    // values to assign
                {
                    structureDefinition.DefaultEncodingId?.ToString(),
                    baseDataType,
                    structureDefinition.StructureType
                });
            typeBuilder.SetCustomAttribute(builder);
        }

        /// <summary>
        /// Build the StructureTypeId attribute for a complex type.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="complexTypeId"></param>
        /// <param name="binaryEncodingId"></param>
        /// <param name="xmlEncodingId"></param>
        public static void StructureTypeIdAttribute(this TypeBuilder typeBuilder,
            ExpandedNodeId complexTypeId, ExpandedNodeId binaryEncodingId,
            ExpandedNodeId xmlEncodingId)
        {
            var attributeType = typeof(StructureTypeIdAttribute);
            var ctorInfo = attributeType.GetConstructor(Type.EmptyTypes)!;
            var builder = new CustomAttributeBuilder(
                ctorInfo,
                Array.Empty<object>(),  // constructor arguments
                new[]           // properties to assign
                {
                    attributeType.GetProperty("ComplexTypeId")!,
                    attributeType.GetProperty("BinaryEncodingId")!,
                    attributeType.GetProperty("XmlEncodingId")!
                },
                new object?[]    // values to assign
                {
                    complexTypeId?.ToString(),
                    binaryEncodingId?.ToString(),
                    xmlEncodingId?.ToString()
                });
            typeBuilder.SetCustomAttribute(builder);
        }

        /// <summary>
        /// Build the StructureField attribute for a complex type.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="structureField"></param>
        public static void StructureFieldAttribute(this PropertyBuilder typeBuilder,
            StructureField structureField)
        {
            var attributeType = typeof(StructureFieldAttribute);
            var ctorInfo = attributeType.GetConstructor(Type.EmptyTypes)!;
            var pi = new List<PropertyInfo>()
            {
                attributeType.GetProperty("ValueRank")!,
                attributeType.GetProperty("MaxStringLength")!,
                attributeType.GetProperty("IsOptional")!
            };

            var pv = new List<object>() {
                structureField.ValueRank,
                structureField.MaxStringLength,
                structureField.IsOptional
            };

            // only unambiguous built in types get the info,
            // IEncodeable types are handled by type property as BuiltInType.Null
            var builtInType = (int)GetBuiltInType(structureField.DataType);
            if (builtInType > (int)BuiltInType.Null)
            {
                pi.Add(attributeType.GetProperty("BuiltInType")!);
                pv.Add(builtInType);
            }

            var builder = new CustomAttributeBuilder(
                ctorInfo,
                Array.Empty<object>(),  // constructor arguments
                pi.ToArray(),           // properties to assign
                pv.ToArray()            // values to assign
            );
            typeBuilder.SetCustomAttribute(builder);
        }

        /// <summary>
        /// Build the EnumMember attribute for an enumeration type.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public static void EnumMemberAttribute(this FieldBuilder typeBuilder,
            string Name, int Value)
        {
            var attributeType = typeof(EnumMemberAttribute);
            var ctorParams = new Type[] { typeof(string) };
            var ctorInfo = attributeType.GetConstructor(Type.EmptyTypes)!;
            var builder = new CustomAttributeBuilder(
                ctorInfo,
                Array.Empty<object>(),  // constructor arguments
                new[]           // properties to assign
                {
                    attributeType.GetProperty("Value")!
                },
                new object[]    // values to assign
                {
                    Name+"_"+Value.ToString(CultureInfo.InvariantCulture)
                });
            typeBuilder.SetCustomAttribute(builder);
        }

        /// <summary>
        /// Build the DataMember attribute.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isRequired"></param>
        /// <param name="order"></param>
        private static CustomAttributeBuilder DataMemberAttributeBuilder(string name,
            bool isRequired, int order)
        {
            var attributeType = typeof(DataMemberAttribute);
            var ctorInfo = attributeType.GetConstructor(Type.EmptyTypes)!;
            return new CustomAttributeBuilder(
                ctorInfo,
                Array.Empty<object>(),  // constructor arguments
                new[]           // properties to assign
                {
                    attributeType.GetProperty("Name")!,
                    attributeType.GetProperty("IsRequired")!,
                    attributeType.GetProperty("Order")!
                },
                new object[]    // values to assign
                {
                    name,
                    isRequired,
                    order
                });
        }

        /// <summary>
        /// Build the DataContract attribute.
        /// </summary>
        /// <param name="Namespace"></param>
        private static CustomAttributeBuilder DataContractAttributeBuilder(
            string Namespace)
        {
            var attributeType = typeof(DataContractAttribute);
            var ctorInfo = attributeType.GetConstructor(Type.EmptyTypes)!;
            return new CustomAttributeBuilder(
                ctorInfo,
                Array.Empty<object>(),  // constructor arguments
                new[]           // properties to assign
                {
                    attributeType.GetProperty("Namespace")!
                },
                new object[]    // values to assign
                {
                    Namespace
                });
        }

        /// <summary>
        /// Convert a DataTypeId to a BuiltInType that can be used
        /// for the switch table in <see cref="BaseComplexType"/>.
        /// </summary>
        /// <remarks>
        /// As a prerequisite the complex type resolver found a
        /// valid .NET supertype that can be mapped to a BuiltInType.
        /// IEncodeable types are mapped to BuiltInType.Null.
        /// </remarks>
        /// <param name="datatypeId">The data type identifier.</param>
        /// <returns>An <see cref="BuiltInType"/> for
        /// <paramref name="datatypeId"/></returns>
        private static BuiltInType GetBuiltInType(NodeId datatypeId)
        {
            if (datatypeId.IsNullNodeId || datatypeId.NamespaceIndex != 0 ||
                datatypeId.IdType != Opc.Ua.IdType.Numeric)
            {
                return BuiltInType.Null;
            }

            var builtInType = (BuiltInType)Enum.ToObject(typeof(BuiltInType),
                datatypeId.Identifier);

            if (builtInType <= BuiltInType.DiagnosticInfo ||
                builtInType == BuiltInType.Enumeration)
            {
                return builtInType;
            }

            // The special case is the internal treatment of Number, Integer and
            // UInteger types which are mapped to Variant, but they have an internal
            // representation in the BuiltInType enum, hence it needs the special
            // handling here to return the BuiltInType.Variant.
            // Other DataTypes which map directly to .NET types in
            // <see cref="TypeInfo.GetSystemType(BuiltInType, int)"/>
            // are handled in <see cref="TypeInfo.GetBuiltInType()"/>
            switch ((uint)builtInType)
            {
                // supertypes of numbers
                case DataTypes.Integer:
                case DataTypes.UInteger:
                case DataTypes.Number:
                case DataTypes.Decimal:
                    return BuiltInType.Variant;
                default:
                    return Ua.TypeInfo.GetBuiltInType(datatypeId);
            }
        }
    }
}
