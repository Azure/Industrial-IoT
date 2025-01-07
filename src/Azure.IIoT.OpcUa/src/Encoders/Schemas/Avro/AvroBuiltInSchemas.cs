// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Avro
{
    using global::Avro;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides the Avro schemas of built in types and objects
    /// for the Avro binary encoding
    /// </summary>
    internal class AvroBuiltInSchemas : BaseBuiltInSchemas<Schema>
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
                    [
                        new (GetSchemaForBuiltInType(BuiltInType.Int32), "SymbolicId", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.Int32), "NamespaceUri", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.Int32), "Locale", 2),
                        new (GetSchemaForBuiltInType(BuiltInType.Int32), "LocalizedText", 3),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "AdditionalInfo", 4),
                        new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "InnerStatusCode", 5),
                        new (GetSchemaForBuiltInType(BuiltInType.DiagnosticInfo).AsNullable(),
                            "InnerDiagnosticInfo", 6)
                    ], SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DiagnosticInfo) });
            }
        }

        private Schema VariantSchema
        {
            get
            {
                var types = AvroSchema.Null.YieldReturn()
                    .Concat(GetPossibleTypes(SchemaRank.Scalar))
                    .Concat(GetPossibleTypes(SchemaRank.Collection))
                    .Concat(GetPossibleTypes(SchemaRank.Matrix))
                    .ToList();
                return RecordSchema.Create(nameof(BuiltInType.Variant),
                [
                    new (UnionSchema.Create(types), kSingleFieldName, 0)
                ], SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.Variant) });
                IEnumerable<Schema> GetPossibleTypes(SchemaRank valueRank)
                {
                    for (var i = 1; i <= 29; i++)
                    {
                        if (i == (int)BuiltInType.DiagnosticInfo)
                        {
                            continue;
                        }
                        if (i == 26 || i == 27 || i == 28)
                        {
                            continue;
                        }
                        if (i == (int)BuiltInType.Variant && valueRank == SchemaRank.Scalar)
                        {
                            continue; // Array of variant is allowed
                        }
                        if (i == (int)BuiltInType.Byte && valueRank == SchemaRank.Collection)
                        {
                            continue; // Array of bytes is not allowed
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
                    [
                        new (AvroSchema.AsUnion(RecordSchema.Create("EncodedDataType",
                            [
                                new (GetSchemaForBuiltInType(BuiltInType.NodeId), "TypeId", 0),
                                new (GetSchemaForBuiltInType(BuiltInType.ByteString), "Body", 1)
                            ], SchemaUtils.NamespaceZeroName)), kSingleFieldName, 0)
                            // ...
                    ], SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.ExtensionObject) });
            }
        }

        private Schema QualifiedNameSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.QualifiedName),
                    [
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Namespace", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Name", 1)
                    ], SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.QualifiedName) });
            }
        }

        private Schema LocalizedTextSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.LocalizedText),
                    [
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Locale", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Text", 1)
                    ], SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.LocalizedText) });
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
                return RecordSchema.Create(nameof(BuiltInType.NodeId),
                    [
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Namespace", 0),
                        new (idType, "Identifier", 1)
                    ],
                    SchemaUtils.NamespaceZeroName,
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
                return RecordSchema.Create(nameof(BuiltInType.ExpandedNodeId),
                    [
                        new (GetSchemaForBuiltInType(BuiltInType.String), "Namespace", 0),
                        new (idType, "Identifier", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.String), "ServerUri", 3)
                    ],
                    SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.ExpandedNodeId) });
            }
        }

        private Schema DataValueSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.DataValue),
                    [
                        new (GetSchemaForBuiltInType(BuiltInType.Variant), "Value", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "StatusCode", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "SourceTimestamp", 2),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "SourcePicoseconds", 3),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "ServerTimestamp", 4),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "ServerPicoseconds", 5)
                    ], SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DataValue) });
            }
        }

        private static Schema UlongSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.UInt64),
                    [
                        new (UnionSchema.Create(
                        [
                            PrimitiveSchema.NewInstance("int"),
                            FixedSchema.Create("ulong", 8, SchemaUtils.NamespaceZeroName)
                        ]), kSingleFieldName, 0)
                    ], SchemaUtils.NamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DataValue) });
            }
        }

        /// <inheritdoc/>
        public override Schema GetSchemaForBuiltInType(BuiltInType builtInType,
            SchemaRank rank = SchemaRank.Scalar)
        {
            // Always use byte string for byte arrays
            if (builtInType == BuiltInType.Byte && rank == SchemaRank.Collection)
            {
                builtInType = BuiltInType.ByteString;
                rank = SchemaRank.Scalar;
            }

            // TODO: Placeholder caching is needed to avoid stack overflow
            // However we should clear the cache every time we completed the
            // api lookup, to avoid getting placeholder items leaking out
            // to an outside caller.
            if (!_builtIn.TryGetValue((builtInType, rank), out var schema))
            {
                // Before we create the schema add a place
                // holder here to break any recursin.

                _builtIn.Add((builtInType, rank),
                    PlaceHolder(builtInType, rank));

                switch (rank)
                {
                    case SchemaRank.Matrix:
                        schema = MatrixType((int)builtInType, builtInType.ToString());
                        break;
                    case SchemaRank.Collection:
                        schema = CollectionType((int)builtInType, builtInType.ToString());
                        break;
                    default:
                        schema = Get((int)builtInType);
                        break;
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
#if UUID_FIXED
                14 => Fixed(id, "Guid", "uuid", 16),
#else
                14 => Primitive(id, "Guid", "string", "uuid"),
#endif
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

                26 => VariantSchema, // Primitive(id, "Number", "string"),
                27 => VariantSchema, // Primitive(id, "Integer", "string"),
                28 => VariantSchema, // Primitive(id, "UInteger", "string"),

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
                [
                    new (GetSchemaForBuiltInType(BuiltInType.NodeId), "TypeId", 0),
                    new (UnionSchema.Create(
                    [
                        AvroSchema.Null,
                        bodyType,
                        RecordSchema.Create("Encoded",
                        [
                            new (encodingType, "Encoding", 0),
                            new (GetSchemaForBuiltInType(BuiltInType.ByteString), "Bytes", 1)
                        ])
                    ]), "Body", 1)
                ], ns, new[] { dataTypeId });
        }

        /// <inheritdoc/>
        public override Schema GetSchemaForDataSetField(string ns, bool asDataValue,
            Schema valueSchema, BuiltInType builtInType)
        {
            var variantSchema = GetSchemaForBuiltInType(BuiltInType.Variant);
#if USE_VARIANT_FOR_DATAVALUE
            valueSchema = variantSchema;
#endif
            var schemaName = string.Empty;
            var space = SchemaUtils.NamespaceZeroName;
            if (valueSchema.Fullname != variantSchema.Fullname)
            {
                // Variant is by default already nullable
                schemaName = valueSchema.Name;
                space = ns;
                valueSchema = valueSchema.AsNullable();
            }

            if (asDataValue)
            {
                return RecordSchema.Create(schemaName + nameof(BuiltInType.DataValue),
                    [
                        new (valueSchema, "Value", 0),
                        new (GetSchemaForBuiltInType(BuiltInType.StatusCode), "StatusCode", 1),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "SourceTimestamp", 2),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "SourcePicoseconds", 3),
                        new (GetSchemaForBuiltInType(BuiltInType.DateTime), "ServerTimestamp", 4),
                        new (GetSchemaForBuiltInType(BuiltInType.UInt16), "ServerPicoseconds", 5)
                    ], space).AsNullable();
            }
            return valueSchema;
        }

        /// <inheritdoc/>
        public override Schema GetSchemaForRank(Schema schema, SchemaRank rank)
        {
            switch (rank)
            {
                case SchemaRank.Matrix:
                    return MatrixType(schema);
                case SchemaRank.Collection:
                    return CollectionType(schema);
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
            return RecordSchema.Create(name,
            [
                new (baseType, kSingleFieldName, 0)
            ], SchemaUtils.NamespaceZeroName,
                new[] { GetDataTypeId((BuiltInType)builtInType) });
        }

        /// <summary>
        /// Create primitive opc ua built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="name"></param>
        /// <param name="baseName"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        internal static Schema Fixed(int builtInType, string name,
            string baseName, int size)
        {
            var baseType = FixedSchema.Create(baseName, size);
            return RecordSchema.Create(name,
            [
                new (baseType, kSingleFieldName, 0)
            ], SchemaUtils.NamespaceZeroName,
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
            return CollectionType(baseType, name, SchemaUtils.NamespaceZeroName);
        }

        /// <summary>
        /// Create collection type
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="name"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        private static RecordSchema CollectionType(Schema baseType, string? name = null,
            string? space = null)
        {
            name ??= baseType.Name;
            if (space == null && baseType is NamedSchema n)
            {
                space = n.Namespace;
            }
            space ??= SchemaUtils.PublisherNamespace;
            return RecordSchema.Create(name + nameof(SchemaRank.Collection),
            [
                new (baseType.AsArray(), kSingleFieldName, 0)
            ], space);
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
            return MatrixType(baseType, name, SchemaUtils.NamespaceZeroName);
        }

        /// <summary>
        /// Create matrix type
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="name"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        private RecordSchema MatrixType(Schema baseType, string? name = null,
            string? space = null)
        {
            name ??= baseType.Name;
            if (space == null && baseType is NamedSchema n)
            {
                space = n.Namespace;
            }
            space ??= SchemaUtils.PublisherNamespace;
            return RecordSchema.Create(name + nameof(SchemaRank.Matrix),
            [
                new (GetSchemaForBuiltInType(BuiltInType.Int32,
                    SchemaRank.Collection), "Dimensions", 0),
                new (baseType.AsArray(), kSingleFieldName, 0)
            ], space);
        }

        /// <summary>
        /// Create a place holder
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        private static AvroSchema.PlaceHolder PlaceHolder(BuiltInType builtInType,
            SchemaRank rank)
        {
            var name = builtInType.ToString();
            if (rank != SchemaRank.Scalar)
            {
                name += rank;
            }
            return AvroSchema.CreatePlaceHolder(name, SchemaUtils.NamespaceZeroName);
        }

        private const string kSingleFieldName = "Value";
        private readonly Dictionary<(BuiltInType, SchemaRank), Schema> _builtIn = [];
    }
}
