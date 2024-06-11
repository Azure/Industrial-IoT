// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
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
            Publisher.Models.DataSetFieldContentFlags? dataSetFieldContentMask,
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
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        protected T? Compile(string? name, PublishedDataSetMetaDataModel dataSet,
            HashSet<string>? uniqueNames)
        {
            // Collect types
            CollectTypes(dataSet);

            var schemas = GetDataSetFieldSchemas(name, dataSet, uniqueNames).ToList();
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
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        protected abstract IEnumerable<T> GetDataSetFieldSchemas(string? name,
            PublishedDataSetMetaDataModel dataSet, HashSet<string>? uniqueNames);

        /// <summary>
        /// Create record schema for the structure
        /// </summary>
        /// <param name="description"></param>
        /// <param name="rank"></param>
        /// <param name="baseTypeSchema"></param>
        /// <returns></returns>
        protected abstract T CreateStructureSchema(
            StructureDescriptionModel description, SchemaRank rank,
            T? baseTypeSchema = default);

        /// <summary>
        /// Create enum schema for the enum description
        /// </summary>
        /// <param name="description"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        protected abstract T CreateEnumSchema(EnumDescriptionModel description,
            SchemaRank rank);

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
        private void CollectTypes(PublishedDataSetMetaDataModel dataSet)
        {
            if (dataSet.StructureDataTypes != null)
            {
                foreach (var t in dataSet.StructureDataTypes)
                {
                    if (!_types.ContainsKey(t.DataTypeId))
                    {
                        _types.Add(t.DataTypeId, new StructureType(t));
                    }
                }
            }
            if (dataSet.SimpleDataTypes != null)
            {
                foreach (var t in dataSet.SimpleDataTypes)
                {
                    if (!_types.ContainsKey(t.DataTypeId))
                    {
                        _types.Add(t.DataTypeId, new SimpleType(t));
                    }
                }
            }
            if (dataSet.EnumDataTypes != null)
            {
                foreach (var t in dataSet.EnumDataTypes)
                {
                    if (!_types.ContainsKey(t.DataTypeId))
                    {
                        _types.Add(t.DataTypeId, new EnumType(t));
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
        /// <param name="valueRank"></param>
        /// <param name="arrayDimensions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected T LookupSchema(string dataType,
            SchemaRank valueRank = SchemaRank.Scalar,
            IReadOnlyList<uint>? arrayDimensions = null)
        {
            T? schema = null;

            if (arrayDimensions?.Count > 1)
            {
                valueRank = SchemaRank.Matrix;
            }

            if (_types.TryGetValue(dataType, out var description))
            {
                schema = description.GetSchema(this, valueRank);
            }

            schema ??= GetBuiltInDataTypeSchema(dataType, valueRank);
            return schema
                ?? throw new ArgumentException($"No Schema found for {dataType}");

            T? GetBuiltInDataTypeSchema(string dataType, SchemaRank valueRank)
            {
                var nodeId = dataType.ToExpandedNodeId(Context);
                if (nodeId.IdType == IdType.Numeric)
                {
                    var id = nodeId.Identifier as uint?;
                    if (id >= 0 && id <= 29)
                    {
                        return Encoding.GetSchemaForBuiltInType((BuiltInType)id, valueRank);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Create a type name
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        protected static string MakeUnique(string typeName, HashSet<string>? uniqueNames)
        {
            if (uniqueNames != null)
            {
                var uniqueName = typeName;
                for (var index = 1; uniqueNames.Contains(uniqueName); index++)
                {
                    uniqueName = typeName + index;
                }
                uniqueNames.Add(uniqueName);
                typeName = uniqueName;
            }
            return typeName;
        }

        /// <summary>
        /// Avro type
        /// </summary>
        private abstract record class TypedDescription
        {
            /// <summary>
            /// Get schema
            /// </summary>
            /// <param name="schema"></param>
            /// <param name="rank"></param>
            /// <returns></returns>
            public abstract T? GetSchema(BaseDataSetSchema<T> schema, SchemaRank rank);
        }

        /// <summary>
        /// Simple type
        /// </summary>
        /// <param name="Description"></param>
        private record class SimpleType(SimpleTypeDescriptionModel Description)
            : TypedDescription
        {
            /// <inheritdoc/>
            public override T? GetSchema(BaseDataSetSchema<T> schemas, SchemaRank rank)
            {
                if (Description.DataTypeId == "i=" + Description.BuiltInType)
                {
                    // Emit the built in type definition here instead
                    Debug.Assert(Description.BuiltInType.HasValue);
                    return schemas.Encoding.GetSchemaForBuiltInType(
                        (BuiltInType)Description.BuiltInType.Value, rank);
                }
                // Derive from base type or built in type
                if (Description.BaseDataType != null)
                {
                    // Derive from base type or built in type
                    return schemas.LookupSchema(Description.BaseDataType, rank);
                }

                // Derive from base type or built in type
                return schemas.Encoding.GetSchemaForBuiltInType((BuiltInType)
                    (Description.BuiltInType ?? (byte?)BuiltInType.String), rank);
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
            public override T? GetSchema(BaseDataSetSchema<T> schemas, SchemaRank rank)
            {
                if (_cache[(int)rank] != null)
                {
                    return _cache[(int)rank];
                }

                // Get super types
                T? baseSchema = default;
                if (Description.BaseDataType != null &&
                    schemas._types.TryGetValue(Description.BaseDataType, out var def) &&
                    def is StructureType baseDescription)
                {
                    baseSchema = baseDescription.GetSchema(schemas, rank);
                }

                _cache[(int)rank] = schemas.CreateStructureSchema(Description, rank, baseSchema);
                return _cache[(int)rank];
            }

            private readonly T?[] _cache = new T?[3];
        }

        /// <summary>
        /// Enum type
        /// </summary>
        /// <param name="Description"></param>
        private record class EnumType(EnumDescriptionModel Description)
            : TypedDescription
        {
            /// <inheritdoc/>
            public override T? GetSchema(BaseDataSetSchema<T> schemas, SchemaRank rank)
            {
                if (_cache[(int)rank] != null)
                {
                    return _cache[(int)rank];
                }

                if (Description.IsOptionSet)
                {
                    // Flags
                    // ...
                }

                _cache[(int)rank] = schemas.CreateEnumSchema(Description, rank);
                return _cache[(int)rank];
            }
            private readonly T?[] _cache = new T?[3];
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
                .HasFlag(Publisher.Models.DataSetFieldContentFlags.SingleFieldDegradeToValue);
            dataValueRepresentation = !_dataSetFieldContentMask
                .HasFlag(Publisher.Models.DataSetFieldContentFlags.RawData)
                && _dataSetFieldContentMask != 0;
        }

        /// <summary> Schema options </summary>
        protected readonly SchemaOptions _options;
        private readonly Publisher.Models.DataSetFieldContentFlags _dataSetFieldContentMask;
        private readonly Dictionary<string, TypedDescription> _types = new();
    }
}
