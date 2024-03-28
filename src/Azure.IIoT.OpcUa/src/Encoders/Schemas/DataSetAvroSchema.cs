// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Avro;
    using Furly;
    using Furly.Extensions.Messaging;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Extensions to convert metadata into avro schema. Note that this class
    /// generates a schema that complies with the json representation in
    /// <see cref="JsonEncoderEx.WriteDataSet(string?, Models.DataSet?)"/>.
    /// This depends on the network settings and reversible vs. nonreversible
    /// encoding mode.
    /// </summary>
    public class DataSetAvroSchema : IEventSchema
    {
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
        /// The schema of the data set
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// Encoding schema for the data set
        /// </summary>
        internal BuiltInAvroSchemas Encoding { get; }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataSet"></param>
        /// <param name="encoding"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public DataSetAvroSchema(string? name, PublishedDataSetModel dataSet,
            MessageEncoding? encoding = null,
            Publisher.Models.DataSetFieldContentMask? dataSetFieldContentMask = null,
            SchemaOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(dataSet);
            _options = options ?? new SchemaOptions();
            _context = new ServiceMessageContext
            {
                NamespaceUris = _options.Namespaces ?? new NamespaceTable()
            };

            Encoding = BuiltInAvroSchemas.GetEncodingSchemas(encoding,
                dataSetFieldContentMask);

            var singleValue = dataSet.EnumerateMetaData().Take(2).Count() != 1;
            GetEncodingMode(out _omitFieldName, out _fieldsAreDataValues,
                singleValue, (uint)(dataSetFieldContentMask ?? 0u));

            Schema = Compile(name, dataSet);
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="encoding"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public DataSetAvroSchema(DataSetWriterModel dataSetWriter,
            MessageEncoding? encoding, SchemaOptions? options = null) :
            this(dataSetWriter.DataSetWriterName, dataSetWriter.DataSet
                    ?? throw new ArgumentException("Missing data set in writer"),
                encoding, dataSetWriter.DataSetFieldContentMask, options)
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
        /// <param name="name"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        private Schema Compile(string? name, PublishedDataSetModel dataSet)
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

            var schemas = GetDataSetSchemas(name, dataSet)
                .Distinct()
                .ToList();
            if (schemas.Count != 1)
            {
                return AvroUtils.CreateUnion(schemas);
            }
            return schemas[0];
        }

        /// <summary>
        /// Create data set schemas
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        private IEnumerable<Schema> GetDataSetSchemas(string? name,
            PublishedDataSetModel dataSet)
        {
            var fields = new List<Field>();
            var pos = 0;
            foreach (var (fieldName, fieldMetadata) in dataSet.EnumerateMetaData())
            {
                // Now collect the fields of the payload
                pos++;
                if (fieldMetadata?.DataType != null)
                {
                    var schema = LookupSchema(fieldMetadata.DataType, out var typeName);
                    if (_omitFieldName)
                    {
                        yield return schema.AsNullable();
                    }
                    else if (fieldName != null)
                    {
                        // TODO: Add properties to the field type
                        schema = Encoding.GetSchemaForDataSetField(
                            (typeName ?? fieldName) + "DataValue", _fieldsAreDataValues, schema);

                        fields.Add(new Field(schema, EscapeSymbol(fieldName), pos));
                    }
                }
            }
            if (!_omitFieldName)
            {
                yield return RecordSchema.Create(
                    EscapeSymbol(name ?? dataSet.Name ?? "DataSetPayload"), fields);
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
        /// Lookup the schema for the data type and make the type an array if
        /// it has such value rank. Make the resulting schema nullable.
        /// Return the name of the root schema.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="name"></param>
        /// <param name="valueRank"></param>
        /// <param name="arrayDimensions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private Schema LookupSchema(string dataType, out string? name,
            int valueRank = -1, IReadOnlyList<uint>? arrayDimensions = null)
        {
            Schema? schema = null;

            if (arrayDimensions != null)
            {
                valueRank = arrayDimensions.Count;
            }

            var array = valueRank > 0;

            name = null;
            if (_types.TryGetValue(dataType, out var description))
            {
                if (description.Schema == null)
                {
                    description.Resolve(this);
                }
                if (description.Schema != null)
                {
                    schema = description.Schema;
                    name = schema.Name;
                    if (valueRank >= ValueRanks.OneOrMoreDimensions)
                    {
                        schema = ArraySchema.Create(schema);
                    }
                }
            }

            schema ??= GetBuiltInDataTypeSchema(dataType, valueRank, out name);
            return schema
                ?? throw new ArgumentException($"No Schema found for {dataType}");

            Schema? GetBuiltInDataTypeSchema(string dataType, int valueRank,
                out string? name)
            {
                if (int.TryParse(dataType[2..], out var id)
                    && id >= 0 && id <= 29)
                {
                    name = ((BuiltInType)id).ToString();
                    return Encoding.GetSchemaForBuiltInType((BuiltInType)id,
                        valueRank);
                }
                name = null;
                return null;
            }
        }

        /// <summary>
        /// Create namespace
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private (string Id, string Namespace) SplitNodeId(string nodeId)
        {
            var id = nodeId.ToExpandedNodeId(_context);
            string avroStyleNamespace;
            if (id.NamespaceIndex == 0 && id.NamespaceUri == null)
            {
                avroStyleNamespace = AvroUtils.NamespaceZeroName;
            }
            else
            {
                avroStyleNamespace =
                    AvroUtils.NamespaceUriToNamespace(id.NamespaceUri);
            }
            var name = id.IdType switch
            {
                IdType.Opaque => "b_",
                IdType.Guid => "g_",
                IdType.String => "s_",
                _ => "i_"
            } + id.Identifier;
            return (avroStyleNamespace, AvroUtils.Escape(name));
        }

        /// <summary>
        /// Create namespace
        /// </summary>
        /// <param name="qualifiedName"></param>
        /// <param name="outerNamespace"></param>
        /// <returns></returns>
        private string SplitQualifiedName(string qualifiedName,
            string? outerNamespace = null)
        {
            var qn = qualifiedName.ToQualifiedName(_context);
            string avroStyleNamespace;
            if (qn.NamespaceIndex == 0)
            {
                avroStyleNamespace = AvroUtils.NamespaceZeroName;
            }
            else
            {
                var uri = _context.NamespaceUris.GetString(qn.NamespaceIndex);
                avroStyleNamespace = AvroUtils.NamespaceUriToNamespace(uri);
            }
            var name = AvroUtils.Escape(qn.Name);
            if (!string.Equals(outerNamespace, avroStyleNamespace,
                StringComparison.OrdinalIgnoreCase))
            {
                // Qualify if the name is in a different namespace
                name = $"{avroStyleNamespace}.{name}";
            }
            return name;
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
            public abstract void Resolve(DataSetAvroSchema schema);
        }

        /// <summary>
        /// Simple type
        /// </summary>
        /// <param name="Description"></param>
        private record class SimpleType(SimpleTypeDescriptionModel Description)
            : TypedDescription
        {
            /// <inheritdoc/>
            public override void Resolve(DataSetAvroSchema schemas)
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
                    Schema = schemas.Encoding.GetSchemaForBuiltInType(
                        (BuiltInType)Description.BuiltInType.Value);
                }
                else
                {
                    // Derive from base type or built in type
                    Schema = Description.BaseDataType != null ?
                        schemas.LookupSchema(Description.BaseDataType, out _) :
                        schemas.Encoding.GetSchemaForBuiltInType((BuiltInType)
                            (Description.BuiltInType ?? (byte?)BuiltInType.String));
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
            public override void Resolve(DataSetAvroSchema schemas)
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
                    var schema = schemas.LookupSchema(field.DataType, out _,
                        field.ValueRank, field.ArrayDimensions);
                    if (field.IsOptional)
                    {
                        schema = schema.AsNullable();
                    }
                    fields.Add(new Field(schema, schemas.EscapeSymbol(field.Name), i));
                }
                var (ns1, dt) = schemas.SplitNodeId(Description.DataTypeId);

                Schema = RecordSchema.Create(
                    schemas.SplitQualifiedName(Description.Name, ns1),
                    fields, ns1, new[] { dt },
                    customProperties: AvroUtils.GetProperties(Description.DataTypeId));
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
            public override void Resolve(DataSetAvroSchema schemas)
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

                var symbols = Description.Fields
                    .Select(e => schemas.EscapeSymbol(e.Name))
                    .ToList();
                Schema = EnumSchema.Create(
                    schemas.SplitQualifiedName(Description.Name, ns),
                    symbols, ns, new[] { dt },
                    customProperties: AvroUtils.GetProperties(Description.DataTypeId),
                    defaultSymbol: symbols[0]);
                // TODO: Build doc from fields descriptions
            }
        }

        /// <summary>
        /// Helper to escape field names and symbols
        /// based on the options set
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string EscapeSymbol(string name)
        {
            if (_options.EscapeSymbols)
            {
                return AvroUtils.Escape(name);
            }
            return name;
        }

        /// <summary>
        /// Determine field encoding
        /// </summary>
        /// <param name="writeSingleValue"></param>
        /// <param name="dataValueRepresentation"></param>
        /// <param name="isSingleFieldDataSet"></param>
        /// <param name="fieldContentMask"></param>
        private static void GetEncodingMode(out bool writeSingleValue,
            out bool dataValueRepresentation, bool isSingleFieldDataSet,
            uint fieldContentMask)
        {
            writeSingleValue = isSingleFieldDataSet &&
               (fieldContentMask &
                (uint)DataSetFieldContentMaskEx.SingleFieldDegradeToValue) != 0;
            dataValueRepresentation = (fieldContentMask &
                (uint)Publisher.Models.DataSetFieldContentMask.RawData) == 0
                && fieldContentMask != 0;
        }

        private readonly Dictionary<string, TypedDescription> _types = new();
        private readonly SchemaOptions _options;
        private readonly IServiceMessageContext _context;
        private readonly bool _omitFieldName;
        private readonly bool _fieldsAreDataValues;
    }
}
