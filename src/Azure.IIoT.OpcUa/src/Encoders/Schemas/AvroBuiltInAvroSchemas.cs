﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Avro;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides the Avro schemas of built in types and objects
    /// for the Avro binary encoding
    /// </summary>
    internal class AvroBuiltInAvroSchemas : BaseBuiltInSchemas<Schema>
    {
        private static Schema EnumerationSchema
        {
            get
            {
                // Enumeration is a record of type int
                return Primitive((int)BuiltInType.Enumeration,
                    nameof(BuiltInType.Enumeration), "int");
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
                var types = AvroSchema.Null.YieldReturn()
                    .Concat(GetPossibleTypes(ValueRanks.Scalar))
                    .Concat(GetPossibleTypes(ValueRanks.OneDimension))
                    .Concat(GetPossibleTypes(ValueRanks.TwoDimensions))
                    .ToList();
                return RecordSchema.Create(nameof(BuiltInType.Variant), new List<Field>
                {
                    new (UnionSchema.Create(types), kSingleFieldName, 0)
                }, SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.Variant) });
                IEnumerable<Schema> GetPossibleTypes(int valueRank)
                {
                    for (var i = 1; i <= 29; i++)
                    {
                        if (i == (int)BuiltInType.DiagnosticInfo)
                        {
                            continue;
                        }
                        if (i == (int)BuiltInType.Variant && valueRank == ValueRanks.Scalar)
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
                return RecordSchema.Create(nameof(BuiltInType.ExtensionObject),
                    new List<Field>
                    {
                        new (AvroSchema.CreateUnion(RecordSchema.Create("EncodedDataType",
                            new List<Field>
                            {
                                new (GetSchemaForBuiltInType(BuiltInType.NodeId), "TypeId", 0),
                                new (GetSchemaForBuiltInType(BuiltInType.ByteString), "Body", 1)
                            }, SchemaUtils.NamespaceZeroName)), kSingleFieldName, 0)
                            // ...
                    }, SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.ExtensionObject) });
            }
        }

        private Schema QualifiedNameSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.QualifiedName),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Namespace", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Name", 1)
                    }, SchemaUtils.NamespaceZeroName,
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
                    }, SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.LocalizedText) });
            }
        }

        private Schema NodeIdSchema
        {
            get
            {
                var idType = AvroSchema.CreateUnion(
                    GetSchemaForBuiltInType(BuiltInType.UInt32),
                    GetSchemaForBuiltInType(BuiltInType.String),
                    GetSchemaForBuiltInType(BuiltInType.Guid),
                    GetSchemaForBuiltInType(BuiltInType.ByteString));
                return RecordSchema.Create(nameof(BuiltInType.NodeId),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Namespace", 0),
                        new (idType, "Identifier", 1)
                    },
                    SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.NodeId) });
            }
        }

        private Schema ExpandedNodeIdSchema
        {
            get
            {
                var idType = AvroSchema.CreateUnion(
                    GetSchemaForBuiltInType(BuiltInType.UInt32),
                    GetSchemaForBuiltInType(BuiltInType.String),
                    GetSchemaForBuiltInType(BuiltInType.Guid),
                    GetSchemaForBuiltInType(BuiltInType.ByteString));
                return RecordSchema.Create(nameof(BuiltInType.ExpandedNodeId),
                    new List<Field>
                    {
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Namespace", 0),
                        new (idType, "Identifier", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "ServerUri", 3)
                    },
                    SchemaUtils.NamespaceZeroName,
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
                        new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "StatusCode", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "SourceTimestamp", 2),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "SourcePicoseconds", 3),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "ServerTimestamp", 4),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "ServerPicoseconds", 5)
                    }, SchemaUtils.NamespaceZeroName,
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
                            FixedSchema.Create("ulong", 8, SchemaUtils.NamespaceZeroName)
                        }), kSingleFieldName, 0)
                    }, SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DataValue) });
            }
        }

        /// <inheritdoc/>
        public override Schema GetSchemaForBuiltInType(BuiltInType builtInType,
            int rank = ValueRanks.Scalar)
        {
            if (!_builtIn.TryGetValue((builtInType, rank), out var schema))
            {
                // Before we create the schema add a place
                // holder here to break any recursin.

                _builtIn.Add((builtInType, rank),
                    PlaceHolder(builtInType, rank));

                if (rank >= ValueRanks.TwoDimensions)
                {
                    schema = MatrixType((int)builtInType,
                        builtInType.ToString());
                }
                else if (rank >= ValueRanks.OneOrMoreDimensions)
                {
                    schema = CollectionType((int)builtInType,
                        builtInType.ToString());
                }
                else
                {
                    schema = Get((int)builtInType);
                }
                _builtIn[(builtInType, rank)] = schema;
            }
            return schema;

            Schema Get(int id) => id switch
            {
                0 => AvroSchema.Null,

                1 => Primitive(id, "Boolean", "boolean"),

                2 => Primitive(id, "SByte", "int"),
                3 => Primitive(id, "Byte", "int"),
                4 => Primitive(id, "Int16", "int"),
                5 => Primitive(id, "UInt16", "int"),
                6 => Primitive(id, "Int32", "int"),
                7 => Primitive(id, "UInt32", "int"),
                8 => Primitive(id, "Int64", "int"),

                9 => UlongSchema,

                10 => Primitive(id, "Float", "float"),
                11 => Primitive(id, "Double", "double"),
                12 => Primitive(id, "String", "string"),
                13 => Primitive(id, "DateTime", "long"),
                14 => Primitive(id, "Guid", "string", "uuid"),
                15 => Primitive(id, "ByteString", "bytes"),
                16 => Primitive(id, "XmlElement", "string"),

                17 => NodeIdSchema,
                18 => ExpandedNodeIdSchema,
                19 => Primitive(id, "StatusCode", "long"),

                20 => QualifiedNameSchema,
                21 => LocalizedTextSchema,
                22 => ExtensionObjectSchema,
                23 => DataValueSchema,
                24 => VariantSchema,
                25 => DiagnosticInfoSchema,

                26 => Primitive(id, "Number", "string"),
                27 => Primitive(id, "Integer", "string"),
                28 => Primitive(id, "UInteger", "string"),

                29 => EnumerationSchema,
                _ => throw new ArgumentException($"Built in type {id} unknown")
            };
        }

        /// <inheritdoc/>
        public override Schema GetSchemaForExtendableType(string name, string ns,
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
                        AvroSchema.Null,
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
        public override Schema GetSchemaForDataSetField(string name, string ns, bool asDataValue,
            Schema valueSchema)
        {
            if (asDataValue)
            {
                return RecordSchema.Create(name,
                    new List<Field>
                    {
                        new (valueSchema, "Value", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "Status", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "SourceTimestamp", 2),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "SourcePicoSeconds", 3),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "ServerTimestamp", 4),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "ServerPicoSeconds", 5)
                    }, ns).AsNullable();
            }
            if (valueSchema is UnionSchema)
            {
                return valueSchema; // Variant is by default already nullable
            }
            return valueSchema.AsNullable();
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
        /// Create primitive opc ua built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="logicalType"></param>
        /// <returns></returns>
        internal static Schema Primitive(int builtInType, string name,
            string type, string? logicalType = null)
        {
            var baseType = logicalType == null ?
                PrimitiveSchema.NewInstance(type) : Schema.Parse(
                $$"""{"type": "{{type}}", "logicalType": "{{logicalType}}"}""");
            return RecordSchema.Create(name, new List<Field>
            {
                new (baseType, kSingleFieldName, 0)
            }, SchemaUtils.NamespaceZeroName,
                new[] { GetDataTypeId((BuiltInType)builtInType) });
        }

        /// <summary>
        /// Create collection opc ua built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Schema CollectionType(int builtInType, string name)
        {
            var baseType = GetSchemaForBuiltInType((BuiltInType)builtInType);
            return RecordSchema.Create(name + "Collection", new List<Field>
            {
                new (ArraySchema.Create(baseType), kSingleFieldName, 0)
            }, SchemaUtils.NamespaceZeroName);
        }

        /// <summary>
        /// Create matrix opc ua built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Schema MatrixType(int builtInType, string name)
        {
            var baseType = GetSchemaForBuiltInType((BuiltInType)builtInType);
            return RecordSchema.Create(name + "Matrix", new List<Field>
            {
                new (GetSchemaForBuiltInType(BuiltInType.Int32,
                    ValueRanks.OneDimension), "Dimensions", 0),
                new (ArraySchema.Create(baseType), kSingleFieldName, 0)
            }, SchemaUtils.NamespaceZeroName);
        }

        /// <summary>
        /// Create a place holder
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        private static PlaceHolderSchema PlaceHolder(BuiltInType builtInType,
            int rank)
        {
            var name = builtInType.ToString();
            if (rank >= ValueRanks.TwoDimensions)
            {
                name += "Matrix";
            }
            else if (rank >= ValueRanks.OneOrMoreDimensions)
            {
                name += "Collection";
            }
            return PlaceHolderSchema.Create(name, SchemaUtils.NamespaceZeroName);
        }

        private const string kSingleFieldName = "Value";
        private readonly Dictionary<(BuiltInType, int), Schema> _builtIn = new();
    }
}
