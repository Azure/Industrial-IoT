// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Furly;
    using Furly.Extensions.Messaging;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Base Dataset message avro schema
    /// </summary>
    public abstract class BaseDataSetMessageAvroSchema : IEventSchema
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
        public string? Id => SchemaNormalization
            .ParsingFingerprint64(Schema)
            .ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// The actual schema
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// The inner data set schema
        /// </summary>
        protected Schema DataSetSchema => _dataSet.Schema;

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="dataSetSchemaToUse"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        protected BaseDataSetMessageAvroSchema(DataSetWriterModel dataSetWriter,
            BaseDataSetSchema<Schema> dataSetSchemaToUse,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            HashSet<string>? uniqueNames = null, bool useCompatibilityMode = false)
        {
            _options = options ?? new SchemaOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = dataSetSchemaToUse;

            Schema = Compile(dataSetWriter.DataSet?.Name ?? dataSetWriter.DataSetWriterName,
                dataSetWriter.MessageSettings?.DataSetMessageContentMask ?? 0u, uniqueNames,
                useCompatibilityMode);
        }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="dataSetSchemaToUse"></param>
        /// <param name="dataSetContentMask"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        protected BaseDataSetMessageAvroSchema(PublishedDataSetModel dataSet,
            BaseDataSetSchema<Schema> dataSetSchemaToUse,
            DataSetContentMask? dataSetContentMask = null,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            HashSet<string>? uniqueNames = null, bool useCompatibilityMode = false)
        {
            _options = options ?? new SchemaOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = dataSetSchemaToUse;
            Schema = Compile(dataSet.Name, dataSetContentMask ?? 0u,
                uniqueNames, useCompatibilityMode);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToString();
        }

        /// <summary>
        /// Compile the data set message schema
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="dataSetMessageContentMask"></param>
        /// <param name="uniqueNames"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        private Schema Compile(string? typeName,
            DataSetContentMask dataSetMessageContentMask, HashSet<string>? uniqueNames,
            bool useCompatibilityMode)
        {
            if (!_withDataSetMessageHeader)
            {
                // Not a data set message
                return DataSetSchema;
            }

            var fields = CollectFields(dataSetMessageContentMask, useCompatibilityMode);

            typeName = GetTypeName(typeName, uniqueNames);
            var ns = _options.Namespace != null ?
                SchemaUtils.NamespaceUriToNamespace(_options.Namespace) :
                SchemaUtils.PublisherNamespace;
            return RecordSchema.Create(typeName, fields.ToList(), ns);
        }

        /// <summary>
        /// Collect fields of the message schema
        /// </summary>
        /// <param name="dataSetMessageContentMask"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        protected abstract IEnumerable<Field> CollectFields(
            DataSetContentMask dataSetMessageContentMask, bool useCompatibilityMode);

        /// <summary>
        /// Create a type name
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        protected static string GetTypeName(string? typeName, HashSet<string>? uniqueNames)
        {
            // Type name of the message record
            if (string.IsNullOrEmpty(typeName))
            {
                // Type name of the message record
                typeName = "DataSetMessage";
            }
            else
            {
                typeName = SchemaUtils.Escape(typeName) + "DataSetMessage";
            }

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

        private readonly BaseDataSetSchema<Schema> _dataSet;
        private readonly SchemaOptions _options;
        private readonly bool _withDataSetMessageHeader;
    }
}
