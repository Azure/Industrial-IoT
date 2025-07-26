// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Avro
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Messaging;
    using global::Avro;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public abstract class BaseNetworkMessage : IEventSchema, IAvroSchema
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
        public string Id { get; }

        /// <inheritdoc/>
        public abstract Schema Schema { get; }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToString();
        }

        /// <summary>
        /// Create network message schema
        /// </summary>
        /// <param name="id"></param>
        /// <param name="version"></param>
        protected BaseNetworkMessage(string id, ulong version)
        {
            Version = version;
            Id = id;
        }

        /// <summary>
        /// Compile the schema for the data sets
        /// </summary>
        /// <param name="networkMessage"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        protected virtual Schema Compile(PublishedNetworkMessageSchemaModel networkMessage,
            SchemaOptions? options = null)
        {
            options ??= new SchemaOptions();
            var networkMessageContentFlags = networkMessage.NetworkMessageContentFlags
                    ?? PubSubMessage.DefaultNetworkMessageContentFlags;
            var dataSetMessages = networkMessage.DataSetMessages;
            var typeName = networkMessage.TypeName;
            var MonitoredItemMessage = networkMessageContentFlags
                .HasFlag(NetworkMessageContentFlags.MonitoredItemMessage);
            if (MonitoredItemMessage)
            {
                networkMessageContentFlags &= ~NetworkMessageContentFlags.NetworkMessageHeader;
            }

            var dataSetMessageSchemas = dataSetMessages
                .Select((dataSet, i) => dataSet != null ?
                    Compile(dataSet, networkMessageContentFlags, options, _uniqueNames) :
                    AvroSchema.CreatePlaceHolder("Empty" + i, SchemaUtils.PublisherNamespace))
                .ToList();

            if (dataSetMessageSchemas.Count == 0)
            {
                return AvroSchema.Null;
            }

            Schema? payloadType;
            if (networkMessageContentFlags.HasFlag(NetworkMessageContentFlags.MonitoredItemMessage))
            {
                return dataSetMessageSchemas
                    .SelectMany(kv => kv is UnionSchema u ? u.Schemas : kv.YieldReturn())
                    .AsUnion();
            }

            if (dataSetMessageSchemas.Count > 1)
            {
                payloadType = dataSetMessageSchemas.AsUnion();
            }
            else
            {
                payloadType = dataSetMessageSchemas[0];
            }

            var HasSingleDataSetMessage = networkMessageContentFlags
                .HasFlag(NetworkMessageContentFlags.SingleDataSetMessage);
            var HasNetworkMessageHeader = networkMessageContentFlags
                .HasFlag(NetworkMessageContentFlags.NetworkMessageHeader);
            if (!HasNetworkMessageHeader && HasSingleDataSetMessage)
            {
                // No network message header
                return payloadType;
            }

            payloadType = payloadType.AsArray();
            var fields = CollectFields(networkMessageContentFlags, payloadType);

            var ns = options.Namespace != null ?
                SchemaUtils.NamespaceUriToNamespace(options.Namespace) :
                SchemaUtils.PublisherNamespace;
            return RecordSchema.Create(GetName(typeName), fields.ToList(), ns);
        }

        /// <summary>
        /// Collect fields
        /// </summary>
        /// <param name="contentMask"></param>
        /// <param name="payloadType"></param>
        /// <returns></returns>
        protected abstract IEnumerable<Field> CollectFields(
            NetworkMessageContentFlags contentMask, Schema payloadType);

        /// <summary>
        /// Get schema
        /// </summary>
        /// <param name="dataSetMessage"></param>
        /// <param name="networkMessageContentFlags"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        protected abstract Schema Compile(PublishedDataSetMessageSchemaModel dataSetMessage,
            NetworkMessageContentFlags networkMessageContentFlags, SchemaOptions options,
            HashSet<string> uniqueNames);

        /// <summary>
        /// Get name of the type
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private string GetName(string? typeName)
        {
            // Type name of the message record
            typeName ??= string.Empty;
            typeName = SchemaUtils.Escape(typeName) + PubSub.BaseNetworkMessage.MessageTypeName;
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

        private readonly HashSet<string> _uniqueNames = [];
    }
}
