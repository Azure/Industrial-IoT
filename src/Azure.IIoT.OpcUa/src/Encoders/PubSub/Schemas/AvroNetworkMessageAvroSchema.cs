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
    public class AvroNetworkMessageAvroSchema : IEventSchema, IAvroSchema
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
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public AvroNetworkMessageAvroSchema(WriterGroupModel writerGroup,
            SchemaOptions? options = null)
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
        public AvroNetworkMessageAvroSchema(DataSetWriterModel dataSetWriter,
            string? name = null,
            NetworkMessageContentMask? networkMessageContentMask = null,
            SchemaOptions? options = null)
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
        internal AvroNetworkMessageAvroSchema(
            IEnumerable<DataSetWriterModel> dataSetWriters, string? name,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaOptions? options)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriters);

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
                    new AvroDataSetMessageAvroSchema(writer,
                        HasDataSetMessageHeader, _options, _uniqueNames).Schema))
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
#if NO_UNION_ROOT
                // If we want to have a root schema instead of union
                if (payloadType is UnionSchema)
                {
                    return payloadType.CreateRoot(
                        typeName == null ? null : MakeUnique(typeName));
                }
#endif
                return payloadType;
            }

            payloadType = ArraySchema.Create(payloadType);

            var encoding = new AvroBuiltInAvroSchemas();
            var fields = HasNetworkMessageHeader ?
                new List<Field>
                {
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(AvroNetworkMessage.MessageId), 0),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(AvroNetworkMessage.MessageType), 1),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(AvroNetworkMessage.PublisherId), 2),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.Guid),
                        nameof(AvroNetworkMessage.DataSetClassId), 3),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(AvroNetworkMessage.DataSetWriterGroup), 4),

                    new(payloadType, nameof(AvroNetworkMessage.Messages), 5)
                } :
                new List<Field>
                {
                    new(payloadType, nameof(AvroNetworkMessage.Messages), 0)
                };

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
                typeName = nameof(AvroNetworkMessage);
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
