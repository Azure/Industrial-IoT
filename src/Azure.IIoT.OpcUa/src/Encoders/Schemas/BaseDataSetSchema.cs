// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Opc.Ua;
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
    /// <typeparam name="T"></typeparam>
    public abstract class BaseDataSetSchema<T> where T : class
    {
        /// <summary>
        /// The schema of the data set
        /// </summary>
        public abstract T Schema { get; }

        /// <summary>
        /// Encoding schema for the data set
        /// </summary>
        protected BaseBuiltInSchemas<T> Encoding { get; }

        /// <summary>
        /// Message context
        /// </summary>
        internal ServiceMessageContext Context { get; }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="encoding"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        protected BaseDataSetSchema(
            Publisher.Models.DataSetFieldContentMask? dataSetFieldContentMask,
            BaseBuiltInSchemas<T> encoding, SchemaOptions? options = null)
        {
            _dataSetFieldContentMask = dataSetFieldContentMask ?? default;
            _options = options ?? new SchemaOptions();
            Context = new ServiceMessageContext
            {
                NamespaceUris = new NamespaceTable()
            };
            Encoding = encoding;
        }

        /// <summary>
        /// Compile
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        protected T? Compile(string? name, PublishedDataSetModel dataSet)
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

            var schemas = GetDataSetFieldSchemas(name, dataSet).ToList();
            if (schemas.Count == 0)
            {
                return default;
            }
            if (schemas.Count != 1)
            {
                return CreateUnionSchema(schemas);
            }
            return schemas[0];
        }

        /// <summary>
        /// Create data set schemas
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        protected abstract IEnumerable<T> GetDataSetFieldSchemas(string? name,
            PublishedDataSetModel dataSet);

        /// <summary>
        /// Create record schema for the structure
        /// </summary>
        /// <param name="description"></param>
        /// <param name="baseTypeSchema"></param>
        /// <returns></returns>
        protected abstract T CreateStructureSchema(
            StructureDescriptionModel description, T? baseTypeSchema = default);

        /// <summary>
        /// Create enum schema for the enum description
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        protected abstract T CreateEnumSchema(EnumDescriptionModel description);

        /// <summary>
        /// Create array schema
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        protected abstract T CreateArraySchema(T schema);

        /// <summary>
        /// Create union schema
        /// </summary>
        /// <param name="schemas"></param>
        /// <returns></returns>
        protected abstract T CreateUnionSchema(IReadOnlyList<T> schemas);

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
        protected T LookupSchema(string dataType, out string? name,
            int valueRank = -1, IReadOnlyList<uint>? arrayDimensions = null)
        {
            T? schema = null;

            if (arrayDimensions != null)
            {
                valueRank = arrayDimensions.Count;
            }

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
                    name = description.Name;
                    if (valueRank >= ValueRanks.OneOrMoreDimensions)
                    {
                        schema = CreateArraySchema(schema);
                    }
                }
            }

            schema ??= GetBuiltInDataTypeSchema(dataType, valueRank, out name);
            return schema
                ?? throw new ArgumentException($"No Schema found for {dataType}");

            T? GetBuiltInDataTypeSchema(string dataType, int valueRank,
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
        /// Avro type
        /// </summary>
        private abstract record class TypedDescription
        {
            /// <summary>
            /// Resolved schema of the type
            /// </summary>
            public T? Schema { get; set; }

            /// <summary>
            /// Return name
            /// </summary>
            public string? Name { get; set; }

            /// <summary>
            /// Resolve the type
            /// </summary>
            /// <param name="schema"></param>
            public abstract void Resolve(BaseDataSetSchema<T> schema);
        }

        /// <summary>
        /// Simple type
        /// </summary>
        /// <param name="Description"></param>
        private record class SimpleType(SimpleTypeDescriptionModel Description)
            : TypedDescription
        {
            /// <inheritdoc/>
            public override void Resolve(BaseDataSetSchema<T> schemas)
            {
                if (Schema != null)
                {
                    return;
                }

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
            public override void Resolve(BaseDataSetSchema<T> schemas)
            {
                if (Schema != null)
                {
                    return;
                }

                // Get super types
                T? baseSchema = default;
                if (Description.BaseDataType != null &&
                    schemas._types.TryGetValue(Description.BaseDataType, out var def) &&
                    def is StructureType baseDescription)
                {
                    baseDescription.Resolve(schemas);
                    if (baseDescription.Schema != null)
                    {
                        baseSchema = baseDescription.Schema;
                    }
                }
                Schema = schemas.CreateStructureSchema(Description, baseSchema);
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
            public override void Resolve(BaseDataSetSchema<T> schemas)
            {
                if (Schema != null)
                {
                    return;
                }

                if (Description.IsOptionSet)
                {
                    // Flags
                    // ...
                }

                Schema = schemas.CreateEnumSchema(Description);
            }
        }

        /// <summary>
        /// Determine field encoding
        /// </summary>
        /// <param name="writeSingleValue"></param>
        /// <param name="dataValueRepresentation"></param>
        /// <param name="isSingleFieldDataSet"></param>
        protected void GetEncodingMode(out bool writeSingleValue,
            out bool dataValueRepresentation, bool isSingleFieldDataSet)
        {
            writeSingleValue = isSingleFieldDataSet && _dataSetFieldContentMask
                .HasFlag(Publisher.Models.DataSetFieldContentMask.SingleFieldDegradeToValue);
            dataValueRepresentation = !_dataSetFieldContentMask
                .HasFlag(Publisher.Models.DataSetFieldContentMask.RawData)
                && _dataSetFieldContentMask != 0;
        }

        /// <summary> Schema options </summary>
        protected readonly SchemaOptions _options;
        private readonly Publisher.Models.DataSetFieldContentMask _dataSetFieldContentMask;
        private readonly Dictionary<string, TypedDescription> _types = new();
    }
}
