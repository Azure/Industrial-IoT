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
    using Opc.Ua;
    using DataSetFieldContentMask = Publisher.Models.DataSetFieldContentMask;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public class JsonDataSetMessageAvroSchema : IEventSchema
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
        /// Use compatibility mode
        /// </summary>
        public bool UseCompatibilityMode { get; }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public JsonDataSetMessageAvroSchema(DataSetWriterModel dataSetWriter,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            bool useCompatibilityMode = false, HashSet<string>? uniqueNames = null)
        {
            _options = options ?? new SchemaOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new JsonDataSetAvroSchema(dataSetWriter, options, uniqueNames);
            UseCompatibilityMode = useCompatibilityMode;

            Schema = Compile(dataSetWriter.DataSet?.Name ?? dataSetWriter.DataSetWriterName,
                dataSetWriter.MessageSettings?.DataSetMessageContentMask ?? 0u, uniqueNames);
        }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="dataSetContentMask"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public JsonDataSetMessageAvroSchema(PublishedDataSetModel dataSet,
            DataSetContentMask? dataSetContentMask = null,
            DataSetFieldContentMask? dataSetFieldContentMask = null,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            bool useCompatibilityMode = false, HashSet<string>? uniqueNames = null)
        {
            _options = options ?? new SchemaOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            UseCompatibilityMode = useCompatibilityMode;
            _dataSet = new JsonDataSetAvroSchema(null, dataSet,
                dataSetFieldContentMask, options, uniqueNames);
            Schema = Compile(dataSet.Name, dataSetContentMask ?? 0u, uniqueNames);
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
        /// <returns></returns>
        private Schema Compile(string? typeName,
            DataSetContentMask dataSetMessageContentMask, HashSet<string>? uniqueNames)
        {
            if (!_withDataSetMessageHeader)
            {
                // Not a data set message
                return _dataSet.Schema;
            }

            var encoding = new JsonBuiltInAvroSchemas(true, true);
            var pos = 0;
            var fields = new List<Field>();
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.DataSetWriterId))
            {
                if (!UseCompatibilityMode)
                {
                    fields.Add(
                        new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt16),
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
                }, SchemaUtils.NamespaceZeroName,
                    new[] { "i_" + DataTypes.ConfigurationVersionDataType });
                fields.Add(new(version, nameof(DataSetContentMask.MetaDataVersion), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Timestamp))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.DateTime),
                    nameof(DataSetContentMask.Timestamp), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Status))
            {
                if (!UseCompatibilityMode)
                {
                    fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.StatusCode),
                        nameof(DataSetContentMask.Status), pos++));
                }
                else
                {
                    // Up to version 2.8 we wrote the full status code
                    fields.Add(new(new JsonBuiltInAvroSchemas(false, false)
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
            if (!UseCompatibilityMode &&
                dataSetMessageContentMask.HasFlag(DataSetContentMask.DataSetWriterName))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(DataSetContentMask.DataSetWriterName), pos++));
            }

            fields.Add(new(_dataSet.Schema, "Payload", pos++));

            typeName = GetTypeName(typeName, uniqueNames);
            var ns = _options.Namespace != null ?
                SchemaUtils.NamespaceUriToNamespace(_options.Namespace) :
                SchemaUtils.PublisherNamespace;
            return RecordSchema.Create(typeName, fields, ns);
        }

        /// <summary>
        /// Create a type name
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        private static string GetTypeName(string? typeName, HashSet<string>? uniqueNames)
        {
            // Type name of the message record
            if (string.IsNullOrEmpty(typeName))
            {
                // Type name of the message record
                typeName = nameof(JsonDataSetMessage);
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

        private readonly JsonDataSetAvroSchema _dataSet;
        private readonly SchemaOptions _options;
        private readonly bool _withDataSetMessageHeader;
    }
}
