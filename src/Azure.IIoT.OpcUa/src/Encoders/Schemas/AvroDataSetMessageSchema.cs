// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Furly;
    using Furly.Extensions.Messaging;
    using Opc.Ua;
    using DataSetFieldContentMask = Publisher.Models.DataSetFieldContentMask;
    using System.Collections.Generic;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public class AvroDataSetMessageSchema : IEventSchema
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
        /// The actual schema
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public AvroDataSetMessageSchema(DataSetWriterModel dataSetWriter,
            bool withDataSetMessageHeader = true,
            SchemaGenerationOptions? options = null)
        {
            _options = options ?? new SchemaGenerationOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new DataSetPayloadSchema(dataSetWriter,
                MessageEncoding.Avro, options);
            Schema = Compile(
                dataSetWriter.DataSet?.Name ?? dataSetWriter.DataSetWriterName,
                GetNamespace(_options.Namespace, _options.Namespaces));
        }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public AvroDataSetMessageSchema(PublishedDataSetModel dataSet,
            DataSetFieldContentMask? dataSetFieldContentMask = null,
            bool withDataSetMessageHeader = true,
            SchemaGenerationOptions? options = null)
        {
            _options = options ?? new SchemaGenerationOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;

            _dataSet = new DataSetPayloadSchema(null, dataSet,
                MessageEncoding.Avro, dataSetFieldContentMask, options);
            Schema = Compile(dataSet.Name,
                GetNamespace(_options.Namespace, _options.Namespaces));
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
        /// <param name="namespace"></param>
        /// <returns></returns>
        private Schema Compile(string? typeName, string? @namespace)
        {
            if (!_withDataSetMessageHeader)
            {
                // Not a data set message
                return _dataSet.Schema;
            }

            var encoding = new AvroEncodingSchemaBuilder();
            var version = RecordSchema.Create(nameof(ConfigurationVersionDataType),
                new List<Field>
            {
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    "MajorVersion", 0),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    "MinorVersion", 1)
            }, AvroUtils.NamespaceZeroName,
                new[] { "i_" + DataTypes.ConfigurationVersionDataType });

            var fields = new List<Field>
            {
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(DataSetContentMask.MessageType), 0),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(DataSetContentMask.DataSetWriterName), 1),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt16),
                    nameof(DataSetContentMask.DataSetWriterId), 2),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    nameof(DataSetContentMask.SequenceNumber), 3),
                new(version,
                    nameof(DataSetContentMask.MetaDataVersion), 4),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.DateTime),
                    nameof(DataSetContentMask.Timestamp), 5),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.StatusCode),
                    nameof(DataSetContentMask.Status), 6),
                new(_dataSet.Schema, "Payload", 7)
            };

            // Type name of the message record
            if (string.IsNullOrEmpty(typeName))
            {
                // Type name of the message record
                typeName = nameof(AvroDataSetMessage);
            }
            else
            {
                if (_options.EscapeSymbols)
                {
                    typeName = AvroUtils.Escape(typeName);
                }
                typeName += "DataSetMessage";
            }
            if (@namespace != null)
            {
                @namespace = AvroUtils.NamespaceUriToNamespace(@namespace);
            }
            return RecordSchema.Create(
                typeName, fields, @namespace ?? AvroUtils.NamespaceZeroName);
        }

        /// <summary>
        /// Get namespace uri
        /// </summary>
        /// <param name="namespace"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        private static string? GetNamespace(string? @namespace,
            NamespaceTable? namespaces)
        {
            if (@namespace == null && namespaces?.Count >= 1)
            {
                // Get own namespace from namespace table if possible
                @namespace = namespaces.GetString(1);
            }
            return @namespace;
        }

        private readonly DataSetPayloadSchema _dataSet;
        private readonly SchemaGenerationOptions _options;
        private readonly bool _withDataSetMessageHeader;
    }
}
