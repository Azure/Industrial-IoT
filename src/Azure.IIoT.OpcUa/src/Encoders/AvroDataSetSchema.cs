// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Avro;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.AccessControl;
    using System.Text;
    using System.Text.Json.Serialization.Metadata;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    /// <summary>
    /// Extensions to convert metadata into avro schema. Note that this class
    /// generates a schema that complies with the json representation in
    /// <see cref="JsonEncoderEx.WriteDataSet(string?, Models.DataSet?)"/> This
    /// depends on the network settings and reversible vs. nonreversible
    /// encoding mode.
    /// </summary>
    public partial class AvroDataSetSchema
    {
        /// <summary>
        /// The actual schema
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="namespaceTable"></param>
        /// <param name="encoding"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <returns></returns>
        public AvroDataSetSchema(PublishedDataSetModel dataSet,
            NamespaceTable? namespaceTable = null, MessageEncoding? encoding = null,
            Publisher.Models.DataSetFieldContentMask? dataSetFieldContentMask = null)
        {
            ArgumentNullException.ThrowIfNull(dataSet);
            _context = new ServiceMessageContext
            {
                NamespaceUris = namespaceTable ?? new NamespaceTable()
            };
            _encoding = encoding ?? MessageEncoding.Json;

            var singleValue = dataSet.EnumerateMetaData().Take(2).Count() != 1;
            GetEncodingMode(
                out _reversibleEncoding,
                out _useUriEncoding,
                out _omitFieldName,
                out _fieldsAreDataValues,
                singleValue,
                (uint)(dataSetFieldContentMask ?? 0u));

            Schema = Compile(dataSet);
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="namespaceTable"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public AvroDataSetSchema(DataSetWriterModel dataSetWriter,
            NamespaceTable? namespaceTable, MessageEncoding? encoding) :
            this(dataSetWriter.DataSet ?? throw new ArgumentException(
                "Missing data set in writer"), namespaceTable, encoding,
                dataSetWriter.DataSetFieldContentMask)
        {
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToString();
        }

        /// <summary>
        /// Compile
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        private Schema Compile(PublishedDataSetModel dataSet)
        {
            // Collect types
            CollectTypes(dataSet);

            // Compile collected types to schemas
            foreach (var type in _types.Values)
            {
                if (type.Schema == null)
                {
                    type.Resolve(this);
                }
            }

            var schemas = GetDataSetSchemas(dataSet).ToList();
            if (schemas.Count != 1)
            {
                return UnionSchema.Create(schemas);
            }
            return schemas[0];
        }

        /// <summary>
        /// Create data set schemas
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        private IEnumerable<Schema> GetDataSetSchemas(PublishedDataSetModel dataSet)
        {
            var fields = new List<Field>();
            var pos = 0;
            foreach (var (fieldName, fieldMetadata) in dataSet.EnumerateMetaData())
            {
                // Now collect the fields of the payload
                pos++;
                if (fieldMetadata?.DataType != null)
                {
                    if (_omitFieldName)
                    {
                        yield return LookupSchema(m.MetaData.DataType, false);
                    }
                    else if (fieldName != null)
                    {
                        var schema = LookupSchema(fieldMetadata.DataType, true);
                        if (_fieldsAreDataValues)
                        {
                            // TODO: we want to take the data set and
                            // network message format into account here
                            // in particular the DataValue definition
                            schema = GetDataValueFieldSchema(schema.Name, schema);
                        }
                        fields.Add(new Field(schema, Escape(fieldName), pos));
                    }
                }
            }
            if (!_omitFieldName)
            {
                yield return RecordSchema.Create(Escape(dataSet.Name ?? "Payload"),
                    fields);
            }
        }

        /// <summary>
        /// Collect types from data set
        /// </summary>
        /// <param name="dataSet"></param>
        private void CollectTypes(PublishedDataSetModel dataSet)
        {
            foreach (var (_, fieldMetadata) in dataSet!
                .EnumerateMetaData()
                .Where(m => m.MetaData != null))
            {
                Debug.Assert(fieldMetadata != null);
                if (fieldMetadata.StructureDataTypes != null)
                {
                    foreach (var t in fieldMetadata.StructureDataTypes)
                    {
                        if (!_types.ContainsKey(t.DataTypeId))
                        {
                            _types.Add(t.DataTypeId, new StructureType(t));
                        }
                    }
                }
                if (fieldMetadata.SimpleDataTypes != null)
                {
                    foreach (var t in fieldMetadata.SimpleDataTypes)
                    {
                        if (!_types.ContainsKey(t.DataTypeId))
                        {
                            _types.Add(t.DataTypeId, new SimpleType(t));
                        }
                    }
                }
                if (fieldMetadata.EnumDataTypes != null)
                {
                    foreach (var t in fieldMetadata.EnumDataTypes)
                    {
                        if (!_types.ContainsKey(t.DataTypeId))
                        {
                            _types.Add(t.DataTypeId, new EnumType(t));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get schema
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="nullable"></param>
        /// <param name="valueRank"></param>
        /// <param name="arrayDimensions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private Schema LookupSchema(string dataType, bool nullable,
            int valueRank = 1, IReadOnlyList<uint>? arrayDimensions = null)
        {
            Schema? schema = null;
            if (_types.TryGetValue(dataType, out var description))
            {
                if (description.Schema == null)
                {
                    description.Resolve(this);
                }
                if (description.Schema != null)
                {
                    schema = description.Schema;
                }
                if (nullable && schema != null)
                {
                    schema = Nullable(schema);
                }
            }

            if (arrayDimensions != null)
            {
                valueRank = arrayDimensions.Count;
            }
            var array = valueRank > 1;

            schema ??= GetBuiltInDataTypeSchema(dataType, nullable && !array);
            if (schema != null)
            {
                if (array)
                {
                    // TODO: we also have matrices, we should have schemas for all
                    schema = ArraySchema.Create(schema);
                    if (nullable)
                    {
                        schema = Nullable(schema);
                    }
                }
                return schema;
            }
            throw new ArgumentException($"No Schema found for {dataType}");

            Schema? GetBuiltInDataTypeSchema(string dataType, bool nullable)
            {
                if (int.TryParse(dataType[2..], out var id)
                    && id >= 0 && id <= 29)
                {
                    return BuiltInSchema((BuiltInType)id, nullable);
                }
                return null;
            }
        }


        /// <summary>
        /// Get built in schema. See
        /// https://reference.opcfoundation.org/Core/Part6/v104/docs/5.1.2#_Ref131507956
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="nullable"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal Schema BuiltInSchema(BuiltInType builtInType,
            bool nullable = false)
        {
            if (!_builtIn.TryGetValue(builtInType, out var schema))
            {
                schema = Get((int)builtInType);
                _builtIn.Add(builtInType, schema);
            }
            if (nullable && IsNullable((int)builtInType))
            {
                return Nullable(schema);
            }
            return schema;

            // These are types that are nullable in the json encoding
            static bool IsNullable(int id)
            {
                return id == 8 || id == 9 || id == 12 ||
                    (id >= 15 && id <= 28);
            }

            Schema Get(int id) => id switch
            {
                0 => Null,

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
                13 => PrimitiveType(id, "DateTime", "string"), // TODO ISO type?
                14 => LogicalType(id, "Guid", "string", "uuid"),
                15 => PrimitiveType(id, "ByteString", "bytes"),
                16 => PrimitiveType(id, "XmlElement", "string"),

                17 => NodeIdSchema, // "NodeId",
                18 => ExpandedNodeIdSchema, // "ExpandedNodeId",
                19 => StatusCodeSchema, // "StatusCode",
                20 => QualifiedNameSchema, // "QualifiedName",
                21 => LocalizedTextSchema, // "LocalizedText",
                22 => ExtensionObjectSchema, // "ExtensionObject",
                23 => DataValueSchema, // "DataValue",
                24 => VariantSchema, // "Variant",
                25 => DiagnosticInfoSchema, // "DiagnosticInfo",

                26 => PrimitiveType(id, "Number", "string"),

                // Should this be string? As per json encoding, long is string
                27 => PrimitiveType(id, "Integer", "string"),
                28 => PrimitiveType(id, "UInteger", "string"),

                29 => EnumerationSchema,
                _ => throw new ArgumentException($"Built in type {id} unknown")
            };
        }

        internal Schema EnumerationSchema
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

        internal Schema DiagnosticInfoSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.DiagnosticInfo),
                    new List<Field>
                    {
                        new (BuiltInSchema(BuiltInType.Int32), "SymbolicId", 0),
                        new (BuiltInSchema(BuiltInType.Int32), "NamespaceUri", 1),
                        new (BuiltInSchema(BuiltInType.Int32), "Locale", 2),
                        new (BuiltInSchema(BuiltInType.Int32), "LocalizedText", 3),
                        new (BuiltInSchema(BuiltInType.String), "AdditionalInfo", 4),
                        new (BuiltInSchema(BuiltInType.StatusCode), "InnerStatusCode", 5),
                        new (BuiltInSchema(BuiltInType.DiagnosticInfo),
                            "InnerDiagnosticInfo", 6)
                    }, kNamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DiagnosticInfo) });
            }
        }

        internal Schema VariantSchema
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
                var bodyType = UnionSchema.Create(types);
                IEnumerable<Schema> GetPossibleTypes(bool array)
                {
                    if (array) // TODO: Fix
                    {
                        yield break;
                    }
                    for (var i = 0; i <= 29; i++)
                    {
                        if (i == (int)BuiltInType.Variant ||
                            i == (int)BuiltInType.ExtensionObject ||
                            i == (int)BuiltInType.DiagnosticInfo ||
                            i == (int)BuiltInType.DataValue)
                        {
                            continue; // TODO: Array of variant is allowed
                        }
                        var schema = BuiltInSchema((BuiltInType)i);
                        yield return array ? ArraySchema.Create(schema) : schema;
                    }
                }
                return RecordSchema.Create(nameof(BuiltInType.Variant),
                    new List<Field>
                    {
                        new (BuiltInSchema(BuiltInType.UInt16), "Type", 0),
                        new (bodyType, "Body", 1),
                        new (ArraySchema.Create(BuiltInSchema(BuiltInType.Int32)),
                            "Dimensions", 2)
                    }, kNamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.Variant) });
            }
        }

        internal Schema ExtensionObjectSchema
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

                var bodyType = UnionSchema.Create(new List<Schema>
                {
                    BuiltInSchema(BuiltInType.Null),
                    BuiltInSchema(BuiltInType.String),
                    BuiltInSchema(BuiltInType.XmlElement),
                    BuiltInSchema(BuiltInType.ByteString)
                });
                var encodingType = EnumSchema.Create("Encoding", new string[]
                {
                    "Structure",
                    "ByteString",
                    "XmlElement"
                });
                return RecordSchema.Create(nameof(BuiltInType.ExtensionObject),
                    new List<Field>
                    {
                        new (BuiltInSchema(BuiltInType.NodeId), "TypeId", 0),
                        new (encodingType, "Encoding", 1),
                        new (bodyType, "Body", 2)
                    }, kNamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.ExtensionObject) });
            }
        }

        internal Schema StatusCodeSchema
        {
            get
            {
                if (_reversibleEncoding)
                {
                    // For the non - reversible form, StatusCode values
                    // shall be encoded as a JSON object with the fields
                    // defined here.
                    return RecordSchema.Create(nameof(BuiltInType.StatusCode),
                        new List<Field>
                        {
                            new (BuiltInSchema(BuiltInType.UInt32), "Code", 0),
                            new (BuiltInSchema(BuiltInType.String), "Symbol", 1)
                        }, kNamespaceZeroName);
                }

                //
                // StatusCode values shall be encoded as a JSON number for
                // the reversible encoding. If the StatusCode is Good (0)
                // it is only encoded if it is an element of a JSON array.
                //
                return CreateDerivedSchema(nameof(BuiltInType.StatusCode),
                    BuiltInSchema(BuiltInType.UInt32), kNamespaceZeroName,
                        new[] { GetDataTypeId(BuiltInType.StatusCode) });
            }
        }

        internal Schema QualifiedNameSchema
        {
            get
            {
                Field field;
                if (_reversibleEncoding)
                {
                    // For reversible encoding this field is a JSON number
                    // with the NamespaceIndex. The field is omitted if the
                    // NamespaceIndex is 0.
                    field = new(BuiltInSchema(BuiltInType.UInt32), "Uri", 1);
                }
                else
                {
                    // For non-reversible encoding this field is the JSON
                    // string containing the NamespaceUri associated with
                    // the NamespaceIndex unless the NamespaceIndex is 0.
                    // If the NamespaceIndex is 0 the field is omitted.
                    field = new(BuiltInSchema(BuiltInType.String), "Uri", 1);
                }
                return RecordSchema.Create(nameof(BuiltInType.QualifiedName),
                    new List<Field>
                    {
                        new (BuiltInSchema(BuiltInType.String), "Name", 0),
                        field
                    }, kNamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.QualifiedName) });
            }
        }

        internal Schema LocalizedTextSchema
        {
            get
            {
                if (_reversibleEncoding)
                {
                    return RecordSchema.Create(nameof(BuiltInType.LocalizedText),
                        new List<Field>
                        {
                            new (BuiltInSchema(BuiltInType.String), "Locale", 0),
                            new (BuiltInSchema(BuiltInType.String), "Text", 1)
                        }, kNamespaceZeroName,
                        new[] { GetDataTypeId(BuiltInType.LocalizedText) });
                }

                // For the non-reversible form, LocalizedText value shall
                // be encoded as a JSON string containing the Text component.
                return CreateDerivedSchema(nameof(BuiltInType.LocalizedText),
                    BuiltInSchema(BuiltInType.String), kNamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.LocalizedText) });
            }
        }

        internal Schema NodeIdSchema
        {
            get
            {
                var idType = UnionSchema.Create(new List<Schema>
                {
                    BuiltInSchema(BuiltInType.UInt32),
                    BuiltInSchema(BuiltInType.String),
                    BuiltInSchema(BuiltInType.Guid),
                    BuiltInSchema(BuiltInType.ByteString)
                });
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
                    field = new(BuiltInSchema(BuiltInType.UInt32), "Namespace", 2);
                }
                else 
                {
                    // For non-reversible encoding this field is the JSON
                    // string containing the NamespaceUri associated with
                    // the NamespaceIndex unless the NamespaceIndex is 0.
                    // If the NamespaceIndex is 0 the field is omitted.
                    field = new(BuiltInSchema(BuiltInType.String), "Namespace", 2);
                }
                return RecordSchema.Create(nameof(BuiltInType.NodeId),
                    new List<Field>
                    {
                        new (idTypeType, "IdType", 0),
                        new (idType, "Id", 1),
                        field
                    }, kNamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.NodeId) });
            }
        }

        internal Schema ExpandedNodeIdSchema
        {
            get
            {
                var idType = UnionSchema.Create(new List<Schema>
                {
                    BuiltInSchema(BuiltInType.UInt32),
                    BuiltInSchema(BuiltInType.String),
                    BuiltInSchema(BuiltInType.Guid),
                    BuiltInSchema(BuiltInType.ByteString)
                });
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
                    field = new(BuiltInSchema(BuiltInType.UInt16), "ServerUri", 3);
                }
                else
                {
                    // For non-reversible encoding this field is the JSON string
                    // containing the ServerUri associated with the ServerIndex
                    // unless the ServerIndex is 0. If the ServerIndex is 0 the
                    // field is omitted.
                    field = new(BuiltInSchema(BuiltInType.String), "ServerUri", 3);
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
                        new(UnionSchema.Create(new List<Schema>
                        {
                            BuiltInSchema(BuiltInType.UInt32),
                            BuiltInSchema(BuiltInType.String)
                        }), "Namespace", 2),
                        field
                    }, kNamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.ExpandedNodeId) });
            }
        }

        internal Schema DataValueSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.DataValue),
                    new List<Field>
                    {
                        new (BuiltInSchema(BuiltInType.Variant), "Value", 0),
                        new (BuiltInSchema(BuiltInType.StatusCode), "Status", 1),
                        new (BuiltInSchema(BuiltInType.DateTime), "SourceTimestamp", 2),
                        new (BuiltInSchema(BuiltInType.UInt16), "SourcePicoSeconds", 3),
                        new (BuiltInSchema(BuiltInType.DateTime), "ServerTimestamp", 4),
                        new (BuiltInSchema(BuiltInType.UInt16), "ServerPicoSeconds", 5)
                    }, kNamespaceZeroName,
                    new[] { GetDataTypeId(BuiltInType.DataValue) });
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
        /// Create namespace
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private (string, string) SplitNodeId(string nodeId)
        {
            var id = nodeId.ToExpandedNodeId(_context);
            string avroStyleNamespace;
            if (id.NamespaceIndex == 0)
            {
                avroStyleNamespace = kNamespaceZeroName;
            }
            else
            {
                var ns = id.NamespaceUri;
                if (!Uri.TryCreate(ns, new UriCreationOptions
                {
                    DangerousDisablePathAndQueryCanonicalization = false
                }, out var result))
                {
                    avroStyleNamespace = ns.Split('/')
                        .Select(Escape)
                        .Aggregate((a, b) => $"{a}.{b}");
                }
                else
                {
                    avroStyleNamespace = result.Host.Split('.').Reverse()
                        .Concat(result.AbsolutePath.Split('/'))
                        .Select(Escape)
                        .Aggregate((a, b) => $"{a}.{b}");
                }
            }
            var name = id.IdType switch
            {
                IdType.Opaque => "b_",
                IdType.Guid => "g_",
                IdType.String => "s_",
                _ => "i_"
            } + id.Identifier;
            return (avroStyleNamespace, Escape(name));
        }

        /// <summary>
        /// Get a avro compliant name string
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string Escape(string name)
        {
            return Escape(name, false);
        }

        /// <summary>
        /// Get a avro compliant name string
        /// </summary>
        /// <param name="name"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        public static string Escape(string name, bool remove)
        {
            return EscapeAvroRegex().Replace(name.Replace('/', '_'),
                match => remove ? string.Empty : $"__{(int)match.Value[0]}");
        }

        /// <summary>
        /// Derived schema
        /// </summary>
        internal class DerivedSchema : NamedSchema
        {
            /// <inheritdoc/>
            public DerivedSchema(Type type, SchemaName name,
                IList<string>? aliases = null, PropertyMap? props = null,
                SchemaNames? names = null, string? doc = null)
                : base(type, name, GetSchemaNames(aliases, name),
                      props, names ?? new SchemaNames(), doc)
            {
            }

            internal static IList<SchemaName>? GetSchemaNames(
                IEnumerable<string>? aliases, SchemaName typeName)
            {
                if (aliases == null)
                {
                    return null;
                }
                return aliases.Select(alias => new SchemaName(
                    alias, typeName.Namespace, null, null)).ToList();
            }
        }

        /// <summary>
        /// Create nullable
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        internal static Schema Nullable(Schema schema)
        {
            return UnionSchema.Create(new List<Schema>
            {
                Null,
                schema
            });
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
            return CreateDerivedSchema(name, baseType, kNamespaceZeroName,
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
            return CreateDerivedSchema(name, baseType, kNamespaceZeroName,
                new[] { GetDataTypeId((BuiltInType)builtInType) });
        }

        /// <summary>
        /// Create derived schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="baseSchema"></param>
        /// <param name="ns"></param>
        /// <param name="aliases"></param>
        /// <returns></returns>
        internal static DerivedSchema CreateDerivedSchema(string name,
            Schema baseSchema, string ns, string[] aliases)
        {
            return new DerivedSchema(baseSchema.Tag, 
                new SchemaName(name, ns, null, null), aliases);
        }

        /// <summary>
        /// Get object as extension object encoding
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="bodyType"></param>
        /// <returns></returns>
        internal Schema GetExtensionObjectSchema(string name, string ns,
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
                    new (BuiltInSchema(BuiltInType.NodeId), "TypeId", 0),
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
        internal Schema GetDataValueFieldSchema(string name, Schema valueSchema)
        {
            return RecordSchema.Create(name + nameof(BuiltInType.DataValue), 
                new List<Field>
                {
                    new (valueSchema, "Value", 0),
                    new (BuiltInSchema(BuiltInType.StatusCode), "Status", 1),
                    new (BuiltInSchema(BuiltInType.DateTime), "SourceTimestamp", 2),
                    new (BuiltInSchema(BuiltInType.UInt16), "SourcePicoSeconds", 3),
                    new (BuiltInSchema(BuiltInType.DateTime), "ServerTimestamp", 4),
                    new (BuiltInSchema(BuiltInType.UInt16), "ServerPicoSeconds", 5)
                });
        }

        /// <summary>a
        /// Get a type in variant encoding form
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="bodyType"></param>
        /// <returns></returns>
        internal Schema GetVariantField(string name, string ns, string dataTypeId, 
            Schema bodyType)
        {
            var variantSchema = RecordSchema.Create(name + nameof(BuiltInType.Variant),
                new List<Field>
                {
                    new (BuiltInSchema(BuiltInType.UInt16), "Type", 0),
                    new (bodyType, "Body", 1),
                    new (Nullable(ArraySchema.Create(
                        BuiltInSchema(BuiltInType.Int32))), "Dimensions", 2)
                }, ns);
            var variant = UnionSchema.Create(new List<Schema>
            {
                Null,
                variantSchema,
                bodyType
            });
            return new DerivedSchema(variantSchema.Tag, new SchemaName(Escape(name), 
                ns, null, null), new[] { dataTypeId });
        }

        /// <summary>
        /// Null schema
        /// </summary>
        private static Schema Null { get; } = PrimitiveSchema.NewInstance("null");








        /// <summary>
        /// Avro type
        /// </summary>
        private abstract record class TypedDescription
        {
            /// <summary>
            /// Resolved schema of the type
            /// </summary>
            public Schema? Schema { get; set; }

            /// <summary>
            /// Resolve the type
            /// </summary>
            /// <param name="schema"></param>
            public abstract void Resolve(AvroDataSetSchema schema);
        }

        /// <summary>
        /// Simple type
        /// </summary>
        /// <param name="Description"></param>
        private record class SimpleType(SimpleTypeDescriptionModel Description)
            : TypedDescription
        {
            /// <inheritdoc/>
            public override void Resolve(AvroDataSetSchema schemas)
            {
                if (Schema != null)
                {
                    return;
                }

                var (ns, dt) = schemas.SplitNodeId(Description.DataTypeId);

                if (Description.DataTypeId == "i=" + Description.BuiltInType)
                {
                    // Emit the built in type definition here instead
                    Debug.Assert(Description.BuiltInType.HasValue);
                    Schema = schemas.BuiltInSchema(
                        (BuiltInType)Description.BuiltInType.Value);
                }
                else
                {
                    // Derive from base type or built in type
                    var baseSchema = Description.BaseDataType != null ?
                        schemas.LookupSchema(Description.BaseDataType, false) :
                        schemas.BuiltInSchema((BuiltInType)
                            (Description.BuiltInType ?? (byte?)BuiltInType.String));
                    Schema = CreateDerivedSchema(Escape(Description.Name),
                        baseSchema, ns, new[] { dt });
                }
            }
        }

        /// <summary>
        /// Record
        /// </summary>
        /// <param name="Description"></param>
        private record class StructureType(StructureDescriptionModel Description)
            : TypedDescription
        {
            /// <inheritdoc/>
            public override void Resolve(AvroDataSetSchema schemas)
            {
                if (Schema != null)
                {
                    return;
                }

                //
                // |---------------|------------|----------------|
                // | Field Value   | Reversible | Non-Reversible |
                // |---------------|------------|----------------|
                // | NULL          | Omitted    | JSON null      |
                // | Default Value | Omitted    | Default Value  |
                // |---------------|------------|----------------|
                //
                var fields = new List<Field>();
                for (var i = 0; i < Description.Fields.Count; i++)
                {
                    var field = Description.Fields[i];
                    var schema = schemas.LookupSchema(field.DataType, true,
                        field.ValueRank, field.ArrayDimensions);
                    fields.Add(new Field(schema, Escape(field.Name), i));
                }
                var (ns, dt) = schemas.SplitNodeId(Description.DataTypeId);
                Schema = RecordSchema.Create(Escape(Description.Name),
                    fields, ns, new[] { dt });
            }
        }

        /// <summary>
        /// Enum type
        /// </summary>
        /// <param name="Description"></param>
        private record class EnumType(EnumDescriptionModel Description)
            : TypedDescription
        {
            /// <inheritdoc/>
            public override void Resolve(AvroDataSetSchema schemas)
            {
                if (Schema != null)
                {
                    return;
                }

                var (ns, dt) = schemas.SplitNodeId(Description.DataTypeId);

                if (Description.IsOptionSet)
                {
                    // Flags
                    // ...
                }

                var symbols = Description.Fields.Select(e => Escape(e.Name)).ToList();
                Schema = EnumSchema.Create(Escape(Description.Name), symbols, ns,
                    new[] { dt }, defaultSymbol: symbols[0]);
                // TODO: Build doc from fields descriptions
            }
        }

        /// <summary>
        /// Get encoding modes
        /// </summary>
        /// <param name="reversibleEncoding"></param>
        /// <param name="uriEncoding"></param>
        /// <param name="writeSingleValue"></param>
        /// <param name="dataValueRepresentation"></param>
        /// <param name="isSingleFieldDataSet"></param>
        /// <param name="fieldContentMask"></param>
        private static void GetEncodingMode(out bool reversibleEncoding, 
            out bool uriEncoding, out bool writeSingleValue, 
            out bool dataValueRepresentation, bool isSingleFieldDataSet, 
            uint fieldContentMask)
        {
            writeSingleValue = isSingleFieldDataSet &&
               ((fieldContentMask &
                (uint)DataSetFieldContentMaskEx.SingleFieldDegradeToValue) != 0);
            if ((fieldContentMask &
                (uint)Publisher.Models.DataSetFieldContentMask.RawData) != 0)
            {
                //
                // If the DataSetFieldContentMask results in a RawData
                // representation, the field value is a Variant encoded
                // using the non-reversible OPC UA JSON Data Encoding
                // defined in OPC 10000-6
                //
                uriEncoding = true;
                reversibleEncoding = false;
                dataValueRepresentation = false;
            }
            else if (fieldContentMask == 0)
            {
                //
                // If the DataSetFieldContentMask results in a Variant
                // representation, the field value is encoded as a Variant
                // encoded using the reversible OPC UA JSON Data Encoding
                // defined in OPC 10000-6.
                //
                uriEncoding = false;
                reversibleEncoding = true;
                dataValueRepresentation = false;
            }
            else
            {
                //
                // If the DataSetFieldContentMask results in a DataValue
                // representation, the field value is a DataValue encoded
                // using the non-reversible OPC UA JSON Data Encoding or
                // reversible depending on encoder configuration.
                //
                dataValueRepresentation = true;
                reversibleEncoding = false;
                uriEncoding = false;
            }
        }

        private const string kNamespaceZeroName = "org.opcfoundation.ua";
        private readonly Dictionary<BuiltInType, Schema> _builtIn = new();
        private readonly Dictionary<string, TypedDescription> _types = new();
        private readonly IServiceMessageContext _context;
        private readonly MessageEncoding _encoding;
        private readonly bool _reversibleEncoding;
        private readonly bool _useUriEncoding;
        private readonly bool _omitFieldName;
        private readonly bool _fieldsAreDataValues;

        [GeneratedRegex("[^a-zA-Z0-9_]")]
        private static partial Regex EscapeAvroRegex();
    }
}
