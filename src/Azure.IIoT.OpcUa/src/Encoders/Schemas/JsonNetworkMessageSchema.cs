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
        /// <param name="useCompatibilityMode"></param>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        public JsonNetworkMessageSchema(WriterGroupModel writerGroup,
            bool useCompatibilityMode = false, NamespaceTable? namespaces = null)
            : this(writerGroup.Name, writerGroup.DataSetWriters!,
                  useCompatibilityMode, namespaces,
                    writerGroup.MessageSettings?.NetworkMessageContentMask)
        {
        }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataSetWriter"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="namespaces"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <returns></returns>
        public JsonNetworkMessageSchema(string name, DataSetWriterModel dataSetWriter,
            bool useCompatibilityMode = false, NamespaceTable? namespaces = null,
            NetworkMessageContentMask? networkMessageContentMask = null)
            : this(name, dataSetWriter.YieldReturn(), useCompatibilityMode,
                  namespaces, networkMessageContentMask)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataSetWriters"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="namespaces"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <returns></returns>
        internal JsonNetworkMessageSchema(string? name,
            IEnumerable<DataSetWriterModel> dataSetWriters,
            bool useCompatibilityMode, NamespaceTable? namespaces,
            NetworkMessageContentMask? networkMessageContentMask)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriters);

            _useCompatibilityMode = useCompatibilityMode;

            Schema = Compile(name ?? "Message", dataSetWriters
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
        /// <param name="name"></param>
        /// <param name="dataSetWriters"></param>
        /// <param name="namespaces"></param>
        /// <param name="contentMask"></param>
        /// <returns></returns>
        private Schema Compile(string name, List<DataSetWriterModel> dataSetWriters,
            NamespaceTable? namespaces, NetworkMessageContentMask contentMask)
        {
            var dataSets = UnionSchema.Create(dataSetWriters
                .Where(writer => writer.DataSet != null)
                .Select(writer => new JsonDataSetMessageSchema(writer,
                    contentMask.HasFlag(NetworkMessageContentMask.DataSetMessageHeader),
                    _useCompatibilityMode, namespaces).Schema)
                .ToList());

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
            return RecordSchema.Create(name, fields, AvroUtils.kNamespaceZeroName);
        }

        private readonly bool _useCompatibilityMode;
    }
}
