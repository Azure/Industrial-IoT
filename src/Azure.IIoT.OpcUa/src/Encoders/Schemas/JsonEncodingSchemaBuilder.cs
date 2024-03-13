// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Avro;
    using Avro.Util;
    using Opc.Ua;
    using DataSetFieldContentMask = Publisher.Models.DataSetFieldContentMask;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides the json encodings of built in types and objects in Avro schema
    /// </summary>
    internal class JsonEncodingSchemaBuilder : EncodingSchemaBuilder
    {
        private Schema EnumerationSchema
        {
            get
            {
                if (_reversibleEncoding)
                {
                    // Enumeration values shall be encoded as a JSON number
                    // for the reversible encoding.
                    return PrimitiveType((int)BuiltInType.Enumeration,
                        nameof(BuiltInType.Enumeration), "int");
                }

                // For the non - reversible form, Enumeration values are
                // encoded as a JSON string with the following format:
                // <name>_<value>
                return PrimitiveType((int)BuiltInType.Enumeration,
                    nameof(BuiltInType.Enumeration), "string");
            }
        }

        private Schema DiagnosticInfoSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.DiagnosticInfo),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.Int32), "SymbolicId", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.Int32), "NamespaceUri", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.Int32), "Locale", 2),
                        new (GetSchemaForBuiltInType(BuiltInType.Int32), "LocalizedText", 3),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "AdditionalInfo", 4),
                        new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "InnerStatusCode", 5),
                        new (GetSchemaForBuiltInType(BuiltInType.DiagnosticInfo, true), "InnerDiagnosticInfo", 6)
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DiagnosticInfo) });
            }
        }

        private Schema VariantSchema
        {
            get
            {
                if (!_reversibleEncoding)
                {
                    // For the non-reversible form, Variant values shall be
                    // encoded as a JSON value containing only the value of
                    // the Body field. The Type and Dimensions fields are
                    // dropped. Multi-dimensional arrays are encoded as a
                    // multi-dimensional JSON array as described in 5.4.5.

                    // TODO
                }

                var types = GetPossibleTypes(false)
                    .Concat(GetPossibleTypes(true))
                    .ToList();
                var bodyType = AvroUtils.CreateUnion(types);
                return RecordSchema.Create(nameof(BuiltInType.Variant),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "Type", 0),
                        new (bodyType, "Body", 1),
                        new (ArraySchema.Create(GetSchemaForBuiltInType(BuiltInType.Int32)),
                            "Dimensions", 2)
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.Variant) });

                IEnumerable<Schema> GetPossibleTypes(bool array)
                {
                    for (var i = 0; i <= 29; i++)
                    {
                        if ((i == (int)BuiltInType.Variant && !array) ||
                            (i == (int)BuiltInType.Null && array))
                        {
                            continue; // TODO: Array of variant is allowed
                        }
                        yield return GetSchemaForBuiltInType((BuiltInType)i,
                            false, array);
                    }
                }
            }
        }

        private Schema ExtensionObjectSchema
        {
            get
            {
                if (!_reversibleEncoding)
                {
                    // For the non-reversible form, ExtensionObject values
                    // shall be encoded as a JSON value containing only the
                    // value of the Body field. The TypeId and Encoding
                    // fields are dropped.

                    // TODO
                }

                var bodyType = AvroUtils.CreateUnion(
                    GetSchemaForBuiltInType(BuiltInType.Null),
                    GetSchemaForBuiltInType(BuiltInType.String),
                    GetSchemaForBuiltInType(BuiltInType.XmlElement),
                    GetSchemaForBuiltInType(BuiltInType.ByteString));
                var encodingType = EnumSchema.Create("Encoding", new string[]
                {
                    "Structure",
                    "ByteString",
                    "XmlElement"
                });
                return RecordSchema.Create(nameof(BuiltInType.ExtensionObject),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.NodeId), "TypeId", 0),
                        new (encodingType, "Encoding", 1),
                        new (bodyType, "Body", 2)
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.ExtensionObject) });
            }
        }

        private Schema StatusCodeSchema
        {
            get
            {
                if (_reversibleEncoding)
                {
                    //
                    // StatusCode values shall be encoded as a JSON number for
                    // the reversible encoding. If the StatusCode is Good (0)
                    // it is only encoded if it is an element of a JSON array.
                    //
#if !DERIVE_PRIMITIVE
                    return PrimitiveType((int)BuiltInType.StatusCode,
                        nameof(BuiltInType.StatusCode), "int");
#else
                    return DerivedSchema.Create(nameof(BuiltInType.StatusCode),
                        GetSchemaForBuiltInType(BuiltInType.UInt32),
                        AvroUtils.NamespaceZeroName,
                            new[] { GetDataTypeId(BuiltInType.StatusCode) });
#endif
                }

                // For the non - reversible form, StatusCode values
                // shall be encoded as a JSON object with the fields
                // defined here.
                return RecordSchema.Create(nameof(BuiltInType.StatusCode),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.UInt32), "Code", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Symbol", 1)
                    }, AvroUtils.NamespaceZeroName);
            }
        }

        private Schema QualifiedNameSchema
        {
            get
            {
                Field field;
                if (_reversibleEncoding)
                {
                    // For reversible encoding this field is a JSON number
                    // with the NamespaceIndex. The field is omitted if the
                    // NamespaceIndex is 0.
                    field = new(GetSchemaForBuiltInType(BuiltInType.UInt32), "Uri", 1);
                }
                else
                {
                    // For non-reversible encoding this field is the JSON
                    // string containing the NamespaceUri associated with
                    // the NamespaceIndex unless the NamespaceIndex is 0.
                    // If the NamespaceIndex is 0 the field is omitted.
                    field = new(GetSchemaForBuiltInType(BuiltInType.String), "Uri", 1);
                }
                return RecordSchema.Create(nameof(BuiltInType.QualifiedName),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Name", 0),
                        field
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.QualifiedName) });
            }
        }

        private Schema LocalizedTextSchema
        {
            get
            {
                if (_reversibleEncoding)
                {
                    return RecordSchema.Create(nameof(BuiltInType.LocalizedText),
                        new List<Field>
                        {
                            new (GetSchemaForBuiltInType(BuiltInType.String), "Locale", 0),
                            new (GetSchemaForBuiltInType(BuiltInType.String), "Text", 1)
                        }, AvroUtils.NamespaceZeroName,
                        new[] { GetDataTypeId(BuiltInType.LocalizedText) });
                }

                // For the non-reversible form, LocalizedText value shall
                // be encoded as a JSON string containing the Text component.
#if !DERIVE_PRIMITIVE
                return PrimitiveType((int)BuiltInType.LocalizedText,
                    nameof(BuiltInType.LocalizedText), "string");
#else
                    return DerivedSchema.Create(nameof(BuiltInType.LocalizedText),
                        GetSchemaForBuiltInType(BuiltInType.String),
                        AvroUtils.NamespaceZeroName,
                            new[] { GetDataTypeId(BuiltInType.LocalizedText) });
#endif
            }
        }

        private Schema NodeIdSchema
        {
            get
            {
                var idType = AvroUtils.CreateUnion(
                    GetSchemaForBuiltInType(BuiltInType.UInt32),
                    GetSchemaForBuiltInType(BuiltInType.String),
                    GetSchemaForBuiltInType(BuiltInType.Guid),
                    GetSchemaForBuiltInType(BuiltInType.ByteString));
                var idTypeType = EnumSchema.Create("IdentifierType", new string[]
                {
                    "UInt32",
                    "String",
                    "Guid",
                    "ByteString"
                });

                Field field;
                if (_reversibleEncoding)
                {
                    // For reversible encoding this field is a JSON number
                    // with the NamespaceIndex. The field is omitted if the
                    // NamespaceIndex is 0.
                    field = new(GetSchemaForBuiltInType(BuiltInType.UInt32), "Namespace", 2);
                }
                else
                {
                    // For non-reversible encoding this field is the JSON
                    // string containing the NamespaceUri associated with
                    // the NamespaceIndex unless the NamespaceIndex is 0.
                    // If the NamespaceIndex is 0 the field is omitted.
                    field = new(GetSchemaForBuiltInType(BuiltInType.String), "Namespace", 2);
                }
                return RecordSchema.Create(nameof(BuiltInType.NodeId),
                    new List<Field>
                    {
                        new (idTypeType, "IdType", 0),
                        new (idType, "Id", 1),
                        field
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.NodeId) });
            }
        }

        private Schema ExpandedNodeIdSchema
        {
            get
            {
                var idType = AvroUtils.CreateUnion(
                    GetSchemaForBuiltInType(BuiltInType.UInt32),
                    GetSchemaForBuiltInType(BuiltInType.String),
                    GetSchemaForBuiltInType(BuiltInType.Guid),
                    GetSchemaForBuiltInType(BuiltInType.ByteString));
                var idTypeType = EnumSchema.Create("IdentifierType", new string[]
                {
                    "UInt32",
                    "String",
                    "Guid",
                    "ByteString"
                });

                Field field;
                if (_reversibleEncoding)
                {
                    // For reversible encoding this field is a JSON number with
                    // the ServerIndex. The field is omitted if the ServerIndex
                    // is 0.
                    field = new(GetSchemaForBuiltInType(BuiltInType.UInt16), "ServerUri", 3);
                }
                else
                {
                    // For non-reversible encoding this field is the JSON string
                    // containing the ServerUri associated with the ServerIndex
                    // unless the ServerIndex is 0. If the ServerIndex is 0 the
                    // field is omitted.
                    field = new(GetSchemaForBuiltInType(BuiltInType.String), "ServerUri", 3);
                }
                return RecordSchema.Create(nameof(BuiltInType.ExpandedNodeId),
                    new List<Field>
                    {
                        new (idTypeType, "IdType", 0),
                        new (idType, "Id", 1),
                        // For reversible encoding this field is a JSON string
                        // with the NamespaceUri if the NamespaceUri is specified.
                        // Otherwise, it is a JSON number with the NamespaceIndex.
                        // The field is omitted if the NamespaceIndex is 0.
                        // For non-reversible encoding this field is the JSON string
                        // containing the NamespaceUri or the NamespaceUri associated
                        // with the NamespaceIndex unless the NamespaceIndex is 0
                        // or 1. If the NamespaceIndex is 0 the field is omitted.
                        new(AvroUtils.CreateUnion(
                            GetSchemaForBuiltInType(BuiltInType.UInt32),
                            GetSchemaForBuiltInType(BuiltInType.String)),
                            "Namespace", 2),
                        field
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.ExpandedNodeId) });
            }
        }

        private Schema DataValueSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.DataValue),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.Variant), "Value", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "Status", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "SourceTimestamp", 2),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "SourcePicoSeconds", 3),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "ServerTimestamp", 4),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "ServerPicoSeconds", 5)
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DataValue) });
            }
        }

        /// <summary>
        /// Create avro schema for json encoder
        /// </summary>
        /// <param name="reversibleEncoding"></param>
        /// <param name="useUriEncoding"></param>
        public JsonEncodingSchemaBuilder(bool reversibleEncoding, bool useUriEncoding)
        {
            _reversibleEncoding = reversibleEncoding;
            _useUriEncoding = useUriEncoding;
        }

        /// <summary>
        /// Create encoding schema
        /// </summary>
        /// <param name="fieldContentMask"></param>
        public JsonEncodingSchemaBuilder(DataSetFieldContentMask fieldContentMask)
        {
            if ((fieldContentMask & DataSetFieldContentMask.RawData) != 0)
            {
                //
                // If the DataSetFieldContentMask results in a RawData
                // representation, the field value is a Variant encoded
                // using the non-reversible OPC UA JSON Data Encoding
                // defined in OPC 10000-6
                //
                _useUriEncoding = true;
                _reversibleEncoding = false;
            }
            else if (fieldContentMask == 0)
            {
                //
                // If the DataSetFieldContentMask results in a Variant
                // representation, the field value is encoded as a Variant
                // encoded using the reversible OPC UA JSON Data Encoding
                // defined in OPC 10000-6.
                //
                _useUriEncoding = false;
                _reversibleEncoding = true;
            }
            else
            {
                //
                // If the DataSetFieldContentMask results in a DataValue
                // representation, the field value is a DataValue encoded
                // using the non-reversible OPC UA JSON Data Encoding or
                // reversible depending on encoder configuration.
                //
                _reversibleEncoding = false;
                _useUriEncoding = false;
            }
        }

        /// <summary>
        /// Get built in schema. See
        /// https://reference.opcfoundation.org/Core/Part6/v104/docs/5.1.2#_Ref131507956
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="nullable"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override Schema GetSchemaForBuiltInType(BuiltInType builtInType,
            bool nullable = false, bool array = false)
        {
            if (!_builtIn.TryGetValue(builtInType, out var schema))
            {
                // Before we create the schema add a place
                // holder here to break any recursion.

                _builtIn.Add(builtInType, PlaceHolder(builtInType));

                schema = Get((int)builtInType);
                _builtIn[builtInType] = schema;
            }

            // All array members are nullable
            if ((nullable || array) && IsNullableType((int)builtInType))
            {
                schema = schema.AsNullable();
            }
            if (array)
            {
                schema = ArraySchema.Create(schema);
                if (nullable)
                {
                    schema = schema.AsNullable();
                }
            }
            return schema;

            // These are types that are nullable in the json encoding
            static bool IsNullableType(int id)
            {
                return id == 8 || id == 9 || id == 12 ||
                    (id >= 15 && id <= 28);
            }

            Schema Get(int id) => id switch
            {
                0 => AvroUtils.Null,

                1 => PrimitiveType(id, "Boolean", "boolean"),
                2 => PrimitiveType(id, "SByte", "int"),
                3 => PrimitiveType(id, "Byte", "int"),
                4 => PrimitiveType(id, "Int16", "int"),
                5 => PrimitiveType(id, "UInt16", "int"),
                6 => PrimitiveType(id, "Int32", "int"),
                7 => PrimitiveType(id, "UInt32", "int"),

                // As per part 6 encoding, long is encoded as string
                8 => PrimitiveType(id, "Int64", "string"),
                9 => PrimitiveType(id, "UInt64", "string"),

                10 => PrimitiveType(id, "Float", "float"),
                11 => PrimitiveType(id, "Double", "double"),
                12 => PrimitiveType(id, "String", "string"),
                13 => PrimitiveType(id, "DateTime", "string"),
                14 => LogicalType(id, "Guid", "string", "uuid"),
                15 => PrimitiveType(id, "ByteString", "bytes"),
                16 => PrimitiveType(id, "XmlElement", "string"),

                17 => NodeIdSchema,
                18 => ExpandedNodeIdSchema,
                19 => StatusCodeSchema,
                20 => QualifiedNameSchema,
                21 => LocalizedTextSchema,
                22 => ExtensionObjectSchema,
                23 => DataValueSchema,
                24 => VariantSchema,
                25 => DiagnosticInfoSchema,

                26 => PrimitiveType(id, "Number", "string"),

                // Should this be string? As per json encoding, long is string
                27 => PrimitiveType(id, "Integer", "string"),
                28 => PrimitiveType(id, "UInteger", "string"),

                29 => EnumerationSchema,
                _ => throw new ArgumentException($"Built in type {id} unknown")
            };
        }

        /// <summary>
        /// Get object as extension object encoding
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="bodyType"></param>
        /// <returns></returns>
        public override Schema GetExtensionObjectSchema(string name, string ns,
            string dataTypeId, Schema bodyType)
        {
            var encodingType = EnumSchema.Create("Encoding", new string[]
            {
                "Structure",
                "ByteString",
                "XmlElement"
            });
            return RecordSchema.Create(name + nameof(BuiltInType.ExtensionObject),
                new List<Field>
                {
                    new (GetSchemaForBuiltInType(BuiltInType.NodeId), "TypeId", 0),
                    new (encodingType, "Encoding", 1),
                    new (bodyType, "Body", 2)
                }, ns, new[] { dataTypeId });
        }

        /// <summary>
        /// Get data value schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="valueSchema"></param>
        /// <returns></returns>
        public override Schema GetDataValueFieldSchema(string name, Schema valueSchema)
        {
            return RecordSchema.Create(name + nameof(BuiltInType.DataValue),
                new List<Field>
                {
                    new (valueSchema, "Value", 0),
                    new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "Status", 1),
                    new (GetSchemaForBuiltInType(BuiltInType.DateTime), "SourceTimestamp", 2),
                    new (GetSchemaForBuiltInType(BuiltInType.UInt16), "SourcePicoSeconds", 3),
                    new (GetSchemaForBuiltInType(BuiltInType.DateTime), "ServerTimestamp", 4),
                    new (GetSchemaForBuiltInType(BuiltInType.UInt16), "ServerPicoSeconds", 5)
                });
        }

        /// <summary>
        /// Get data typeid
        /// </summary>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        private static string GetDataTypeId(BuiltInType builtInType)
        {
            return "i_" + (int)builtInType;
        }

        /// <summary>
        /// Create logical opc ua derived type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="logicalType"></param>
        /// <returns></returns>
        internal static Schema LogicalType(int builtInType, string name,
            string type, string logicalType)
        {
            var baseType = Schema.Parse(
                $$"""{"type": "{{type}}", "logicalType": "{{logicalType}}"}""");
#if !DERIVE_PRIMITIVE
            return baseType;
#else
            return DerivedSchema.Create(name, baseType, AvroUtils.NamespaceZeroName,
                new[] { GetDataTypeId((BuiltInType)builtInType) });
#endif
        }

        /// <summary>
        /// Create primitive opc ua built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static Schema PrimitiveType(int builtInType, string name,
            string type)
        {
            var baseType = PrimitiveSchema.NewInstance(type);
#if !DERIVE_PRIMITIVE
            return baseType;
#else
            return DerivedSchema.Create(name, baseType, AvroUtils.NamespaceZeroName,
                new[] { GetDataTypeId((BuiltInType)builtInType) });
#endif
        }

        /// <summary>
        /// Create a place holder
        /// </summary>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        private static PlaceHolderSchema PlaceHolder(BuiltInType builtInType)
        {
            return PlaceHolderSchema.Create(builtInType.ToString(),
                AvroUtils.NamespaceZeroName);
        }

        private readonly Dictionary<BuiltInType, Schema> _builtIn = new();
        private readonly bool _reversibleEncoding;
        private readonly bool _useUriEncoding;
    }
}
