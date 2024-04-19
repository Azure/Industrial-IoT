// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Furly;
    using Furly.Extensions.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions to convert metadata into avro schema. Note that this class
    /// generates a schema that complies with the avro representation in
    /// <see cref="AvroEncoder.WriteDataSet(string?, Models.DataSet?)"/>.
    /// </summary>
    public class AvroDataSetAvroSchema : BaseDataSetSchema<Schema>, IAvroSchema,
        IEventSchema
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

        /// <inheritdoc/>
        public override Schema Schema { get; }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataSet"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public AvroDataSetAvroSchema(string? name, PublishedDataSetModel dataSet,
            DataSetFieldContentMask? dataSetFieldContentMask = null,
            SchemaOptions? options = null, HashSet<string>? uniqueNames = null)
            : base(dataSetFieldContentMask, new AvroBuiltInAvroSchemas(), options)
        {
            Schema = Compile(name, dataSet, uniqueNames) ?? AvroSchema.Null;
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in avro
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public AvroDataSetAvroSchema(DataSetWriterModel dataSetWriter,
            SchemaOptions? options = null, HashSet<string>? uniqueNames = null) :
            this(dataSetWriter.DataSetWriterName, dataSetWriter.DataSet
                    ?? throw new ArgumentException("Missing data set in writer"),
                dataSetWriter.DataSetFieldContentMask, options, uniqueNames)
        {
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToString();
        }

        /// <inheritdoc/>
        protected override IEnumerable<Schema> GetDataSetFieldSchemas(string? name,
            PublishedDataSetModel dataSet, HashSet<string>? uniqueNames)
        {
            var singleValue = dataSet.EnumerateMetaData().Take(2).Count() != 1;
            GetEncodingMode(out var omitFieldName, out var fieldsAreDataValues,
                singleValue);
            if (omitFieldName)
            {
                var set = new HashSet<Schema>();
                foreach (var (_, fieldMetadata) in dataSet.EnumerateMetaData())
                {
                    if (fieldMetadata?.DataType != null)
                    {
                        set.Add(LookupSchema(fieldMetadata.DataType));
                    }
                }
                return set.Select(s => s.AsNullable());
            }

            var ns = _options.Namespace != null ?
                SchemaUtils.NamespaceUriToNamespace(_options.Namespace) :
                SchemaUtils.PublisherNamespace;

            var fields = new List<Field>();
            var pos = 0;
            foreach (var (fieldName, fieldMetadata) in dataSet.EnumerateMetaData())
            {
                // Now collect the fields of the payload
                pos++;
                if (fieldMetadata?.DataType != null)
                {
                    var schema = LookupSchema(fieldMetadata.DataType);
                    if (fieldName != null)
                    {
                        // TODO: Add properties to the field type
                        schema = Encoding.GetSchemaForDataSetField(
                            ns, fieldsAreDataValues, schema);

                        fields.Add(new Field(schema, SchemaUtils.Escape(fieldName), pos));
                    }
                }
            }
            if (fields.Count == 0)
            {
                return Enumerable.Empty<Schema>();
            }
            // Type name of the message record
            name ??= dataSet.Name;
            if (string.IsNullOrEmpty(name))
            {
                // Type name of the message record
                name = "DataSet";
            }
            else
            {
                name = SchemaUtils.Escape(name);
            }
            return RecordSchema.Create(MakeUnique(name, uniqueNames), fields).YieldReturn();
        }

        /// <inheritdoc/>
        protected override Schema CreateStructureSchema(StructureDescriptionModel description,
            Schema? baseTypeSchema)
        {
            //
            // |---------------|------------|----------------|
            // | Field Value   | Reversible | Non-Reversible |
            // |---------------|------------|----------------|
            // | NULL          | Omitted    | JSON null      |
            // | Default Value | Omitted    | Default Value  |
            // |---------------|------------|----------------|
            //
            var fields = new List<Field>();
            var pos = 0;
            if (baseTypeSchema is RecordSchema b)
            {
                foreach (var field in b.Fields)
                {
                    fields.Add(new Field(field.Schema, field.Name, pos++,
                        field.Aliases, field.Documentation, field.DefaultValue));
                    // Can we copy type property to the field to show inheritance
                }
            }

            foreach (var field in description.Fields)
            {
                var schema = LookupSchema(field.DataType,
                    SchemaUtils.GetRank(field.ValueRank), field.ArrayDimensions);
                if (field.IsOptional)
                {
                    schema = schema.AsNullable();
                }
                fields.Add(new Field(schema, SchemaUtils.Escape(field.Name), pos++));
            }

            var (ns1, dt) = SchemaUtils.SplitNodeId(description.DataTypeId, Context, true);
            var name = SchemaUtils.SplitQualifiedName(description.Name, Context, ns1);
            return RecordSchema.Create(name, fields, ns1, new[] { dt },
                customProperties: AvroSchema.Properties(description.DataTypeId));
        }

        /// <inheritdoc/>
        protected override Schema CreateEnumSchema(EnumDescriptionModel description)
        {
            var (ns, dt) = SchemaUtils.SplitNodeId(description.DataTypeId, Context, true);
            var symbols = description.Fields
                .Select(e => SchemaUtils.Escape(e.Name))
                .ToList();
            return EnumSchema.Create(
                SchemaUtils.SplitQualifiedName(description.Name, Context, ns),
                symbols, ns, new[] { dt },
                customProperties: AvroSchema.Properties(description.DataTypeId),
                defaultSymbol: symbols[0]);
            // TODO: Build doc from fields descriptions
        }

        /// <inheritdoc/>
        protected override Schema CreateArraySchema(Schema schema)
        {
            return ArraySchema.Create(schema);
        }

        /// <inheritdoc/>
        protected override Schema CreateUnionSchema(IReadOnlyList<Schema> schemas)
        {
            return AvroSchema.CreateUnion(schemas);
        }
    }
}
