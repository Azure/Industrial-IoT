// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Messaging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Json.Schema;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public sealed class JsonNetworkMessageSchema : IEventSchema
    {
        /// <inheritdoc/>
        public string Type => ContentMimeType.AvroSchema;

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public ulong Version { get; }

        /// <inheritdoc/>
        public string? Id { get; }

        /// <inheritdoc/>
        string IEventSchema.Schema => ToString()!;

        /// <summary>
        /// Schema reference
        /// </summary>
        public JsonSchema? Ref { get; }

        /// <summary>
        /// Definitions
        /// </summary>
        internal Dictionary<string, JsonSchema> Definitions { get; }

        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        public JsonNetworkMessageSchema(WriterGroupModel writerGroup,
            SchemaOptions? options = null,
            Dictionary<string, JsonSchema>? definitions = null)
            : this(writerGroup.DataSetWriters!, writerGroup.Name,
                  writerGroup.MessageSettings?.NetworkMessageContentMask,
                  options, definitions)
        {
        }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="name"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        public JsonNetworkMessageSchema(DataSetWriterModel dataSetWriter,
            string? name = null,
            NetworkMessageContentMask? networkMessageContentMask = null,
            SchemaOptions? options = null,
            Dictionary<string, JsonSchema>? definitions = null)
            : this(dataSetWriter.YieldReturn(), name,
                  networkMessageContentMask, options, definitions)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="name"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        internal JsonNetworkMessageSchema(
            IEnumerable<DataSetWriterModel> dataSetWriters, string? name,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaOptions? options, Dictionary<string, JsonSchema>? definitions)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriters);

            Definitions = definitions ?? new();
            _options = options ?? new SchemaOptions();

            Name = GetName(name);
            Ref = Compile(dataSetWriters
                .Where(writer => writer.DataSet != null)
                .ToList(), networkMessageContentMask ?? 0u);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return new JsonSchema
            {
                SchemaVersion = SchemaVersion.Draft4,
                Types = Ref == null ? new[] { SchemaType.Null } : Array.Empty<SchemaType>(),
                Definitions = Definitions,
                Reference = Ref?.Reference
            }.ToJsonString();
        }

        /// <summary>
        /// Compile the schema for the data sets
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="contentMask"></param>
        /// <returns></returns>
        private JsonSchema? Compile(List<DataSetWriterModel> dataSetWriters,
            NetworkMessageContentMask contentMask)
        {
            var dataSets = dataSetWriters
                .Where(writer => writer.DataSet != null)
                .Select(writer => new JsonDataSetMessageSchema(writer,
                    contentMask.HasFlag(NetworkMessageContentMask.DataSetMessageHeader),
                        _options, Definitions).Ref!)
                .Where(r => r != null)
                .ToList();

            if (dataSets.Count == 0)
            {
                return null;
            }

            var dataSetMessages = dataSets.Count > 1 ? dataSets.AsUnion(Definitions)
                : dataSets[0];
            var payloadType =
                contentMask.HasFlag(NetworkMessageContentMask.SingleDataSetMessage) ?
                dataSetMessages : dataSetMessages.AsArray();
            if ((contentMask &
                ~(NetworkMessageContentMask.SingleDataSetMessage |
                  NetworkMessageContentMask.DataSetMessageHeader)) == 0u)
            {
                // No network message header
                return payloadType;
            }

            var encoding = new BuiltInJsonSchemas(true, false, Definitions);
            var properties = new Dictionary<string, JsonSchema>
            {
                [nameof(JsonNetworkMessage.MessageId)] =
                    encoding.GetSchemaForBuiltInType(BuiltInType.String),
                [nameof(JsonNetworkMessage.MessageType)] =
                    encoding.GetSchemaForBuiltInType(BuiltInType.String)
            };

            if (contentMask.HasFlag(NetworkMessageContentMask.PublisherId))
            {
                properties.Add(nameof(JsonNetworkMessage.PublisherId),
                    encoding.GetSchemaForBuiltInType(BuiltInType.String));
            }
            if (contentMask.HasFlag(NetworkMessageContentMask.DataSetClassId))
            {
                properties.Add(nameof(JsonNetworkMessage.DataSetClassId),
                    encoding.GetSchemaForBuiltInType(BuiltInType.Guid));
            }

            properties.Add(nameof(JsonNetworkMessage.DataSetWriterGroup),
                encoding.GetSchemaForBuiltInType(BuiltInType.String));

            // Now write messages - this is either one of or array of one of
            properties.Add(nameof(JsonNetworkMessage.Messages), payloadType);

            return Definitions.Reference(_options.GetSchemaId(Name), id => new JsonSchema
            {
                Id = id,
                Types = Ref == null ? new[] { SchemaType.Null } : Array.Empty<SchemaType>(),
                AdditionalProperties = new JsonSchema { Allowed = false },
                Properties = properties,
                Required = properties.Keys.ToList()

            });
        }

        /// <summary>
        /// Get name of the type
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private static string GetName(string? typeName)
        {
            // Type name of the message record
            if (string.IsNullOrEmpty(typeName))
            {
                // Type name of the message record
                typeName = nameof(JsonNetworkMessage);
            }
            else
            {
                typeName += "NetworkMessage";
            }
            return typeName;
        }

        private readonly SchemaOptions _options;
    }
}
