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
    public class AvroDataSetMessageAvroSchema : IEventSchema, IAvroSchema
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
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public AvroDataSetMessageAvroSchema(DataSetWriterModel dataSetWriter,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            HashSet<string>? uniqueNames = null)
        {
            _options = options ?? new SchemaOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new AvroDataSetAvroSchema(dataSetWriter, options, uniqueNames);

            Schema = Compile(dataSetWriter.DataSet?.Name
                ?? dataSetWriter.DataSetWriterName, uniqueNames);
        }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public AvroDataSetMessageAvroSchema(PublishedDataSetModel dataSet,
            DataSetFieldContentMask? dataSetFieldContentMask = null,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            HashSet<string>? uniqueNames = null)
        {
            _options = options ?? new SchemaOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;

            _dataSet = new AvroDataSetAvroSchema(null, dataSet,
                dataSetFieldContentMask, options, uniqueNames);
            Schema = Compile(dataSet.Name, uniqueNames);
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
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        private Schema Compile(string? typeName, HashSet<string>? uniqueNames)
        {
            if (!_withDataSetMessageHeader)
            {
                // Not a data set message
                return _dataSet.Schema;
            }

            var encoding = new AvroBuiltInAvroSchemas();
            var version = RecordSchema.Create(nameof(ConfigurationVersionDataType),
                new List<Field>
            {
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    "MajorVersion", 0),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    "MinorVersion", 1)
            }, SchemaUtils.NamespaceZeroName,
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

                new(_dataSet.Schema, nameof(AvroDataSetMessage.Payload), 7)
            };

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
                typeName = nameof(AvroDataSetMessage);
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

        private readonly AvroDataSetAvroSchema _dataSet;
        private readonly SchemaOptions _options;
        private readonly bool _withDataSetMessageHeader;
    }
}
