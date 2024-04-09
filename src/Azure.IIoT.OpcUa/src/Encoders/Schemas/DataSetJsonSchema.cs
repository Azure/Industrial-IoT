// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly;
    using Furly.Extensions.Messaging;
    using Json.Schema;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Avro;

    /// <summary>
    /// Extensions to convert metadata into json schema. Note that this class
    /// generates a schema that complies with the json representation in
    /// <see cref="JsonEncoderEx.WriteDataSet(string?, Models.DataSet?)"/>.
    /// This depends on the network settings and reversible vs. nonreversible
    /// encoding mode.
    /// </summary>
    public sealed class DataSetJsonSchema : BaseDataSetSchema<JsonSchema>, IEventSchema
    {
        /// <inheritdoc/>
        public string Type => ContentMimeType.Json;

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public ulong Version { get; }

        /// <inheritdoc/>
        public string? Id { get; }

        /// <inheritdoc/>
        string IEventSchema.Schema => Schema.ToJsonString();

        /// <inheritdoc/>
        public override JsonSchema Schema => new()
        {
            Definitions = Definitions,
            Type = Ref == null ? SchemaType.Null : SchemaType.None,
            Reference = Ref?.Reference
        };

        /// <summary>
        /// Schema reference
        /// </summary>
        public JsonSchema? Ref { get; }

        /// <summary>
        /// Definitions
        /// </summary>
        internal Dictionary<string, JsonSchema> Definitions
            => ((BuiltInJsonSchemas)Encoding).Definitions;

        /// <summary>
        /// Get json schema for a dataset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataSet"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        public DataSetJsonSchema(string? name, PublishedDataSetModel dataSet,
            DataSetFieldContentMask? dataSetFieldContentMask = null,
            SchemaOptions? options = null, Dictionary<string, JsonSchema>? definitions = null)
            : base(dataSetFieldContentMask, new BuiltInJsonSchemas(
                dataSetFieldContentMask ?? default, definitions), options)
        {
            Name = name ?? "DataSet";
            Ref = Compile(name, dataSet);
        }

        /// <summary>
        /// Get json schema for a dataset
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        public DataSetJsonSchema(DataSetWriterModel dataSetWriter,
            SchemaOptions? options = null, Dictionary<string, JsonSchema>? definitions = null) :
            this(dataSetWriter.DataSetWriterName, dataSetWriter.DataSet
                    ?? throw new ArgumentException("Missing data set in writer"),
                dataSetWriter.DataSetFieldContentMask, options, definitions)
        {
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToJsonString();
        }

        /// <inheritdoc/>
        protected override IEnumerable<JsonSchema> GetDataSetFieldSchemas(string? name,
            PublishedDataSetModel dataSet)
        {
            var singleValue = dataSet.EnumerateMetaData().Take(2).Count() != 1;
            GetEncodingMode(out var omitFieldName, out var fieldsAreDataValues,
                singleValue);

            if (omitFieldName)
            {
                var set = new HashSet<JsonSchema>();
                foreach (var (_, fieldMetadata) in dataSet.EnumerateMetaData())
                {
                    if (fieldMetadata?.DataType != null)
                    {
                        var schema = LookupSchema(fieldMetadata.DataType, out _);
                        set.Add(LookupSchema(fieldMetadata.DataType, out _));
                    }
                }
                return set;
            }

            var ns = _options.GetNamespaceUri();
            var properties = new Dictionary<string, JsonSchema>();
            var required = new List<string>();
            foreach (var (fieldName, fieldMetadata) in dataSet.EnumerateMetaData())
            {
                if (fieldMetadata?.DataType != null)
                {
                    var schema = LookupSchema(fieldMetadata.DataType, out var typeName);
                    if (fieldName != null)
                    {
                        // TODO: Add properties to the field type
                        schema = Encoding.GetSchemaForDataSetField(
                            (typeName ?? fieldName) + "DataValue", ns,
                            fieldsAreDataValues, schema);

                        properties.Add(fieldName, schema);
                    }
                }
            }
            if (properties.Count == 0)
            {
                return Enumerable.Empty<JsonSchema>();
            }
            var type = name ?? dataSet.Name ?? "DataSetPayload";
            return Definitions.Reference(_options.GetSchemaId(type), id => new JsonSchema
            {
                Id = id,
                Title = type,
                Type = SchemaType.Object,
                Properties = properties,
                Required = required
            }).YieldReturn();
        }

        /// <inheritdoc/>
        protected override JsonSchema CreateStructureSchema(StructureDescriptionModel description,
            JsonSchema? baseTypeSchema)
        {
            return Definitions.Reference(description.DataTypeId.GetSchemaId(Context), id =>
            {
                //
                // |---------------|------------|----------------|
                // | Field Value   | Reversible | Non-Reversible |
                // |---------------|------------|----------------|
                // | NULL          | Omitted    | JSON null      |
                // | Default Value | Omitted    | Default Value  |
                // |---------------|------------|----------------|
                //
                var properties = new Dictionary<string, JsonSchema>();
                var required = new List<string>();
                for (var i = 0; i < description.Fields.Count; i++)
                {
                    var field = description.Fields[i];
                    var schema = LookupSchema(field.DataType, out _,
                        field.ValueRank, field.ArrayDimensions);
                    if (!field.IsOptional)
                    {
                        required.Add(field.Name);
                    }
                    properties.Add(field.Name, schema);
                }
                return new JsonSchema
                {
                    Id = id,
                    Title = description.Name,
                    Type = SchemaType.Object,
                    AllOf = baseTypeSchema == null ? null : new[] { baseTypeSchema },
                    Properties = properties,
                    Required = required,
                    AdditionalProperties = new JsonSchema { Allowed = false }
                };
            });
        }

        /// <inheritdoc/>
        protected override JsonSchema CreateEnumSchema(EnumDescriptionModel description)
        {
            return Definitions.Reference(description.DataTypeId.GetSchemaId(Context), id =>
            {
                var fields = description.Fields.Select(f => new Const<long>(f.Value)).ToArray();
                // TODO: Build doc from fields descriptions

                return new JsonSchema
                {
                    Id = id,
                    Title = description.Name,
                    Enum = fields,
                    Type = SchemaType.Integer,
                    Format = "int32"
                };
            });
        }

        /// <inheritdoc/>
        protected override JsonSchema CreateArraySchema(JsonSchema schema)
        {
            return schema.AsArray();
        }

        /// <inheritdoc/>
        protected override JsonSchema CreateUnionSchema(IReadOnlyList<JsonSchema> schemas)
        {
            return schemas.AsUnion(Definitions);
        }
    }
}
