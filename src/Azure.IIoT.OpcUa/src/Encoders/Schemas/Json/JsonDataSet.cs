// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Messaging;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions to convert metadata into json schema. Note that this class
    /// generates a schema that complies with the json representation in
    /// <see cref="JsonEncoderEx.WriteDataSet(string?, Models.DataSet?)"/>.
    /// This depends on the network settings and reversible vs. nonreversible
    /// encoding mode.
    /// </summary>
    public sealed class JsonDataSet : BaseDataSetSchema<JsonSchema>,
        IEventSchema
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
            => ((JsonBuiltInSchemas)Encoding).Schemas;

        /// <summary>
        /// Get json schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="dataSetFieldContentFlags"></param>
        /// <param name="options"></param>
        /// <param name="def"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public JsonDataSet(PublishedDataSetMetaDataModel dataSet,
            DataSetFieldContentFlags? dataSetFieldContentFlags = null,
            SchemaOptions? options = null, Dictionary<string, JsonSchema>? def = null,
            HashSet<string>? uniqueNames = null)
            : base(dataSetFieldContentFlags, new JsonBuiltInSchemas(
                dataSetFieldContentFlags ?? default, def), options)
        {
            var name = dataSet.DataSetMetaData?.Name;
            Name = name ?? "DataSet";
            Ref = Compile(name, dataSet, uniqueNames);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToJsonString();
        }

        /// <inheritdoc/>
        protected override IEnumerable<JsonSchema> GetDataSetFieldSchemas(string? name,
            PublishedDataSetMetaDataModel dataSet, HashSet<string>? uniqueNames)
        {
            var singleValue = dataSet.Fields.Count == 1;
            GetEncodingMode(out var omitFieldName, out var fieldsAreDataValues,
                singleValue);
            if (omitFieldName)
            {
                var set = new HashSet<JsonSchema>();
                foreach (var fieldMetadata in dataSet.Fields)
                {
                    if (fieldMetadata?.DataType != null)
                    {
                        var schema = LookupSchema(fieldMetadata.DataType,
                            SchemaUtils.GetRank(fieldMetadata.ValueRank),
                            fieldMetadata.ArrayDimensions);
                        set.Add(schema);
                    }
                }
                return set;
            }

            var ns = _options.GetNamespaceUri();
            var properties = new Dictionary<string, JsonSchema>();
            var required = new List<string>();
            foreach (var fieldMetadata in dataSet.Fields)
            {
                if (fieldMetadata?.DataType != null)
                {
                    var schema = LookupSchema(fieldMetadata.DataType,
                        SchemaUtils.GetRank(fieldMetadata.ValueRank),
                        fieldMetadata.ArrayDimensions);
                    if (fieldMetadata.Name != null)
                    {
                        // TODO: Add properties to the field type
                        schema = Encoding.GetSchemaForDataSetField(ns, fieldsAreDataValues,
                            schema, (Opc.Ua.BuiltInType)fieldMetadata.BuiltInType);

                        schema.Description = fieldMetadata.Description;
                        properties.Add(fieldMetadata.Name, schema);
                    }
                }
            }
            var type = MakeUnique(name ?? dataSet.DataSetMetaData.Name ?? "DataSet", uniqueNames);
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
            SchemaRank rank, JsonSchema? baseTypeSchema)
        {
            var scalar = Definitions.Reference(description.DataTypeId
                .GetSchemaId(description.Name, Context), id =>
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
                    var schema = LookupSchema(field.DataType,
                        SchemaUtils.GetRank(field.ValueRank), field.ArrayDimensions);
                    if (!field.IsOptional)
                    {
                        required.Add(field.Name);
                    }
                    schema.Description = field.Description;
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

            return Encoding.GetSchemaForRank(scalar, rank);
        }

        /// <inheritdoc/>
        protected override JsonSchema CreateEnumSchema(EnumDescriptionModel description,
            SchemaRank rank)
        {
            var scalar = Definitions.Reference(description.DataTypeId
                .GetSchemaId(description.Name, Context), id =>
            {
                var fields = description.Fields
                    .Select(f => new Const<long>(f.Value)).ToArray();

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
            return Encoding.GetSchemaForRank(scalar, rank);
        }

        /// <inheritdoc/>
        protected override JsonSchema CreateUnionSchema(IReadOnlyList<JsonSchema> schemas)
        {
            return schemas.AsUnion(Definitions);
        }
    }
}
