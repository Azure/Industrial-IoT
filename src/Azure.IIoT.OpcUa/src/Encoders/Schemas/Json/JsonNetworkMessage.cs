// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
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

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public sealed class JsonNetworkMessage : IEventSchema
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
        /// Compatibility with 2.8 when encoding and decoding
        /// </summary>
        public bool UseCompatibilityMode { get; }

        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="networkMessage"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="definitions"></param>
        public JsonNetworkMessage(PublishedNetworkMessageSchemaModel networkMessage,
            SchemaOptions? options = null, bool useCompatibilityMode = false,
            Dictionary<string, JsonSchema>? definitions = null)
        {
            ArgumentNullException.ThrowIfNull(networkMessage);

            UseCompatibilityMode = useCompatibilityMode;
            Definitions = definitions ?? new();
            _options = options ?? new SchemaOptions();

            Name = GetName(networkMessage.TypeName);
            Ref = Compile(networkMessage);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return new JsonSchema
            {
                Type = Ref == null ? SchemaType.Null : Ref.Type,
                Items = Ref?.Items,
                Definitions = Definitions,
                Reference = Ref?.Reference
            }.ToJsonString();
        }

        /// <summary>
        /// Compile the schema for the data sets
        /// </summary>
        /// <param name="networkMessage"></param>
        /// <returns></returns>
        private JsonSchema? Compile(PublishedNetworkMessageSchemaModel networkMessage)
        {
            var dataSetMessages = networkMessage.DataSetMessages;
            var networkMessageContentFlags = networkMessage.NetworkMessageContentFlags
                ?? PubSubMessage.DefaultNetworkMessageContentFlags;
            var MonitoredItemMessage = networkMessageContentFlags
                .HasFlag(NetworkMessageContentFlags.MonitoredItemMessage);
            if (MonitoredItemMessage)
            {
                networkMessageContentFlags &= ~NetworkMessageContentFlags.NetworkMessageHeader;
            }

            var dataSetSchemas = dataSetMessages
                .Where(dataSet => dataSet != null)
                .Select(dataSet => GetSchema(dataSet!, networkMessageContentFlags))
                .Where(r => r != null)
                .ToList();

            if (dataSetSchemas.Count == 0)
            {
                return null;
            }

            if (MonitoredItemMessage)
            {
                return CollapseUnions(dataSetSchemas);
            }

            var dataSetMessageSchemas = dataSetSchemas.Count > 1 ?
                dataSetSchemas.AsUnion(Definitions,
                    id: _options.GetSchemaId(MakeUnique("DataSets"))) :
                dataSetSchemas[0];

            var HasSingleDataSetMessage = networkMessageContentFlags
                .HasFlag(NetworkMessageContentFlags.SingleDataSetMessage);
            var payloadType = HasSingleDataSetMessage ?
                dataSetMessageSchemas : dataSetMessageSchemas.AsArray();
            if ((networkMessageContentFlags &
                ~(NetworkMessageContentFlags.SingleDataSetMessage |
                  NetworkMessageContentFlags.DataSetMessageHeader |
                  NetworkMessageContentFlags.MonitoredItemMessage)) == 0u)
            {
                // No network message header
                return payloadType;
            }

            var encoding = new JsonBuiltInSchemas(true, false, Definitions);
            var properties = new Dictionary<string, JsonSchema>
            {
                [nameof(PubSub.JsonNetworkMessage.MessageId)] =
                    encoding.GetSchemaForBuiltInType(BuiltInType.String),
                [nameof(PubSub.JsonNetworkMessage.MessageType)] =
                    encoding.GetSchemaForBuiltInType(BuiltInType.String)
            };

            if (networkMessageContentFlags.HasFlag(NetworkMessageContentFlags.PublisherId))
            {
                properties.Add(nameof(PubSub.JsonNetworkMessage.PublisherId),
                    encoding.GetSchemaForBuiltInType(BuiltInType.String));
            }
            if (networkMessageContentFlags.HasFlag(NetworkMessageContentFlags.DataSetClassId))
            {
                properties.Add(nameof(PubSub.JsonNetworkMessage.DataSetClassId),
                    encoding.GetSchemaForBuiltInType(BuiltInType.Guid));
            }

            properties.Add(nameof(PubSub.JsonNetworkMessage.DataSetWriterGroup),
                encoding.GetSchemaForBuiltInType(BuiltInType.String));

            // Now write messages - this is either one of or array of one of
            properties.Add(nameof(PubSub.JsonNetworkMessage.Messages), payloadType);

            var messageSchema = Definitions.Reference(_options.GetSchemaId(Name),
                id => new JsonSchema
                {
                    Id = id,
                    Type = Ref == null ? SchemaType.Null : SchemaType.None,
                    AdditionalProperties = new JsonSchema { Allowed = false },
                    Properties = properties,
                    Required = properties.Keys.ToList()
                });

            if (networkMessageContentFlags
                .HasFlag(NetworkMessageContentFlags.UseArrayEnvelope))
            {
                return messageSchema.AsArray(BaseNetworkMessage.MessageTypeName + "s");
            }
            return messageSchema;
        }

        /// <summary>
        /// Collapse the unions into one
        /// </summary>
        /// <param name="dataSets"></param>
        /// <returns></returns>
        private JsonSchema CollapseUnions(List<JsonSchema> dataSets)
        {
            // Collapse all unions into one
            var messages = new List<JsonSchema>();
            foreach (var dataSet in dataSets)
            {
                if (dataSet.OneOf == null)
                {
                    messages.Add(dataSet);
                    continue;
                }
                // Remove dataset schema from definitions
                messages.AddRange(dataSet.OneOf);
                if (dataSet.Reference?.Fragment != null)
                {
                    Definitions.Remove(dataSet.Reference.Fragment);
                }
            }
            return messages.AsUnion(Definitions, id: _options.GetSchemaId(
                MakeUnique(nameof(MonitoredItemMessage) + "s")));
        }

        /// <summary>
        /// Get data set message schema
        /// </summary>
        /// <param name="dataSetMessage"></param>
        /// <param name="networkMessageContentFlags"></param>
        /// <returns></returns>
        private JsonSchema GetSchema(PublishedDataSetMessageSchemaModel dataSetMessage,
            NetworkMessageContentFlags networkMessageContentFlags)
        {
            if (networkMessageContentFlags.HasFlag(NetworkMessageContentFlags.MonitoredItemMessage))
            {
                return new MonitoredItemMessage(dataSetMessage,
                    networkMessageContentFlags.HasFlag(NetworkMessageContentFlags.DataSetMessageHeader),
                    _options, Definitions, _uniqueNames).Ref!;
            }
            return new JsonDataSetMessage(dataSetMessage,
                networkMessageContentFlags.HasFlag(NetworkMessageContentFlags.DataSetMessageHeader),
                _options, Definitions, UseCompatibilityMode, _uniqueNames).Ref!;
        }

        /// <summary>
        /// Get name of the type
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private string GetName(string? typeName)
        {
            // Type name of the message record
            typeName ??= string.Empty;
            typeName += BaseNetworkMessage.MessageTypeName;
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
