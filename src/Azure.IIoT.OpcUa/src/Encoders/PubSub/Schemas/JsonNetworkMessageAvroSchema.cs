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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public class JsonNetworkMessageAvroSchema : IEventSchema
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
        /// Compatibility with 2.8 when encoding and decoding
        /// </summary>
        public bool UseCompatibilityMode { get; }

        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        public JsonNetworkMessageAvroSchema(WriterGroupModel writerGroup,
            SchemaOptions? options = null, bool useCompatibilityMode = false)
            : this(writerGroup.DataSetWriters!, writerGroup.Name,
                  writerGroup.MessageSettings?.NetworkMessageContentMask,
                  options, useCompatibilityMode)
        {
        }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="name"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        public JsonNetworkMessageAvroSchema(DataSetWriterModel dataSetWriter,
            string? name = null,
            NetworkMessageContentMask? networkMessageContentMask = null,
            SchemaOptions? options = null, bool useCompatibilityMode = false)
            : this(dataSetWriter.YieldReturn(), name,
                  networkMessageContentMask, options, useCompatibilityMode)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="name"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        internal JsonNetworkMessageAvroSchema(
            IEnumerable<DataSetWriterModel> dataSetWriters, string? name,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaOptions? options, bool useCompatibilityMode)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriters);

            UseCompatibilityMode = useCompatibilityMode;
            _options = options ?? new SchemaOptions();

            Schema = Compile(name, dataSetWriters
                .Where(writer => writer.DataSet != null)
                .ToList(), networkMessageContentMask ?? 0u);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToString();
        }

        /// <summary>
        /// Compile the schema for the data sets
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="dataSetWriters"></param>
        /// <param name="contentMask"></param>
        /// <returns></returns>
        private Schema Compile(string? typeName, List<DataSetWriterModel> dataSetWriters,
            NetworkMessageContentMask contentMask)
        {
            var HasDataSetMessageHeader = contentMask
                .HasFlag(NetworkMessageContentMask.DataSetMessageHeader);
            var HasNetworkMessageHeader = contentMask
                .HasFlag(NetworkMessageContentMask.NetworkMessageHeader);

            var dataSetMessageSchemas = dataSetWriters
                .Where(writer => writer.DataSet != null)
                .OrderBy(writer => writer.DataSetWriterId)
                .Select(writer =>
                    (writer.DataSetWriterId,
                    new JsonDataSetMessageAvroSchema(writer, HasDataSetMessageHeader,
                    	_options, UseCompatibilityMode, _uniqueNames).Schema))
                .ToList();

            if (dataSetMessageSchemas.Count == 0)
            {
                return AvroSchema.Null;
            }

            Schema? payloadType;
            if (dataSetMessageSchemas.Count > 1)
            {
                // Use the index of the data set writer as union index
                var length = dataSetMessageSchemas.Max(i => i.DataSetWriterId) + 1;
                Debug.Assert(length < ushort.MaxValue);
                var unionSchemas = Enumerable.Range(0, length)
                    .Select(i => (Schema)AvroSchema.CreatePlaceHolder(
                        "Empty" + i, SchemaUtils.PublisherNamespace))
                    .ToList();
                dataSetMessageSchemas
                    .ForEach(kv => unionSchemas[kv.DataSetWriterId] = kv.Schema);
                payloadType = AvroSchema.CreateUnion(unionSchemas);
            }
            else
            {
                payloadType = dataSetMessageSchemas[0].Schema;
            }

            var HasSingleDataSetMessage = contentMask
                .HasFlag(NetworkMessageContentMask.SingleDataSetMessage);
            if (!HasNetworkMessageHeader && HasSingleDataSetMessage)
            {
                // No network message header
                return payloadType;
            }

            payloadType = ArraySchema.Create(payloadType);

            var encoding = new JsonBuiltInAvroSchemas(true, false);
            var pos = 0;
            var fields = new List<Field>
            {
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(JsonNetworkMessage.MessageId), pos++),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(JsonNetworkMessage.MessageType), pos++)
            };

            if (contentMask.HasFlag(NetworkMessageContentMask.PublisherId))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(JsonNetworkMessage.PublisherId), pos++));
            }
            if (contentMask.HasFlag(NetworkMessageContentMask.DataSetClassId))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.Guid),
                    nameof(JsonNetworkMessage.DataSetClassId), pos++));
            }

            fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                nameof(JsonNetworkMessage.DataSetWriterGroup), pos++));

            // Now write messages - this is either one of or array of one of
            fields.Add(new(payloadType, nameof(JsonNetworkMessage.Messages), pos++));

            var ns = _options.Namespace != null ?
                SchemaUtils.NamespaceUriToNamespace(_options.Namespace) :
                SchemaUtils.PublisherNamespace;
            return RecordSchema.Create(GetName(typeName), fields, ns);
        }

        /// <summary>
        /// Get name of the type
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private string GetName(string? typeName)
        {
            // Type name of the message record
            if (string.IsNullOrEmpty(typeName))
            {
                // Type name of the message record
                typeName = nameof(JsonNetworkMessage);
            }
            else
            {
                typeName = SchemaUtils.Escape(typeName) + "NetworkMessage";
            }
            return MakeUnique(typeName);
        }

        /// <summary>
        /// Make unique
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string MakeUnique(string name)
        {
            var uniqueName = name;
            for (var index = 1; _uniqueNames.Contains(uniqueName); index++)
            {
                uniqueName = name + index;
            }
            _uniqueNames.Add(uniqueName);
            return uniqueName;
        }

        private readonly SchemaOptions _options;
        private readonly HashSet<string> _uniqueNames = new();
    }
}
