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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides the json encodings of built in types and objects in Avro schema
    /// </summary>
    internal class AvroEncodingSchemaBuilder : EncodingSchemaBuilder
    {
        private static Schema EnumerationSchema
        {
            get
            {
                // Enumeration is a record of type int
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
                var types = GetPossibleTypes(false)
                    .Concat(GetPossibleTypes(true))
                    .ToList();
                var bodyType = UnionSchema.Create(types);
                return RecordSchema.Create(nameof(BuiltInType.Variant),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(
                            BuiltInType.Int32, false, true), "Dimensions", 0),
                        new (bodyType, "Body", 1)
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
                return RecordSchema.Create(nameof(BuiltInType.QualifiedName),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Name", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt32), "Uri", 1)
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.QualifiedName) });
            }
        }

        private Schema LocalizedTextSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.LocalizedText),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Locale", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Text", 1)
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.LocalizedText) });
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
                return RecordSchema.Create(nameof(BuiltInType.NodeId),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.UInt32), "Namespace", 0),
                        new (idType, "Identifier", 1)
                    },
                    AvroUtils.NamespaceZeroName,
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
                return RecordSchema.Create(nameof(BuiltInType.ExpandedNodeId),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.UInt32), "Namespace", 0),
                        new (idType, "Identifier", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "NamespaceUri", 2),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt32), "ServerUri", 3)
                    },
                    AvroUtils.NamespaceZeroName,
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

        private static Schema UlongSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.UInt64),
                    new List<Field>
                    {
                        new (UnionSchema.Create(new List<Schema>
                        {
                            PrimitiveSchema.NewInstance("int"),
                            FixedSchema.Create("ulong", 8, AvroUtils.NamespaceZeroName)
                        }), kSingleFieldName, 0)
                    }, AvroUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DataValue) });
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
            if (!_builtIn.TryGetValue((builtInType, array), out var schema))
            {
                // Before we create the schema add a place
                // holder here to break any recursion.

                _builtIn.Add((builtInType, array),
                    PlaceHolder(builtInType, array));

                if (array)
                {
                    schema = CollectionType((int)builtInType,
                        builtInType.ToString());
                    if (nullable)
                    {
                        schema = schema.AsNullable();
                    }
                }
                else
                {
                    schema = Get((int)builtInType);
                }
                _builtIn[(builtInType, array)] = schema;
            }

            // All array members are nullable
            if (nullable && IsNullable((int)builtInType))
            {
                schema = schema.AsNullable();
            }
            return schema;

            // These are types that are nullable in the avro encoding
            static bool IsNullable(int id)
            {
                return id == 12 || id == 15 || id == 16 ||
                    (id >= 20 && id <= 25);
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
                8 => PrimitiveType(id, "Int64", "int"),

                9 => UlongSchema,

                10 => PrimitiveType(id, "Float", "float"),
                11 => PrimitiveType(id, "Double", "double"),

                12 => PrimitiveType(id, "String", "string"),        // Nullable

                13 => LogicalType(id, "DateTime", "long", "timestamp-millis"),
                14 => LogicalType(id, "Guid", "string", "uuid"),

                15 => PrimitiveType(id, "ByteString", "bytes"),     // Nullable
                16 => PrimitiveType(id, "XmlElement", "string"),    // Nullable

                17 => NodeIdSchema,
                18 => ExpandedNodeIdSchema,
                19 => StatusCodeSchema,

                20 => QualifiedNameSchema,                          // Nullable
                21 => LocalizedTextSchema,                          // Nullable
                22 => ExtensionObjectSchema,                        // Nullable
                23 => DataValueSchema,                              // Nullable
                24 => VariantSchema,                                // Nullable
                25 => DiagnosticInfoSchema,                         // Nullable

                26 => PrimitiveType(id, "Number", "string"),
                27 => PrimitiveType(id, "Integer", "string"),
                28 => PrimitiveType(id, "UInteger", "string"),

                29 => EnumerationSchema,
                _ => throw new ArgumentException($"Built in type {id} unknown")
            };
        }

        /// <inheritdoc/>
        public override Schema GetExtensionObjectSchema(string name, string ns,
            string dataTypeId, Schema bodyType)
        {
            // Extension objects are records of fields
            // 1. Encoding Node Id
            // 2. A union of
            //   1. null
            //   2. A encodeable type
            //   3. A record with
            //     1. ExtensionObjectEncoding type enum
            //     2. bytes that are either binary opc ua or xml/json utf 8

            var encodingType = EnumSchema.Create("ExtensionObjectEncoding", new string[]
            {
                "None",
                "Binary",
                "Xml",
                "Reserved1",
                "ByteString"
            });
            return RecordSchema.Create(name + nameof(BuiltInType.ExtensionObject),
                new List<Field>
                {
                    new (GetSchemaForBuiltInType(BuiltInType.NodeId), "TypeId", 0),
                    new (UnionSchema.Create(new List<Schema>
                    {
                        AvroUtils.Null,
                        bodyType,
                        RecordSchema.Create("Encoded", new List<Field>
                        {
                            new (encodingType, "Encoding", 0),
                            new (GetSchemaForBuiltInType(BuiltInType.ByteString), "Bytes", 1)
                        })
                    }), "Body", 1)
                }, ns, new[] { dataTypeId });
        }

        /// <inheritdoc/>
        public override Schema GetDataValueFieldSchema(string name, Schema valueSchema)
        {
            return RecordSchema.Create(name + nameof(BuiltInType.DataValue),
                new List<Field>
                {
                    new (GetVariantFieldSchema(name, valueSchema), "Value", 0),
                    new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "Status", 1),
                    new (GetSchemaForBuiltInType(BuiltInType.DateTime), "SourceTimestamp", 2),
                    new (GetSchemaForBuiltInType(BuiltInType.UInt16), "SourcePicoSeconds", 3),
                    new (GetSchemaForBuiltInType(BuiltInType.DateTime), "ServerTimestamp", 4),
                    new (GetSchemaForBuiltInType(BuiltInType.UInt16), "ServerPicoSeconds", 5)
                });
        }

        /// <inheritdoc/>
        public override Schema GetVariantFieldSchema(string name, Schema valueSchema)
        {
            return RecordSchema.Create(name + nameof(BuiltInType.Variant),
                new List<Field>
                {
                    new (GetSchemaForBuiltInType(BuiltInType.Int32, false, true), "Dimensions", 0),
                    new (GetSchemaForBuiltInType(BuiltInType.Byte), "Reserved1", 1), // Stub out union id
                    new (valueSchema, "Body", 2)
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
            return RecordSchema.Create(name, new List<Field>
            {
                new (baseType, kSingleFieldName, 0)
            }, AvroUtils.NamespaceZeroName,
                new[] { GetDataTypeId((BuiltInType)builtInType) });
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
            return RecordSchema.Create(name, new List<Field>
            {
                new (baseType, kSingleFieldName, 0)
            }, AvroUtils.NamespaceZeroName,
                new[] { GetDataTypeId((BuiltInType)builtInType) });
        }

        /// <summary>
        /// Create primitive opc ua built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Schema CollectionType(int builtInType, string name)
        {
            var baseType = GetSchemaForBuiltInType((BuiltInType)builtInType, true);
            return RecordSchema.Create(name + "Collection", new List<Field>
            {
                new (ArraySchema.Create(baseType), kSingleFieldName, 0)
            }, AvroUtils.NamespaceZeroName);
        }

        /// <summary>
        /// Create a place holder
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        private static PlaceHolderSchema PlaceHolder(BuiltInType builtInType, bool array)
        {
            var name = builtInType.ToString();
            if (array)
            {
                name += "Collection";
            }
            return PlaceHolderSchema.Create(name,
                AvroUtils.NamespaceZeroName);
        }

        private const string kSingleFieldName = "Value";
        private readonly Dictionary<(BuiltInType, bool), Schema> _builtIn = new();
    }
}
