﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Messaging;
    using Json.Schema;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public sealed class JsonNetworkMessageJsonSchema : IEventSchema
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
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        public JsonNetworkMessageJsonSchema(WriterGroupModel writerGroup,
            SchemaOptions? options = null, bool useCompatibilityMode = false,
            Dictionary<string, JsonSchema>? definitions = null)
            : this(writerGroup.DataSetWriters!, writerGroup.Name,
                  writerGroup.MessageSettings?.NetworkMessageContentMask,
                  options, useCompatibilityMode, definitions)
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
        /// <param name="definitions"></param>
        /// <returns></returns>
        public JsonNetworkMessageJsonSchema(DataSetWriterModel dataSetWriter,
            string? name = null,
            NetworkMessageContentMask? networkMessageContentMask = null,
            SchemaOptions? options = null, bool useCompatibilityMode = false,
            Dictionary<string, JsonSchema>? definitions = null)
            : this(dataSetWriter.YieldReturn(), name,
                  networkMessageContentMask, options, useCompatibilityMode, definitions)
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
        /// <param name="definitions"></param>
        /// <returns></returns>
        internal JsonNetworkMessageJsonSchema(
            IEnumerable<DataSetWriterModel> dataSetWriters, string? name,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaOptions? options, bool useCompatibilityMode,
            Dictionary<string, JsonSchema>? definitions)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriters);

            UseCompatibilityMode = useCompatibilityMode;
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
                Type = Ref == null ? SchemaType.Null : Ref.Type,
                Items = Ref?.Items,
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
            var HasDataSetMessageHeader = contentMask
                .HasFlag(NetworkMessageContentMask.DataSetMessageHeader);
            var HasNetworkMessageHeader = contentMask
                .HasFlag(NetworkMessageContentMask.NetworkMessageHeader);

            var dataSets = dataSetWriters
                .Where(writer => writer.DataSet != null)
                .Select(writer => 
					new JsonDataSetMessageJsonSchema(writer, HasDataSetMessageHeader,
                        _options, Definitions, UseCompatibilityMode, _uniqueNames).Ref!)
                .Where(r => r != null)
                .ToList();

            if (dataSets.Count == 0)
            {
                return null;
            }

            var dataSetMessages = dataSets.Count > 1 ?
                dataSets.AsUnion(Definitions, id: _options.GetSchemaId(MakeUnique("DataSets"))) :
                dataSets[0];

            var HasSingleDataSetMessage = contentMask
                .HasFlag(NetworkMessageContentMask.SingleDataSetMessage);
            var payloadType = HasSingleDataSetMessage ?
                dataSetMessages : dataSetMessages.AsArray();
            if ((contentMask &
                ~(NetworkMessageContentMask.SingleDataSetMessage |
                  NetworkMessageContentMask.DataSetMessageHeader)) == 0u)
            {
                // No network message header
                return payloadType;
            }

            var encoding = new JsonBuiltInJsonSchemas(true, false, Definitions);
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
                Type = Ref == null ? SchemaType.Null : SchemaType.None,
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
                typeName += "NetworkMessage";
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
