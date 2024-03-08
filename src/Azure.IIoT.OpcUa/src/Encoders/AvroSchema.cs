// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Opc.Ua;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Extensions to convert metadata into avro schema. Note that this class
    /// generates a schema that complies with the json representation in
    /// <see cref="JsonEncoderEx.WriteDataSet(string?, Models.DataSet?)"/> This
    /// depends on the network settings and reversible vs. nonreversible
    /// encoding mode.
    /// </summary>
    public class AvroSchema
    {
        /// <summary>
        /// The actual schema
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public AvroSchema(DataSetWriterModel dataSetWriter,
            IServiceMessageContext context)
            : this(dataSetWriter.DataSet!, context,
                  dataSetWriter.DataSetFieldContentMask)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="context"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <returns></returns>
        public AvroSchema(PublishedDataSetModel dataSet,
            IServiceMessageContext context,
            Publisher.Models.DataSetFieldContentMask? dataSetFieldContentMask)
        {
            ArgumentNullException.ThrowIfNull(dataSet);
            _context = context;

            // Get the encoding mode to apply when generating the schema
            dataSetFieldContentMask ??=
                Publisher.Models.DataSetFieldContentMask.RawData;
            GetEncodingModes(out _useReversibleEncoding, out _uriEncoding,
                out _writeSingleValue, out _dataValueRepresentation,
                dataSet.EnumerateMetaData().Take(2).Count() != 1,
                (uint)dataSetFieldContentMask.Value);

            Schema = CompileSchema(dataSet);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToString();
        }

        /// <summary>
        /// Compile the schema
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        private RecordSchema CompileSchema(PublishedDataSetModel dataSet)
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
                    fields.Add(new Field(schema, fieldName, pos));
                }
            }

            return RecordSchema.Create(dataSet.Name ?? "Payload", fields);
        }

        /// <summary>
        /// Collect types from data set
        /// </summary>
        /// <param name="dataSet"></param>
        private void CollectTypes(PublishedDataSetModel dataSet)
        {
            foreach (var (_, fieldMetadata) in dataSet.EnumerateMetaData()
                .Where(m => m.Item2 != null))
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
            schema ??= GetBuiltInSchema(dataType);
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
        }

        /// <summary>
        /// Get built in schema
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        internal static Schema? GetBuiltInSchema(string dataType)
        {
            if (int.TryParse(dataType[2..], out var id))
            {
                return GetBuiltInSchema(id);
            }
            return null;
        }

        internal static Schema DiagnosticInfoSchema
        {
            get
            {
                return RecordSchema.Create("i=" + BuiltInType.DiagnosticInfo, new List<Field>
                {
                    new (GetBuiltInSchema((int)BuiltInType.Int32)!, "SymbolicId", 0),
                    new (GetBuiltInSchema((int)BuiltInType.Int32)!, "NamespaceUri", 1),
                    new (GetBuiltInSchema((int)BuiltInType.Int32)!, "Locale", 2),
                    new (GetBuiltInSchema((int)BuiltInType.Int32)!, "LocalizedText", 3),
                    new (GetBuiltInSchema((int)BuiltInType.String)!, "AdditionalInfo", 4),
                    new (GetBuiltInSchema((int)BuiltInType.StatusCode)!, "InnerStatusCode", 5),
                    new (GetBuiltInSchema((int)BuiltInType.DiagnosticInfo)!, "InnerDiagnosticInfo", 6)
                });
            }
        }

        internal static Schema ExtensionObjectSchema
        {
            get
            {
                var bodyType = UnionSchema.Create(new List<Schema>
                {
                    GetBuiltInSchema((int)BuiltInType.Null)!,
                    GetBuiltInSchema((int)BuiltInType.String)!,
                    GetBuiltInSchema((int)BuiltInType.XmlElement)!,
                    GetBuiltInSchema((int)BuiltInType.ByteString)!
                });
                var encodingType = EnumSchema.Create("Encoding", new string[]
                {
                    "Structure",
                    "ByteString",
                    "XmlElement"
                });
                return RecordSchema.Create("i=" + BuiltInType.ExtensionObject, new List<Field>
                {
                    new (GetBuiltInSchema((int)BuiltInType.NodeId)!, "TypeId", 0),
                    new (encodingType, "Encoding", 1),
                    new (bodyType, "Body", 2)
                });
            }
        }

        internal static Schema StatusCodeSchema
        {
            get
            {
                return RecordSchema.Create("i=" + BuiltInType.StatusCode, new List<Field>
                {
                    new (GetBuiltInSchema((int)BuiltInType.UInt32)!, "Code", 0),
                    new (GetBuiltInSchema((int)BuiltInType.String)!, "Symbol", 1)
                });
            }
        }

        internal static Schema QualifiedNameSchema
        {
            get
            {
                return RecordSchema.Create("i=" + BuiltInType.QualifiedName, new List<Field>
                {
                    new (GetBuiltInSchema((int)BuiltInType.String)!, "Name", 0),
                    new (GetBuiltInSchema((int)BuiltInType.UInt32)!, "Uri", 1)
                });
            }
        }

        internal static Schema LocalizedTextSchema
        {
            get
            {
                return RecordSchema.Create("i=" + BuiltInType.LocalizedText, new List<Field>
                {
                    new (GetBuiltInSchema((int)BuiltInType.String)!, "Locale", 0),
                    new (GetBuiltInSchema((int)BuiltInType.String)!, "Text", 1)
                });
            }
        }

        internal static Schema NodeIdSchema
        {
            get
            {
                var idType = UnionSchema.Create(new List<Schema>
                {
                    GetBuiltInSchema((int)BuiltInType.UInt32)!,
                    GetBuiltInSchema((int)BuiltInType.String)!,
                    GetBuiltInSchema((int)BuiltInType.Guid)!,
                    GetBuiltInSchema((int)BuiltInType.ByteString)!
                });
                var idTypeType = EnumSchema.Create("IdentifierType", new string[]
                {
                    "UInt32",
                    "String",
                    "Guid",
                    "ByteString"
                });
                return RecordSchema.Create("i=" + BuiltInType.NodeId, new List<Field>
                {
                    new (idTypeType, "IdType", 0),
                    new (idType, "Id", 1),
                    new (GetBuiltInSchema((int)BuiltInType.UInt32)!, "Namespace", 2)
                });
            }
        }

        internal static Schema ExpandedNodeIdSchema
        {
            get
            {
                var idType = UnionSchema.Create(new List<Schema>
                {
                    GetBuiltInSchema((int)BuiltInType.UInt32)!,
                    GetBuiltInSchema((int)BuiltInType.String)!,
                    GetBuiltInSchema((int)BuiltInType.Guid)!,
                    GetBuiltInSchema((int)BuiltInType.ByteString)!
                });
                var idTypeType = EnumSchema.Create("IdentifierType", new string[]
                {
                    "UInt32",
                    "String",
                    "Guid",
                    "ByteString"
                });
                return RecordSchema.Create("i=" + BuiltInType.NodeId, new List<Field>
                {
                    new (idTypeType, "IdType", 0),
                    new (idType, "Id", 1),
                    new (GetBuiltInSchema((int)BuiltInType.UInt32)!, "Namespace", 2),
                    new (GetBuiltInSchema((int)BuiltInType.UInt16)!, "ServerUri", 3)
                });
            }
        }

        internal static Schema DataValueSchema
        {
            get
            {
                return RecordSchema.Create("i=" + BuiltInType.DataValue, new List<Field>
                {
                    new (GetBuiltInSchema((int)BuiltInType.Variant)!, "Value", 0),
                    new (GetBuiltInSchema((int)BuiltInType.StatusCode)!, "Status", 1),
                    new (GetBuiltInSchema((int)BuiltInType.DateTime)!, "SourceTimestamp", 2),
                    new (GetBuiltInSchema((int)BuiltInType.UInt16)!, "SourcePicoSeconds", 3),
                    new (GetBuiltInSchema((int)BuiltInType.DateTime)!, "ServerTimestamp", 4),
                    new (GetBuiltInSchema((int)BuiltInType.UInt16)!, "ServerPicoSeconds", 5)
                });
            }
        }

        internal static Schema VariantSchema
        {
            get
            {
                var idType = UnionSchema.Create(new List<Schema>
                {
                    GetBuiltInSchema((int)BuiltInType.UInt32)!,
                    GetBuiltInSchema((int)BuiltInType.String)!,
                    GetBuiltInSchema((int)BuiltInType.Guid)!,
                    GetBuiltInSchema((int)BuiltInType.ByteString)!
                });
                var idTypeType = EnumSchema.Create("IdentifierType", new string[]
                {
                    "UInt32",
                    "String",
                    "Guid",
                    "ByteString"
                });
                return RecordSchema.Create("i=" + BuiltInType.NodeId, new List<Field>
                {
                    new (idTypeType, "IdType", 0),
                    new (idType, "Id", 1),
                    new (GetBuiltInSchema((int)BuiltInType.UInt16)!, "Namespace", 2)
                });
            }
        }

        /// <summary>
        /// Get built in schema
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static Schema? GetBuiltInSchema(int id)
        {
            return id switch
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
                8 => DerivedSchema.CreateLogical(id, "Int64", "string", "long"),
                9 => DerivedSchema.CreateLogical(id, "UInt64", "string", "long"),

                10 => DerivedSchema.CreatePrimitive(id, "Float", "float"),
                11 => DerivedSchema.CreatePrimitive(id, "Double", "double"),
                12 => DerivedSchema.CreatePrimitive(id, "String", "string"),
                13 => DerivedSchema.CreateLogical(id, "DateTime", "string", "datetime"),
                14 => DerivedSchema.CreateLogical(id, "Guid", "string", "uuid"),
                15 => DerivedSchema.CreatePrimitive(id, "ByteString", "bytes"),
                16 => DerivedSchema.CreateLogical(id, "XmlElement", "string", "xml"),

                17 => NodeIdSchema, // "NodeId",
                18 => ExpandedNodeIdSchema, // "ExpandedNodeId",
                19 => StatusCodeSchema, // "StatusCode",
                20 => QualifiedNameSchema, // "QualifiedName",
                21 => LocalizedTextSchema, // "LocalizedText",
                22 => ExtensionObjectSchema, // "ExtensionObject",
                23 => DataValueSchema, // "DataValue",
                24 => VariantSchema, // "Variant",
                25 => DiagnosticInfoSchema, // "DiagnosticInfo",

                26 => DerivedSchema.CreateLogical(id, "Number", "string", "decimal"),

                // Should this be string? As per json encoding, long is string
                27 => DerivedSchema.CreateLogical(id, "Integer", "string", "long"),
                28 => DerivedSchema.CreateLogical(id, "UInteger", "string", "long"),

                _ => null
            };
        }

        /// <summary>
        /// Create namespace
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private (string, string) SplitNodeId(string nodeId)
        {
            var id = nodeId.ToNodeId(_context);
            string avroStyleNamespace;
            if (id.NamespaceIndex == 0)
            {
                avroStyleNamespace = kNamespaceZeroName;
            }
            else
            {
                var ns = _context.NamespaceUris.GetString(id.NamespaceIndex);
                if (!Uri.TryCreate(ns, new UriCreationOptions
                {
                    DangerousDisablePathAndQueryCanonicalization = false
                }, out var result))
                {
                    avroStyleNamespace = ns.Split('/')
                        .Aggregate((a, b) => $"{a}.{b}");
                }
                else
                {
                    avroStyleNamespace = result.Host.Split('.').Reverse()
                        .Concat(result.AbsolutePath.Split('/'))
                        .Aggregate((a, b) => $"{a}.{b}");
                }
            }
            return (avroStyleNamespace, id.Identifier.ToString()!);
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
            /// <param name="alias"></param>
            /// <param name="type"></param>
            /// <param name="logicalType"></param>
            /// <returns></returns>
            internal static Schema CreateLogical(int builtInType, string alias,
                string type, string logicalType)
            {
                var dataTypeId = "i=" + builtInType;
                var baseType = Parse(
                    $$"""{"type": "{{type}}", "logicalType": "{{logicalType}}"}""");
                return new DerivedSchema(baseType.Tag,
                    new SchemaName(dataTypeId, kNamespaceZeroName, null, null),
                    new[] { alias });
            }

            /// <summary>
            /// Create primitive opc ua built in type
            /// </summary>
            /// <param name="builtInType"></param>
            /// <param name="alias"></param>
            /// <param name="type"></param>
            /// <returns></returns>
            internal static Schema CreatePrimitive(int builtInType,
                string alias, string type)
            {
                var dataTypeId = "i=" + builtInType;
                var baseType = PrimitiveSchema.NewInstance(type);
                return new DerivedSchema(baseType.Tag,
                    new SchemaName(dataTypeId, kNamespaceZeroName, null, null),
                    new[] { alias });
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
                var aliases = new[]
                {
                    Description.Name,
                    Description.DataTypeId
                };
                var (ns, dt) = schemas.SplitNodeId(Description.DataTypeId);

                if (Description.BaseDataType != null)
                {
                    // Derive from base schema
                    var baseSchema = schemas.GetSchema(Description.BaseDataType);
                    Schema = new DerivedSchema(baseSchema.Tag,
                        new SchemaName(dt, ns, null, null), aliases);
                    return;
                }

                // This is a built in type?
                if (Description.DataTypeId == "i=" + Description.BuiltInType)
                {
                    // Emit the built in type definition here instead
                    var builtIn = GetBuiltInSchema(Description.BuiltInType!.Value);
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
                    fields.Add(new Field(schema, field.Name, i));
                }
                var aliases = new[]
                {
                    Description.Name,
                    Description.DataTypeId
                };
                var (ns, dt) = schemas.SplitNodeId(Description.DataTypeId);
                Schema = RecordSchema.Create(dt, fields, ns, aliases);
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
                var aliases = new[]
                {
                    Description.Name,
                    Description.DataTypeId
                };
                var (ns, dt) = schemas.SplitNodeId(Description.DataTypeId);

                if (Description.IsOptionSet)
                {
                    // Flags
                    // ...
                }

                var symbols = Description.Fields.Select(e => e.Name).ToList();
                Schema = EnumSchema.Create(dt, symbols, ns, aliases,
                    defaultSymbol: Description.Fields[0].Name);
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
        private static void GetEncodingModes(out bool reversible, out bool uriEncoding,
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
        private readonly Dictionary<string, TypedDescription> _types = new();
        private readonly Opc.Ua.IServiceMessageContext _context;
        private readonly bool _useReversibleEncoding;
        private readonly bool _uriEncoding;
        private readonly bool _writeSingleValue;
        private readonly bool _dataValueRepresentation;
    }
}
