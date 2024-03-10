// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Furly.Extensions.Messaging;
    using Furly;

    /// <summary>
    /// Extensions to convert metadata into avro schema. Note that this class
    /// generates a schema that complies with the json representation in
    /// <see cref="JsonEncoderEx.WriteDataSet(string?, Models.DataSet?)"/> This
    /// depends on the network settings and reversible vs. nonreversible
    /// encoding mode.
    /// </summary>
    public partial class AvroSchema : IEventSchema
    {
        /// <summary>
        /// The actual schema
        /// </summary>
        public Schema Schema { get; }

        /// <inheritdoc/>
        public string Type => ContentMimeType.AvroSchema;

        /// <inheritdoc/>
        public string Name => Schema.Fullname;

        /// <inheritdoc/>
        public ulong Version { get; }

        /// <inheritdoc/>
        string IEventSchema.Schema => Schema.ToString();

        /// <inheritdoc/>
        public string? Id { get; }

        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        public AvroSchema(WriterGroupModel writerGroup,
            NamespaceTable? namespaces = null)
            : this(writerGroup.DataSetWriters!, namespaces,
                  writerGroup.MessageType ?? MessageEncoding.Json,
                  writerGroup.MessageSettings?.NetworkMessageContentMask)
        {
        }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="namespaces"></param>
        /// <param name="encoding"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <returns></returns>
        public AvroSchema(DataSetWriterModel dataSetWriter,
            NamespaceTable? namespaces = null, MessageEncoding? encoding = null,
            NetworkMessageContentMask? networkMessageContentMask = null)
            : this(dataSetWriter.YieldReturn(), namespaces, encoding,
                  networkMessageContentMask)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="context"></param>
        /// <param name="encoding"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="dataSetContentMask"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <returns></returns>
        public AvroSchema(PublishedDataSetModel dataSet,
            NamespaceTable? context = null, MessageEncoding? encoding = null,
            NetworkMessageContentMask? networkMessageContentMask = null,
            DataSetContentMask? dataSetContentMask = null,
            Publisher.Models.DataSetFieldContentMask? dataSetFieldContentMask = null)
            : this(new DataSetWriterModel
            {
                Id = null!,
                DataSet = dataSet,
                DataSetFieldContentMask = dataSetFieldContentMask,
                MessageSettings = new DataSetWriterMessageSettingsModel
                {
                    DataSetMessageContentMask = dataSetContentMask
                }
            }.YieldReturn(), context, encoding, networkMessageContentMask)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="context"></param>
        /// <param name="encoding"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <returns></returns>
        internal AvroSchema(IEnumerable<DataSetWriterModel> dataSetWriters,
            NamespaceTable? context, MessageEncoding? encoding,
            NetworkMessageContentMask? networkMessageContentMask)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriters);

            _context = new ServiceMessageContext
            {
                NamespaceUris = context ?? new NamespaceTable()
            };
            Schema = Compile(dataSetWriters
                .Where(d => d.DataSet != null)
                .ToList(), networkMessageContentMask,
                encoding ?? MessageEncoding.Json);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToString();
        }

        /// <summary>
        /// Compile the schema for the data sets
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private Schema Compile(List<DataSetWriterModel> dataSetWriters,
            NetworkMessageContentMask? networkMessageContentMask,
            MessageEncoding encoding)
        {
            var networkMessageMask = networkMessageContentMask.GetValueOrDefault(0);
            if (networkMessageContentMask != 0 || dataSetWriters.Count <= 1)
            {
                // Compile a schema for the entire writer group
                return CompileSchema(dataSetWriters,
                    networkMessageMask, encoding);
            }

            // No network message so writer writes data set messages without header
            // Get schema per writer
            return UnionSchema.Create(dataSetWriters
                .ConvertAll(writer => CompileSchema(
                    new List<DataSetWriterModel>() { writer },
                    networkMessageMask, encoding)));
        }

        private Schema CompileSchema(List<DataSetWriterModel> dataSetWriters,
            NetworkMessageContentMask networkMessageContentMask, MessageEncoding encoding)
        {
            // Collect types
            CollectTypes(dataSetWriters);

            // Compile collected types to schemas
            foreach (var type in _types.Values)
            {
                if (type.Schema == null)
                {
                    type.Resolve(this);
                }
            }

            var schemas = GetDataSetSchemas(dataSetWriters,
                networkMessageContentMask, encoding).ToList();
            if (schemas.Count != 1)
            {
                return UnionSchema.Create(schemas);
            }
            return schemas[0];
        }

        /// <summary>
        /// Create data set schemas
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private IEnumerable<Schema> GetDataSetSchemas(
            IEnumerable<DataSetWriterModel> dataSetWriters,
            NetworkMessageContentMask networkMessageContentMask,
            MessageEncoding encoding)
        {
            foreach (var dataSetWriter in dataSetWriters)
            {
                var dataSet = dataSetWriter.DataSet;
                Debug.Assert(dataSet != null);

                GetEncodingMode(out var reversible, out var uriEncoding, out var single,
                    out var dataValues, dataSet.EnumerateMetaData().Take(2).Count() != 1,
                    (uint)(dataSetWriter.DataSetFieldContentMask ?? 0u));

                var fields = new List<Field>();
                var pos = 0;
                // Now collect the fields of the payload
                foreach (var (fieldName, fieldMetadata) in dataSet.EnumerateMetaData())
                {
                    pos++;
                    if (fieldMetadata?.DataType != null && fieldName != null)
                    {
                        // TODO: we want to take the data set and
                        // network message format into account here
                        // in particular the DataValue definition

                        var schema = fieldMetadata?.DataType == null ? null
                            : GetSchema(fieldMetadata.DataType);
                        fields.Add(new Field(schema, Escape(fieldName), pos));
                    }
                }
                yield return RecordSchema.Create(
                    Escape(dataSet.Name ?? "Payload"), fields);
            }
        }

        /// <summary>
        /// Collect types from data set
        /// </summary>
        /// <param name="dataSetWriters"></param>
        private void CollectTypes(IEnumerable<DataSetWriterModel> dataSetWriters)
        {
            foreach (var (_, fieldMetadata) in dataSetWriters
                .SelectMany(d => d.DataSet!.EnumerateMetaData())
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
        /// <param name="valueRank"></param>
        /// <param name="arrayDimensions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private Schema GetSchema(string dataType, int valueRank = 1,
            IReadOnlyList<uint>? arrayDimensions = null)
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
            }
            schema ??= GetBuiltInDataTypeSchema(dataType);
            if (schema != null)
            {
                if (arrayDimensions != null)
                {
                    valueRank = arrayDimensions.Count;
                }

                // TODO: we also have matrices, we should have schemas for all
                return valueRank > 1 ? ArraySchema.Create(schema) : schema;
            }
            throw new ArgumentException($"No Schema found for {dataType}");

            Schema? GetBuiltInDataTypeSchema(string dataType)
            {
                if (int.TryParse(dataType[2..], out var id)
                    && id >= 0 && id <= 29)
                {
                    return GetBuiltInSchema((BuiltInType)id);
                }
                return null;
            }
        }

        internal Schema DiagnosticInfoSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.DiagnosticInfo), new List<Field>
                {
                    new (GetBuiltInSchema(BuiltInType.Int32), "SymbolicId", 0),
                    new (GetBuiltInSchema(BuiltInType.Int32), "NamespaceUri", 1),
                    new (GetBuiltInSchema(BuiltInType.Int32), "Locale", 2),
                    new (GetBuiltInSchema(BuiltInType.Int32), "LocalizedText", 3),
                    new (GetBuiltInSchema(BuiltInType.String), "AdditionalInfo", 4),
                    new (GetBuiltInSchema(BuiltInType.StatusCode), "InnerStatusCode", 5),
                    new (GetBuiltInSchema(BuiltInType.DiagnosticInfo), "InnerDiagnosticInfo", 6)
                }, kNamespaceZeroName, new[] { GetDataTypeId(BuiltInType.DiagnosticInfo) });
            }
        }

        internal Schema ExtensionObjectSchema
        {
            get
            {
                var bodyType = UnionSchema.Create(new List<Schema>
                {
                    GetBuiltInSchema(BuiltInType.Null),
                    GetBuiltInSchema(BuiltInType.String),
                    GetBuiltInSchema(BuiltInType.XmlElement),
                    GetBuiltInSchema(BuiltInType.ByteString)
                });
                var encodingType = EnumSchema.Create("Encoding", new string[]
                {
                    "Structure",
                    "ByteString",
                    "XmlElement"
                });
                return RecordSchema.Create(nameof(BuiltInType.ExtensionObject), new List<Field>
                {
                    new (GetBuiltInSchema(BuiltInType.NodeId), "TypeId", 0),
                    new (encodingType, "Encoding", 1),
                    new (bodyType, "Body", 2)
                }, kNamespaceZeroName, new[] { GetDataTypeId(BuiltInType.ExtensionObject) });
            }
        }

        internal Schema StatusCodeSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.StatusCode), new List<Field>
                {
                    new (GetBuiltInSchema(BuiltInType.UInt32), "Code", 0),
                    new (GetBuiltInSchema(BuiltInType.String), "Symbol", 1)
                }, kNamespaceZeroName, new[] { GetDataTypeId(BuiltInType.StatusCode) });
            }
        }

        internal Schema QualifiedNameSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.QualifiedName), new List<Field>
                {
                    new (GetBuiltInSchema(BuiltInType.String), "Name", 0),
                    new (GetBuiltInSchema(BuiltInType.UInt32), "Uri", 1)
                }, kNamespaceZeroName, new[] { GetDataTypeId(BuiltInType.QualifiedName) });
            }
        }

        internal Schema LocalizedTextSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.LocalizedText), new List<Field>
                {
                    new (GetBuiltInSchema(BuiltInType.String), "Locale", 0),
                    new (GetBuiltInSchema(BuiltInType.String), "Text", 1)
                }, kNamespaceZeroName, new[] { GetDataTypeId(BuiltInType.LocalizedText) });
            }
        }

        internal Schema NodeIdSchema
        {
            get
            {
                var idType = UnionSchema.Create(new List<Schema>
                {
                    GetBuiltInSchema(BuiltInType.UInt32),
                    GetBuiltInSchema(BuiltInType.String),
                    GetBuiltInSchema(BuiltInType.Guid),
                    GetBuiltInSchema(BuiltInType.ByteString)
                });
                var idTypeType = EnumSchema.Create("IdentifierType", new string[]
                {
                    "UInt32",
                    "String",
                    "Guid",
                    "ByteString"
                });
                return RecordSchema.Create(nameof(BuiltInType.NodeId), new List<Field>
                {
                    new (idTypeType, "IdType", 0),
                    new (idType, "Id", 1),
                    new (GetBuiltInSchema(BuiltInType.UInt32), "Namespace", 2)
                }, kNamespaceZeroName, new[] { GetDataTypeId(BuiltInType.NodeId) });
            }
        }

        internal Schema ExpandedNodeIdSchema
        {
            get
            {
                var idType = UnionSchema.Create(new List<Schema>
                {
                    GetBuiltInSchema(BuiltInType.UInt32),
                    GetBuiltInSchema(BuiltInType.String),
                    GetBuiltInSchema(BuiltInType.Guid),
                    GetBuiltInSchema(BuiltInType.ByteString)
                });
                var idTypeType = EnumSchema.Create("IdentifierType", new string[]
                {
                    "UInt32",
                    "String",
                    "Guid",
                    "ByteString"
                });
                return RecordSchema.Create(nameof(BuiltInType.ExpandedNodeId), new List<Field>
                {
                    new (idTypeType, "IdType", 0),
                    new (idType, "Id", 1),
                    new (GetBuiltInSchema(BuiltInType.UInt32), "Namespace", 2),
                    new (GetBuiltInSchema(BuiltInType.UInt16), "ServerUri", 3)
                }, kNamespaceZeroName, new[] { GetDataTypeId(BuiltInType.ExpandedNodeId) });
            }
        }

        internal Schema DataValueSchema
        {
            get
            {
                return RecordSchema.Create(nameof(BuiltInType.DataValue), new List<Field>
                {
                    new (GetBuiltInSchema(BuiltInType.Variant), "Value", 0),
                    new (GetBuiltInSchema(BuiltInType.StatusCode), "Status", 1),
                    new (GetBuiltInSchema(BuiltInType.DateTime), "SourceTimestamp", 2),
                    new (GetBuiltInSchema(BuiltInType.UInt16), "SourcePicoSeconds", 3),
                    new (GetBuiltInSchema(BuiltInType.DateTime), "ServerTimestamp", 4),
                    new (GetBuiltInSchema(BuiltInType.UInt16), "ServerPicoSeconds", 5)
                }, kNamespaceZeroName, new[] { GetDataTypeId(BuiltInType.DataValue) });
            }
        }

        internal Schema VariantSchema
        {
            get
            {
                var bodyType = UnionSchema.Create(
                    GetPossibleTypes(false).Concat(GetPossibleTypes(true)).ToList());
                IEnumerable<Schema> GetPossibleTypes(bool array)
                {
                    if (array)
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
                        var schema = GetBuiltInSchema((BuiltInType)i);
                        yield return array ? ArraySchema.Create(schema) : schema;
                    }
                }
                return RecordSchema.Create(nameof(BuiltInType.Variant), new List<Field>
                {
                    new (GetBuiltInSchema(BuiltInType.UInt16), "Type", 0),
                    new (bodyType, "Body", 1),
                    new (ArraySchema.Create(GetBuiltInSchema(BuiltInType.Int32)),
                        "Dimensions", 2)
                }, kNamespaceZeroName, new[] { GetDataTypeId(BuiltInType.Variant) });
            }
        }

        /// <summary>
        /// Get data typeid
        /// </summary>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        internal static string GetDataTypeId(BuiltInType builtInType)
        {
            return "i_" + (int)builtInType;
        }

        /// <summary>
        /// Get built in schema
        /// </summary>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal Schema GetBuiltInSchema(BuiltInType builtInType)
        {
            // https://reference.opcfoundation.org/Core/Part6/v104/docs/5.1.2#_Ref131507956
            if (!_builtIn.TryGetValue(builtInType, out var schema))
            {
                schema = Get((int)builtInType);
                _builtIn.Add(builtInType, schema);
            }
            return schema;

            Schema Get(int id) => id switch
            {
                0 => PrimitiveSchema.NewInstance("null"),

                1 => DerivedSchema.CreatePrimitive(id, "Boolean", "boolean"),
                2 => DerivedSchema.CreatePrimitive(id, "SByte", "int"),
                3 => DerivedSchema.CreatePrimitive(id, "Byte", "int"),
                4 => DerivedSchema.CreatePrimitive(id, "Int16", "int"),
                5 => DerivedSchema.CreatePrimitive(id, "UInt16", "int"),
                6 => DerivedSchema.CreatePrimitive(id, "Int32", "int"),
                7 => DerivedSchema.CreatePrimitive(id, "UInt32", "int"),

                // As per part 6 encoding, long is encoded as string
                8 => DerivedSchema.CreatePrimitive(id, "Int64", "string"),
                9 => DerivedSchema.CreatePrimitive(id, "UInt64", "string"),

                10 => DerivedSchema.CreatePrimitive(id, "Float", "float"),
                11 => DerivedSchema.CreatePrimitive(id, "Double", "double"),
                12 => DerivedSchema.CreatePrimitive(id, "String", "string"),
                13 => DerivedSchema.CreatePrimitive(id, "DateTime", "string"), // TODO ISO type?
                14 => DerivedSchema.CreateLogical(id, "Guid", "string", "uuid"),
                15 => DerivedSchema.CreatePrimitive(id, "ByteString", "bytes"),
                16 => DerivedSchema.CreatePrimitive(id, "XmlElement", "string"),

                17 => NodeIdSchema, // "NodeId",
                18 => ExpandedNodeIdSchema, // "ExpandedNodeId",
                19 => StatusCodeSchema, // "StatusCode",
                20 => QualifiedNameSchema, // "QualifiedName",
                21 => LocalizedTextSchema, // "LocalizedText",
                22 => ExtensionObjectSchema, // "ExtensionObject",
                23 => DataValueSchema, // "DataValue",
                24 => VariantSchema, // "Variant",
                25 => DiagnosticInfoSchema, // "DiagnosticInfo",

                26 => DerivedSchema.CreatePrimitive(id, "Number", "string"),

                // Should this be string? As per json encoding, long is string
                27 => DerivedSchema.CreatePrimitive(id, "Integer", "string"),
                28 => DerivedSchema.CreatePrimitive(id, "UInteger", "string"),

                29 => DerivedSchema.CreatePrimitive(id, "Enumeration", "int"),
                _ => throw new ArgumentException($"Built in type {id} unknown")
            };
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
            return EscapeAvroRegex().Replace(name.Replace('/', '_'), match => remove ? string.Empty : $"__{(int)match.Value[0]}");
        }

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
            public abstract void Resolve(AvroSchema schema);
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

            /// <summary>
            /// Create logical opc ua derived type
            /// </summary>
            /// <param name="builtInType"></param>
            /// <param name="name"></param>
            /// <param name="type"></param>
            /// <param name="logicalType"></param>
            /// <returns></returns>
            internal static Schema CreateLogical(int builtInType, string name,
                string type, string logicalType)
            {
                var baseType = Parse(
                    $$"""{"type": "{{type}}", "logicalType": "{{logicalType}}"}""");
                return new DerivedSchema(baseType.Tag,
                    new SchemaName(name, kNamespaceZeroName, null, null),
                    new[] { GetDataTypeId((BuiltInType)builtInType) });
            }

            /// <summary>
            /// Create primitive opc ua built in type
            /// </summary>
            /// <param name="builtInType"></param>
            /// <param name="name"></param>
            /// <param name="type"></param>
            /// <returns></returns>
            internal static Schema CreatePrimitive(int builtInType,
                string name, string type)
            {
                var baseType = PrimitiveSchema.NewInstance(type);
                return new DerivedSchema(baseType.Tag,
                    new SchemaName(name, kNamespaceZeroName, null, null),
                    new[] { GetDataTypeId((BuiltInType)builtInType) });
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
        /// Simple type
        /// </summary>
        /// <param name="Description"></param>
        private record class SimpleType(SimpleTypeDescriptionModel Description)
            : TypedDescription
        {
            /// <inheritdoc/>
            public override void Resolve(AvroSchema schemas)
            {
                if (Schema != null)
                {
                    return;
                }

                var (ns, dt) = schemas.SplitNodeId(Description.DataTypeId);
                if (Description.BaseDataType != null)
                {
                    // Derive from base schema
                    var baseSchema = schemas.GetSchema(Description.BaseDataType);
                    Schema = new DerivedSchema(baseSchema.Tag,
                        new SchemaName(Escape(Description.Name), ns, null, null),
                        new[] { dt });
                    return;
                }

                // This is a built in type?
                if (Description.DataTypeId == "i=" + Description.BuiltInType)
                {
                    // Emit the built in type definition here instead
                    Debug.Assert(Description.BuiltInType.HasValue);
                    var builtIn = schemas.GetBuiltInSchema(
                        (BuiltInType)Description.BuiltInType.Value);
                    if (builtIn != null)
                    {
                        Schema = builtIn;
                    }
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
            public override void Resolve(AvroSchema schemas)
            {
                if (Schema != null)
                {
                    return;
                }
                var fields = new List<Field>();
                for (var i = 0; i < Description.Fields.Count; i++)
                {
                    var field = Description.Fields[i];
                    var schema = schemas.GetSchema(field.DataType,
                        field.ValueRank, field.ArrayDimensions);
                    fields.Add(new Field(schema, Escape(field.Name), i));
                }
                var (ns, dt) = schemas.SplitNodeId(Description.DataTypeId);
                Schema = RecordSchema.Create(Escape(Description.Name), fields, ns,
                    new[] { dt });
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
            public override void Resolve(AvroSchema schemas)
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
        /// <param name="reversible"></param>
        /// <param name="uriEncoding"></param>
        /// <param name="writeSingleValue"></param>
        /// <param name="dataValueRepresentation"></param>
        /// <param name="isSingleFieldDataSet"></param>
        /// <param name="fieldContentMask"></param>
        private static void GetEncodingMode(out bool reversible, out bool uriEncoding,
            out bool writeSingleValue, out bool dataValueRepresentation,
            bool isSingleFieldDataSet, uint fieldContentMask)
        {
            writeSingleValue = isSingleFieldDataSet &&
               ((fieldContentMask &
                (uint)DataSetFieldContentMaskEx.SingleFieldDegradeToValue) != 0);
            if ((fieldContentMask &
                (uint)Publisher.Models.DataSetFieldContentMask.RawData) != 0)
            {
                //
                // If the DataSetFieldContentMask results in a RawData representation,
                // the field value is a Variant encoded using the non-reversible OPC UA
                // JSON Data Encoding defined in OPC 10000-6
                //
                uriEncoding = true;
                reversible = false;
                dataValueRepresentation = false;
            }
            else if (fieldContentMask == 0)
            {
                //
                // If the DataSetFieldContentMask results in a Variant representation,
                // the field value is encoded as a Variant encoded using the reversible
                // OPC UA JSON Data Encoding defined in OPC 10000-6.
                //
                uriEncoding = false;
                reversible = true;
                dataValueRepresentation = false;
            }
            else
            {
                //
                // If the DataSetFieldContentMask results in a DataValue representation,
                // the field value is a DataValue encoded using the non-reversible OPC UA
                // JSON Data Encoding or reversible depending on encoder configuration.
                //
                dataValueRepresentation = true;
                reversible = false;
                uriEncoding = false;
            }
        }

        private const string kNamespaceZeroName = "org.opcfoundation.ua";
        private readonly Dictionary<BuiltInType, Schema> _builtIn = new();
        private readonly Dictionary<string, TypedDescription> _types = new();
        private readonly IServiceMessageContext _context;

        [GeneratedRegex("[^a-zA-Z0-9_]")]
        private static partial Regex EscapeAvroRegex();
    }
}
