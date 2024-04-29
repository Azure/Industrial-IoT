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
    using System;

    /// <summary>
    /// Json Network message avro schema
    /// </summary>
    public sealed class JsonNetworkMessageAvroSchema : BaseNetworkMessageAvroSchema
    {
        /// <inheritdoc/>
        public override Schema Schema { get; }

        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="useArrayEnvelope"></param>
        /// <returns></returns>
        public JsonNetworkMessageAvroSchema(WriterGroupModel writerGroup,
            SchemaOptions? options = null, bool useCompatibilityMode = false,
            bool useArrayEnvelope = false)
        {
            Schema = Compile(writerGroup.Name,
                writerGroup.DataSetWriters ?? Array.Empty<DataSetWriterModel>(),
                GetMask(writerGroup.MessageSettings?.NetworkMessageContentMask,
                    useCompatibilityMode, useArrayEnvelope), options);
        }

        /// <inheritdoc/>
        protected override Schema GetDataSetMessageSchema(DataSetWriterModel writer,
            NetworkMessageContentMask contentMask, SchemaOptions options,
            HashSet<string> uniqueNames)
        {
            if (contentMask.HasFlag(NetworkMessageContentMask.MonitoredItemMessage))
            {
                return new MonitoredItemMessageAvroSchema(writer, contentMask,
                    options, uniqueNames).Schema;
            }
            return new JsonDataSetMessageAvroSchema(writer, contentMask, options,
                uniqueNames).Schema;
        }

        /// <inheritdoc/>
        protected override Schema Compile(string? typeName,
            IEnumerable<DataSetWriterModel> dataSetWriters,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaOptions? options)
        {
            var messageSchema = base.Compile(typeName, dataSetWriters,
                networkMessageContentMask, options);

            if (networkMessageContentMask.HasValue && networkMessageContentMask
                .Value.HasFlag(NetworkMessageContentMask.UseArrayEnvelope))
            {
                // set array as root
                return messageSchema.AsArray(true);
            }
            return messageSchema;
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            NetworkMessageContentMask contentMask, Schema? payloadType)
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

            if (contentMask.HasFlag(NetworkMessageContentMask.PublisherId))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(JsonNetworkMessage.PublisherId), pos++));
            }
            if (contentMask.HasFlag(NetworkMessageContentMask.DataSetClassId))
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
        /// Update content maske
        /// </summary>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="useArrayEnvelope"></param>
        /// <returns></returns>
        private static NetworkMessageContentMask GetMask(
            NetworkMessageContentMask? networkMessageContentMask,
            bool useCompatibilityMode, bool useArrayEnvelope)
        {
            var newNetworkMessageContentMask = networkMessageContentMask ?? default;
            if (useCompatibilityMode)
            {
                newNetworkMessageContentMask |= NetworkMessageContentMask.UseCompatibilityMode;
            }
            if (useArrayEnvelope)
            {
                newNetworkMessageContentMask |= NetworkMessageContentMask.UseArrayEnvelope;
            }
            return newNetworkMessageContentMask;
        }
    }
}
