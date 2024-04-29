﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using System.Collections.Generic;
    using Furly.Extensions.Messaging;
    using Furly;
    using Json.Schema;
    using System.Linq;
    using System;

    /// <summary>
    /// Monitored item message json schema
    /// </summary>
    public sealed class MonitoredItemMessageJsonSchema : IEventSchema
    {
        /// <inheritdoc/>
        public string Type => ContentMimeType.Json;

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
        /// Get json schema for message
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <param name="uniqueNames"></param>
        internal MonitoredItemMessageJsonSchema(DataSetWriterModel dataSetWriter,
            bool withDataSetMessageHeader, SchemaOptions options,
            Dictionary<string, JsonSchema> definitions, HashSet<string> uniqueNames)
        {
            _options = options;
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new JsonDataSetJsonSchema(dataSetWriter, options, definitions,
                uniqueNames);
            Definitions = definitions ?? new Dictionary<string, JsonSchema>();
            Name = GetName(dataSetWriter.DataSet?.Name
                ?? dataSetWriter.DataSetWriterName, uniqueNames);
            Ref = Compile(dataSetWriter.DataSet?.Name,
                dataSetWriter.MessageSettings?.DataSetMessageContentMask ?? 0u,
                uniqueNames);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return new JsonSchema
            {
                Type = Ref == null ? SchemaType.Null : SchemaType.None,
                Definitions = Definitions,
                Reference = Ref?.Reference
            }.ToJsonString();
        }

        /// <summary>
        /// Compile the message schemas into a union of messages
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="dataSetContentMask"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        private JsonSchema? Compile(string? typeName,
            DataSetContentMask dataSetContentMask, HashSet<string> uniqueNames)
        {
            // TODO: Events are encoded as dictionary, not single values

            if (_dataSet.Ref?.Reference?.Fragment == null)
            {
                return null;
            }
            // Resolve the data set schema
            var dataSetSchema = _dataSet.Ref.Resolve(_dataSet.Definitions);
            if (dataSetSchema?.Properties == null)
            {
                return null;
            }
            // Remove dataset schema from definitions
            _dataSet.Definitions.Remove(_dataSet.Ref.Reference.Fragment);

            // For each value in the data set, compile a message from it
            var items = new List<JsonSchema>();
            foreach (var property in dataSetSchema.Properties)
            {
                var name = GetName(typeName, uniqueNames);
                var valueSchema = Compile(name, dataSetContentMask, property.Value);
                if (valueSchema != null)
                {
                    items.Add(valueSchema);
                }
            }
            return items.AsUnion(_dataSet.Definitions, Name);
        }

        /// <summary>
        /// Compile the message schema for the value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataSetMessageContentMask"></param>
        /// <param name="valueSchema"></param>
        /// <returns></returns>
        private JsonSchema? Compile(string name, DataSetContentMask dataSetMessageContentMask,
            JsonSchema valueSchema)
        {
            var encoding = new JsonBuiltInJsonSchemas(true, true, Definitions);
            var properties = new Dictionary<string, JsonSchema>();

            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.NodeId))
            {
                properties.Add(nameof(MonitoredItemMessage.NodeId),
                   encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.EndpointUrl))
            {
                properties.Add(nameof(MonitoredItemMessage.EndpointUrl),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.ApplicationUri))
            {
                properties.Add(nameof(MonitoredItemMessage.ApplicationUri),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.DisplayName))
            {
                properties.Add(nameof(MonitoredItemMessage.DisplayName),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Timestamp))
            {
                properties.Add(nameof(MonitoredItemMessage.Timestamp),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.DateTime));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Status))
            {
                properties.Add(nameof(MonitoredItemMessage.Status),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }

            properties.Add(nameof(MonitoredItemMessage.Value), valueSchema);

            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.SequenceNumber))
            {
                properties.Add(nameof(MonitoredItemMessage.SequenceNumber),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.UInt32));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.ExtensionFields))
            {
                properties.Add(nameof(MonitoredItemMessage.ExtensionFields), new JsonSchema
                {
                    Type = SchemaType.Object,
                    AdditionalProperties = new JsonSchema { Allowed = true }
                });
            }
            return Definitions.Reference(_options.GetSchemaId(name), id => new JsonSchema
            {
                Id = id,
                Type = SchemaType.Object,
                AdditionalProperties = new JsonSchema { Allowed = false },
                Properties = properties
            });
        }

        /// <summary>
        /// Get name of the type
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        private static string GetName(string? typeName, HashSet<string>? uniqueNames)
        {
            // Type name of the message record
            typeName ??= string.Empty;
            typeName += nameof(MonitoredItemMessage);
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

        private readonly DataSetFieldContentMask _dataSetFieldContentMask;
        private readonly SchemaOptions _options;
        private readonly bool _withDataSetMessageHeader;
        private readonly JsonDataSetJsonSchema _dataSet;
    }
}
