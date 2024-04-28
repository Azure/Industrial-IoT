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
    using System.Linq;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public class AvroNetworkMessageAvroSchema : BaseNetworkMessageAvroSchema
    {
        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public AvroNetworkMessageAvroSchema(WriterGroupModel writerGroup,
            SchemaOptions? options = null)
            : this(writerGroup.DataSetWriters!, writerGroup.Name,
                  writerGroup.MessageSettings?.NetworkMessageContentMask,
                  options)
        {
        }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="name"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public AvroNetworkMessageAvroSchema(DataSetWriterModel dataSetWriter,
            string? name = null,
            NetworkMessageContentMask? networkMessageContentMask = null,
            SchemaOptions? options = null)
            : this(dataSetWriter.YieldReturn(), name,
                  networkMessageContentMask, options)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="name"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal AvroNetworkMessageAvroSchema(
            IEnumerable<DataSetWriterModel> dataSetWriters, string? name,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaOptions? options)
            : base(dataSetWriters, name, networkMessageContentMask, options)
        {
        }

        /// <inheritdoc/>
        protected override Schema GetDataSetSchema(DataSetWriterModel writer,
            bool hasDataSetMessageHeader, SchemaOptions options,
            HashSet<string> uniqueNames, bool useCompatibilityMode)
        {
            return new AvroDataSetMessageAvroSchema(writer, hasDataSetMessageHeader,
            options, uniqueNames).Schema;
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            NetworkMessageContentMask contentMask, Schema? payloadType)
        {
            var HasNetworkMessageHeader = contentMask
                .HasFlag(NetworkMessageContentMask.NetworkMessageHeader);

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
