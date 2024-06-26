// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Avro
{
    using global::Avro;
    using Opc.Ua;
    using DataSetFieldContentFlags = Publisher.Models.DataSetFieldContentFlags;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides the json encodings of built in types and objects in Avro schema
    /// </summary>
    internal class JsonBuiltInSchemas : BaseBuiltInSchemas<Schema>
    {
        private Schema EnumerationSchema
        {
            get
            {
                if (_reversibleEncoding)
                {
                    // Enumeration values shall be encoded as a JSON number
                    // for the reversible encoding.
                    return PrimitiveType("int");
                }

                // For the non - reversible form, Enumeration values are
                // encoded as a JSON string with the following format:
                // <name>_<value>
                return PrimitiveType("string");
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
                        new (GetSchemaForBuiltInType(BuiltInType.DiagnosticInfo).AsNullable(),
                            "InnerDiagnosticInfo", 6)
                    }, SchemaUtils.NamespaceZeroName,
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

                var types = AvroSchema.Null.YieldReturn()
                    .Concat(GetPossibleTypes(SchemaRank.Scalar))
                    .Concat(GetPossibleTypes(SchemaRank.Collection))
                    .Concat(GetPossibleTypes(SchemaRank.Matrix))
                    .ToList();
                return RecordSchema.Create(nameof(BuiltInType.Variant), new List<Field>
                {
                    new (types.AsUnion(), "Value", 0)
                }, SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.Variant) });

                IEnumerable<Schema> GetPossibleTypes(SchemaRank valueRank)
                {
                    for (var i = 1; i <= 29; i++)
                    {
                        if (i == (int)BuiltInType.DiagnosticInfo)
                        {
                            continue;
                        }
                        if (i == (int)BuiltInType.Variant && valueRank == SchemaRank.Scalar)
                        {
                            continue; // Array of variant is allowed
                        }
                        yield return GetSchemaForBuiltInType((BuiltInType)i, valueRank);
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

                var bodyType = AvroSchema.AsUnion(
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
                    }, SchemaUtils.NamespaceZeroName,
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
                    return PrimitiveType("int");
#else
                    return DerivedSchema.Create(nameof(BuiltInType.StatusCode),
                        GetSchemaForBuiltInType(BuiltInType.UInt32),
                        SchemaUtils.NamespaceZeroName,
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
                    }, SchemaUtils.NamespaceZeroName);
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
                    }, SchemaUtils.NamespaceZeroName,
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
                        }, SchemaUtils.NamespaceZeroName,
                        new[] { GetDataTypeId(BuiltInType.LocalizedText) });
                }

                // For the non-reversible form, LocalizedText value shall
                // be encoded as a JSON string containing the Text component.
#if !DERIVE_PRIMITIVE
                return PrimitiveType("string");
#else
                    return DerivedSchema.Create(nameof(BuiltInType.LocalizedText),
                        GetSchemaForBuiltInType(BuiltInType.String),
                        SchemaUtils.NamespaceZeroName,
                            new[] { GetDataTypeId(BuiltInType.LocalizedText) });
