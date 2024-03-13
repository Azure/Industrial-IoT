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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public class JsonNetworkMessageSchema : IEventSchema
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
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public JsonNetworkMessageSchema(WriterGroupModel writerGroup,
            SchemaGenerationOptions? options = null)
            : this(writerGroup.DataSetWriters!, writerGroup.Name,
                  writerGroup.MessageSettings?.NetworkMessageContentMask,
                  options)
        {
        }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="name"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public JsonNetworkMessageSchema(DataSetWriterModel dataSetWriter,
            string? name = null,
            NetworkMessageContentMask? networkMessageContentMask = null,
            SchemaGenerationOptions? options = null)
            : this(dataSetWriter.YieldReturn(), name,
                  networkMessageContentMask, options)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="name"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal JsonNetworkMessageSchema(
            IEnumerable<DataSetWriterModel> dataSetWriters, string? name,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaGenerationOptions? options)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriters);

            _options = options ?? new SchemaGenerationOptions();

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
            var @namespace = GetNamespace(_options.Namespace, _options.Namespaces);

            var dataSets = AvroUtils.CreateUnion(dataSetWriters
                .Where(writer => writer.DataSet != null)
                .Select(writer => new JsonDataSetMessageSchema(writer,
                    contentMask.HasFlag(NetworkMessageContentMask.DataSetMessageHeader),
                    _options).Schema));

            var payloadType =
                contentMask.HasFlag(NetworkMessageContentMask.SingleDataSetMessage) ?
                (Schema)dataSets : ArraySchema.Create(dataSets);

            if ((contentMask &
                ~(NetworkMessageContentMask.SingleDataSetMessage |
                  NetworkMessageContentMask.DataSetMessageHeader)) == 0u)
            {
                // No network message header
                return payloadType;
            }

            var encoding = new JsonEncodingSchemaBuilder(true, false);
            var pos = 0;
            var fields = new List<Field>
            {
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    "MessageId", pos++),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    "MessageType", pos++)
            };

            if (contentMask.HasFlag(NetworkMessageContentMask.PublisherId))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    "PublisherId", pos++));
            }
            if (contentMask.HasFlag(NetworkMessageContentMask.DataSetClassId))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.Guid),
                    "DataSetClassId", pos++));
            }

            fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                "DataSetWriterGroup", pos++));

            // Now write messages - this is either one of or array of one of
            fields.Add(new(payloadType,
                "Messages", pos++));

            // Type name of the message record
            if (string.IsNullOrEmpty(typeName))
            {
                // Type name of the message record
                typeName = nameof(JsonNetworkMessage);
            }
            else
            {
                if (_options.EscapeSymbols)
                {
                    typeName = AvroUtils.Escape(typeName);
                }
                typeName += "NetworkMessage";
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

        private readonly SchemaGenerationOptions _options;
    }
}
