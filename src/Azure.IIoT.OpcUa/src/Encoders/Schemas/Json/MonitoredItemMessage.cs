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
    using System.Collections.Generic;

    /// <summary>
    /// Monitored item message json schema
    /// </summary>
    public sealed class MonitoredItemMessage : IEventSchema
    {
        /// <inheritdoc/>
        public string Type => ContentMimeType.Json;

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public ulong Version { get; }

        /// <inheritdoc/>
        public string Id { get; }

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
        /// Get json schema for monitored item message
        /// </summary>
        /// <param name="dataSetMessage"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <param name="uniqueNames"></param>
        internal MonitoredItemMessage(PublishedDataSetMessageSchemaModel dataSetMessage,
            bool withDataSetMessageHeader, SchemaOptions options,
            Dictionary<string, JsonSchema> definitions, HashSet<string> uniqueNames)
        {
            _options = options;
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new JsonDataSet(dataSetMessage.Id, dataSetMessage.MetaData,
                dataSetMessage.DataSetFieldContentFlags, options, definitions, uniqueNames);
            Definitions = definitions ?? [];
            Id = dataSetMessage.Id;
            Name = GetName(dataSetMessage.TypeName, uniqueNames);
            _dataSetFieldContentMask = dataSetMessage.DataSetFieldContentFlags
                    ?? PubSubMessage.DefaultDataSetFieldContentFlags;
            Ref = Compile(dataSetMessage.TypeName, dataSetMessage.DataSetMessageContentFlags
                    ?? PubSubMessage.DefaultDataSetMessageContentFlags, uniqueNames);
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
            DataSetMessageContentFlags dataSetContentMask, HashSet<string> uniqueNames)
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
        private JsonSchema? Compile(string name, DataSetMessageContentFlags dataSetMessageContentMask,
            JsonSchema valueSchema)
        {
            var encoding = new JsonBuiltInSchemas(true, true, Definitions);
            var properties = new Dictionary<string, JsonSchema>();

            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.NodeId))
            {
                properties.Add(nameof(PubSub.MonitoredItemMessage.NodeId),
                   encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.EndpointUrl))
            {
                properties.Add(nameof(PubSub.MonitoredItemMessage.EndpointUrl),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.ApplicationUri))
            {
                properties.Add(nameof(PubSub.MonitoredItemMessage.ApplicationUri),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.DisplayName))
            {
                properties.Add(nameof(PubSub.MonitoredItemMessage.DisplayName),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Timestamp))
            {
                properties.Add(nameof(PubSub.MonitoredItemMessage.Timestamp),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.DateTime));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Status))
            {
                properties.Add(nameof(PubSub.MonitoredItemMessage.Status),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String));
            }

            properties.Add(nameof(PubSub.MonitoredItemMessage.Value), valueSchema);

            if (dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.SequenceNumber))
            {
                properties.Add(nameof(PubSub.MonitoredItemMessage.SequenceNumber),
                    encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.UInt32));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.ExtensionFields))
            {
                properties.Add(nameof(PubSub.MonitoredItemMessage.ExtensionFields), new JsonSchema
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
            typeName += nameof(PubSub.MonitoredItemMessage);
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

        private readonly DataSetFieldContentFlags _dataSetFieldContentMask;
        private readonly SchemaOptions _options;
        private readonly bool _withDataSetMessageHeader;
        private readonly JsonDataSet _dataSet;
    }
}