#endif
            }
        }

        private Schema NodeIdSchema
        {
            get
            {
                var idType = AvroSchema.AsUnion(
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
                }, SchemaUtils.NamespaceZeroName);

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
                    }, SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.NodeId) });
            }
        }

        private Schema ExpandedNodeIdSchema
        {
            get
            {
                var idType = AvroSchema.AsUnion(
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
                }, SchemaUtils.NamespaceZeroName);

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
                        new(AvroSchema.AsUnion(
                            GetSchemaForBuiltInType(BuiltInType.UInt32),
                            GetSchemaForBuiltInType(BuiltInType.String)),
                            "Namespace", 2),
                        field
                    }, SchemaUtils.NamespaceZeroName,
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
                    }, SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DataValue) });
            }
        }

        /// <summary>
        /// Create avro schema for json encoder
        /// </summary>
        /// <param name="reversibleEncoding"></param>
        /// <param name="useUriEncoding"></param>
        public JsonBuiltInSchemas(bool reversibleEncoding, bool useUriEncoding)
        {
            _reversibleEncoding = reversibleEncoding;
            _useUriEncoding = useUriEncoding;
        }

        /// <summary>
        /// Create encoding schema
        /// </summary>
        /// <param name="fieldContentMask"></param>
        public JsonBuiltInSchemas(DataSetFieldContentFlags fieldContentMask)
        {
            if ((fieldContentMask & DataSetFieldContentFlags.RawData) != 0)
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
        /// <param name="rank"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override Schema GetSchemaForBuiltInType(BuiltInType builtInType,
            SchemaRank rank = SchemaRank.Scalar)
        {
            // Always use byte string for byte arrays
            if (builtInType == BuiltInType.Byte && rank == SchemaRank.Collection)
            {
                builtInType = BuiltInType.ByteString;
                rank = SchemaRank.Scalar;
            }
            if (!_builtIn.TryGetValue(builtInType, out var schema))
            {
                // Before we create the schema add a place
                // holder here to break any recursion.

                _builtIn.Add(builtInType, PlaceHolder(builtInType));

                schema = Get((int)builtInType);
                _builtIn[builtInType] = schema;
            }
            if (rank != SchemaRank.Scalar)
            {
                schema = schema.AsArray();
            }
            return schema;

            Schema Get(int id) => id switch
            {
                0 => AvroSchema.Null,

                1 => PrimitiveType("boolean"),
                2 => PrimitiveType("int"),
                3 => PrimitiveType("int"),
                4 => PrimitiveType("int"),
                5 => PrimitiveType("int"),
                6 => PrimitiveType("int"),
                7 => PrimitiveType("int"),

                // As per part 6 encoding, long is encoded as string
                8 => PrimitiveType("string"),
                9 => PrimitiveType("string"),

                10 => PrimitiveType("float"),
                11 => PrimitiveType("double"),
                12 => PrimitiveType("string"),
                13 => PrimitiveType("string"),
                14 => LogicalType("string", "uuid"),
                15 => PrimitiveType("bytes"),
                16 => PrimitiveType("string"),

                17 => NodeIdSchema,
                18 => ExpandedNodeIdSchema,
                19 => StatusCodeSchema,
                20 => QualifiedNameSchema,
                21 => LocalizedTextSchema,
                22 => ExtensionObjectSchema,
                23 => DataValueSchema,
                24 => VariantSchema,
                25 => DiagnosticInfoSchema,

                26 => PrimitiveType("string"),

                // Should this be string? As per json encoding, long is string
                27 => PrimitiveType("string"),
                28 => PrimitiveType("string"),

                29 => EnumerationSchema,
                _ => throw new ArgumentException($"Built in type {id} unknown")
            };
        }

        /// <inheritdoc/>
        public override Schema GetSchemaForExtendableType(string name, string ns,
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

        /// <inheritdoc/>
        public override Schema GetSchemaForDataSetField(string ns, bool asDataValue,
            Schema valueSchema, BuiltInType builtInType)
        {
            if (asDataValue)
            {
                return RecordSchema.Create(valueSchema.Name + nameof(BuiltInType.DataValue),
                    new List<Field>
                    {
                        new (valueSchema, "Value", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "Status", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "SourceTimestamp", 2),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "SourcePicoseconds", 3),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "ServerTimestamp", 4),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "ServerPicoseconds", 5)
                    }, ns).AsNullable();
            }
            return valueSchema;
        }

        /// <inheritdoc/>
        public override Schema GetSchemaForRank(Schema schema, SchemaRank rank)
        {
            switch (rank)
            {
                case SchemaRank.Matrix:
                    // Variant schema
                    return GetSchemaForBuiltInType(BuiltInType.Variant);
                case SchemaRank.Collection:
                    return schema.AsArray();
                default:
                    return schema;
            }
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
        /// <param name="type"></param>
        /// <param name="logicalType"></param>
        /// <returns></returns>
        internal static Schema LogicalType(string type, string logicalType)
        {
            return Schema.Parse(
                $$"""{"type": "{{type}}", "logicalType": "{{logicalType}}"}""");
        }

        /// <summary>
        /// Create primitive opc ua built in type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static Schema PrimitiveType(string type)
        {
            return PrimitiveSchema.NewInstance(type);
        }

        /// <summary>
        /// Create a place holder
        /// </summary>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        private static AvroSchema.PlaceHolder PlaceHolder(BuiltInType builtInType)
        {
            return AvroSchema.CreatePlaceHolder(builtInType.ToString(),
                SchemaUtils.NamespaceZeroName);
        }

        private readonly Dictionary<BuiltInType, Schema> _builtIn = new();
        private readonly bool _reversibleEncoding;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly bool _useUriEncoding;
#pragma warning restore IDE0052 // Remove unread private members
    }
}
