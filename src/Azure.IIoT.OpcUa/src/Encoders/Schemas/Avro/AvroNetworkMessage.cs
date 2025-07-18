// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Avro
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using global::Avro;
    using Opc.Ua;
    using System.Collections.Generic;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public sealed class AvroNetworkMessage : BaseNetworkMessage
    {
        /// <inheritdoc/>
        public override Schema Schema { get; }

        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="networkMessage"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public AvroNetworkMessage(PublishedNetworkMessageSchemaModel networkMessage,
            SchemaOptions? options = null) : base(networkMessage.Id, networkMessage.Version)
        {
            Schema = Compile(networkMessage, options);
        }

        /// <inheritdoc/>
        protected override Schema Compile(PublishedDataSetMessageSchemaModel dataSetMessage,
            NetworkMessageContentFlags networkMessageContentFlags,
            SchemaOptions options, HashSet<string> uniqueNames)
        {
            return new AvroDataSetMessage(dataSetMessage, networkMessageContentFlags, options,
                uniqueNames).Schema;
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            NetworkMessageContentFlags contentMask, Schema payloadType)
        {
            var HasNetworkMessageHeader = contentMask
                .HasFlag(NetworkMessageContentFlags.NetworkMessageHeader);

            var encoding = new AvroBuiltInSchemas();
            return HasNetworkMessageHeader ?
                [
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(PubSub.AvroNetworkMessage.MessageId), 0),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(PubSub.AvroNetworkMessage.MessageType), 1),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(PubSub.AvroNetworkMessage.PublisherId), 2),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.Guid),
                        nameof(PubSub.AvroNetworkMessage.DataSetClassId), 3),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(PubSub.AvroNetworkMessage.DataSetWriterGroup), 4),

                    new(payloadType, nameof(PubSub.AvroNetworkMessage.Messages), 5)
                ] :
                [
                    new(payloadType, nameof(PubSub.AvroNetworkMessage.Messages), 0)
                ];
        }
    }
}
