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
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Azure.IIoT.OpcUa.Encoders.PubSub;

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
        /// <param name="namespace"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        public JsonNetworkMessageSchema(WriterGroupModel writerGroup,
            string? @namespace = null, bool useCompatibilityMode = false,
            NamespaceTable? namespaces = null)
            : this(writerGroup.DataSetWriters!, writerGroup.Name, @namespace,
                  writerGroup.MessageSettings?.NetworkMessageContentMask,
                  useCompatibilityMode, namespaces)
        {
        }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="name"></param>
        /// <param name="namespace"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        public JsonNetworkMessageSchema(DataSetWriterModel dataSetWriter,
            string? name = null, string? @namespace = null,
            NetworkMessageContentMask? networkMessageContentMask = null,
            bool useCompatibilityMode = false, NamespaceTable? namespaces = null)
            : this(dataSetWriter.YieldReturn(), name, @namespace,
                  networkMessageContentMask, useCompatibilityMode, namespaces)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="name"></param>
        /// <param name="namespace"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        internal JsonNetworkMessageSchema(
            IEnumerable<DataSetWriterModel> dataSetWriters, string? name,
            string? @namespace, NetworkMessageContentMask? networkMessageContentMask,
            bool useCompatibilityMode, NamespaceTable? namespaces)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriters);

            _useCompatibilityMode = useCompatibilityMode;

            Schema = Compile(name, @namespace, dataSetWriters
                .Where(writer => writer.DataSet != null)
                .ToList(),
                namespaces, networkMessageContentMask ?? 0u);
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
        /// <param name="namespace"></param>
        /// <param name="dataSetWriters"></param>
        /// <param name="namespaces"></param>
        /// <param name="contentMask"></param>
        /// <returns></returns>
        private Schema Compile(string? typeName, string? @namespace,
            List<DataSetWriterModel> dataSetWriters, NamespaceTable? namespaces,
            NetworkMessageContentMask contentMask)
        {
            @namespace = GetNamespace(@namespace, namespaces);

            var dataSets = AvroUtils.CreateUnion(dataSetWriters
                .Where(writer => writer.DataSet != null)
                .Select(writer => new JsonDataSetMessageSchema(writer, @namespace,
                    contentMask.HasFlag(NetworkMessageContentMask.DataSetMessageHeader),
                    _useCompatibilityMode, namespaces).Schema));

            var payloadType =
                contentMask.HasFlag(NetworkMessageContentMask.SingleDataSetMessage) ?
                (Schema)dataSets : ArraySchema.Create(dataSets);

            if (contentMask == 0u ||
                contentMask == NetworkMessageContentMask.SingleDataSetMessage)
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
            fields.Add(new(payloadType,
                "Payload", pos++));

            typeName = string.IsNullOrEmpty(typeName)
                ? nameof(JsonNetworkMessage) : typeName + "NetworkMessage";
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

        private readonly bool _useCompatibilityMode;
    }
}
