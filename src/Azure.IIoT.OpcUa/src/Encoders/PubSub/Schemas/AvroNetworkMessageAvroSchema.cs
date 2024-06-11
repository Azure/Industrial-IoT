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
    /// Network message avro schema
    /// </summary>
    public sealed class AvroNetworkMessageAvroSchema : BaseNetworkMessageAvroSchema
    {
        /// <inheritdoc/>
        public override Schema Schema { get; }

        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="networkMessage"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public AvroNetworkMessageAvroSchema(PublishedNetworkMessageSchemaModel networkMessage,
            SchemaOptions? options = null)
        {
            Schema = Compile(networkMessage, options);
        }

        /// <inheritdoc/>
        protected override Schema Compile(PublishedDataSetMessageSchemaModel dataSetMessage,
            NetworkMessageContentFlags networkMessageContentFlags,
            SchemaOptions options, HashSet<string> uniqueNames)
        {
            return new AvroDataSetMessageAvroSchema(dataSetMessage, networkMessageContentFlags, options,
                uniqueNames).Schema;
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            NetworkMessageContentFlags contentMask, Schema? payloadType)
        {
            var HasNetworkMessageHeader = contentMask
                .HasFlag(NetworkMessageContentFlags.NetworkMessageHeader);

            var encoding = new AvroBuiltInAvroSchemas();
            return HasNetworkMessageHeader ?
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
        }
    }
}
