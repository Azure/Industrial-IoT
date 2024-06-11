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
    using Opc.Ua;
    using System.Collections.Generic;

    /// <summary>
    /// Json Network message avro schema
    /// </summary>
    public sealed class JsonNetworkMessageAvroSchema : BaseNetworkMessageAvroSchema
    {
        /// <inheritdoc/>
        public override Schema Schema { get; }

        /// <summary>
        /// Get avro schema for a network message
        /// </summary>
        /// <param name="networkMessage"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="useArrayEnvelope"></param>
        public JsonNetworkMessageAvroSchema(PublishedNetworkMessageSchemaModel networkMessage,
            SchemaOptions? options = null, bool useCompatibilityMode = false, bool useArrayEnvelope = false)
        {
            networkMessage = SetAdditionalFlags(networkMessage, useCompatibilityMode, useArrayEnvelope);
            Schema = Compile(networkMessage, options);
        }

        /// <inheritdoc/>
        protected override Schema Compile(PublishedDataSetMessageSchemaModel dataSetMessage,
            NetworkMessageContentFlags networkMessageContentFlags, SchemaOptions options,
            HashSet<string> uniqueNames)
        {
            if (networkMessageContentFlags.HasFlag(NetworkMessageContentFlags.MonitoredItemMessage))
            {
                return new MonitoredItemMessageAvroSchema(dataSetMessage, networkMessageContentFlags,
                    options, uniqueNames).Schema;
            }
            return new JsonDataSetMessageAvroSchema(dataSetMessage, networkMessageContentFlags,
                options, uniqueNames).Schema;
        }

        /// <inheritdoc/>
        protected override Schema Compile(PublishedNetworkMessageSchemaModel networkMessage, SchemaOptions? options)
        {
            var messageSchema = base.Compile(networkMessage, options);

            if (networkMessage.NetworkMessageContentFlags.HasFlag(NetworkMessageContentFlags.UseArrayEnvelope))
            {
                // set array as root
                return messageSchema.AsArray(true);
            }
            return messageSchema;
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            NetworkMessageContentFlags contentMask, Schema? payloadType)
        {
            var encoding = new JsonBuiltInAvroSchemas(true, false);
            var pos = 0;
            var fields = new List<Field>
            {
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(JsonNetworkMessage.MessageId), pos++),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(JsonNetworkMessage.MessageType), pos++)
            };

            if (contentMask.HasFlag(NetworkMessageContentFlags.PublisherId))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(JsonNetworkMessage.PublisherId), pos++));
            }
            if (contentMask.HasFlag(NetworkMessageContentFlags.DataSetClassId))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.Guid),
                    nameof(JsonNetworkMessage.DataSetClassId), pos++));
            }

            fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                nameof(JsonNetworkMessage.DataSetWriterGroup), pos++));

            // Now write messages - this is either one of or array of one of
            fields.Add(new(payloadType, nameof(JsonNetworkMessage.Messages), pos++));
            return fields;
        }

        /// <summary>
        /// Set additional flags
        /// </summary>
        /// <param name="networkMessage"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="useArrayEnvelope"></param>
        /// <returns></returns>
        private static PublishedNetworkMessageSchemaModel SetAdditionalFlags(
            PublishedNetworkMessageSchemaModel networkMessage,
            bool useCompatibilityMode, bool useArrayEnvelope)
        {
            var networkMessageContentFlags = networkMessage.NetworkMessageContentFlags;
            if (useCompatibilityMode)
            {
                networkMessageContentFlags |= NetworkMessageContentFlags.UseCompatibilityMode;
            }
            if (useArrayEnvelope)
            {
                networkMessageContentFlags |= NetworkMessageContentFlags.UseArrayEnvelope;
            }
            return networkMessage with { NetworkMessageContentFlags = networkMessageContentFlags };
        }
    }
}
