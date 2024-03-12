// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Furly;
    using Furly.Extensions.Messaging;
    using System.Collections.Generic;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Opc.Ua;
    using DataSetFieldContentMask = Publisher.Models.DataSetFieldContentMask;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public class JsonDataSetMessageSchema : IEventSchema
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
        public JsonDataSetMessageSchema(DataSetWriterModel dataSetWriter,
            bool withDataSetMessageHeader = true,
            SchemaGenerationOptions? options = null)
        {
            _options = options ?? new SchemaGenerationOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new DataSetPayloadSchema(dataSetWriter,
                MessageEncoding.Json, options);
            Schema = Compile(
                dataSetWriter.DataSet?.Name ?? dataSetWriter.DataSetWriterName,
                GetNamespace(_options.Namespace, _options.Namespaces),
                dataSetWriter.MessageSettings?.DataSetMessageContentMask ?? 0u);
        }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="dataSetContentMask"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public JsonDataSetMessageSchema(PublishedDataSetModel dataSet,
            DataSetContentMask? dataSetContentMask = null,
            DataSetFieldContentMask? dataSetFieldContentMask = null,
            bool withDataSetMessageHeader = true,
            SchemaGenerationOptions? options = null)
        {
            _options = options ?? new SchemaGenerationOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new DataSetPayloadSchema(null, dataSet,
                MessageEncoding.Json, dataSetFieldContentMask, options);
            Schema = Compile(dataSet.Name,
                GetNamespace(_options.Namespace, _options.Namespaces),
                dataSetContentMask ?? 0u);
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
        /// <param name="dataSetMessageContentMask"></param>
        /// <returns></returns>
        private Schema Compile(string? typeName, string? @namespace,
            DataSetContentMask dataSetMessageContentMask)
        {
            if (!_withDataSetMessageHeader)
            {
                // Not a data set message
                return _dataSet.Schema;
            }

            var encoding = new JsonEncodingSchemaBuilder(true, false);
            var pos = 0;
            var fields = new List<Field>();
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.DataSetWriterId))
            {
                if (!_options.UseCompatibilityMode)
                {
                    fields.Add(
                        new (encoding.GetSchemaForBuiltInType(BuiltInType.UInt16),
                            nameof(DataSetContentMask.DataSetWriterId), pos++));
                }
                else
                {
                    // Up to version 2.8 we wrote the string id as id which is not per standard
                    fields.Add(
                        new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                            nameof(DataSetContentMask.DataSetWriterId), pos++));
                }
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.SequenceNumber))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                        nameof(DataSetContentMask.SequenceNumber), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.MetaDataVersion))
            {
                var version = RecordSchema.Create(nameof(ConfigurationVersionDataType),
                    new List<Field>
                {
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                        "MajorVersion", 0),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                        "MinorVersion", 1)
                }, AvroUtils.NamespaceZeroName,
                    new [] { "i_" + DataTypes.ConfigurationVersionDataType });
                fields.Add(new(version, nameof(DataSetContentMask.MetaDataVersion), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Timestamp))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.DateTime),
                    nameof(DataSetContentMask.Timestamp), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Status))
            {
                if (!_options.UseCompatibilityMode)
                {
                    fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.StatusCode),
                        nameof(DataSetContentMask.Status), pos++));
                }
                else
                {
                    // Up to version 2.8 we wrote the full status code
                    fields.Add(new(new JsonEncodingSchemaBuilder(false, false)
                        .GetSchemaForBuiltInType(BuiltInType.StatusCode),
                            nameof(DataSetContentMask.Status), pos++));
                }
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.MessageType))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(DataSetContentMask.MessageType), pos++));
            }
            if (!_options.UseCompatibilityMode &&
                dataSetMessageContentMask.HasFlag(DataSetContentMask.DataSetWriterName))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(DataSetContentMask.DataSetWriterName), pos++));
            }

            fields.Add(new(_dataSet.Schema, "Payload", pos++));

            // Type name of the message record
            if (string.IsNullOrEmpty(typeName))
            {
                // Type name of the message record
                typeName = nameof(JsonDataSetMessage);
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
