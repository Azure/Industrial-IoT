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
    /// Json Network message avro schema
    /// </summary>
    public class JsonNetworkMessageAvroSchema : BaseNetworkMessageAvroSchema
    {
        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        public JsonNetworkMessageAvroSchema(WriterGroupModel writerGroup,
            SchemaOptions? options = null, bool useCompatibilityMode = false)
            : this(writerGroup.DataSetWriters!, writerGroup.Name,
                  writerGroup.MessageSettings?.NetworkMessageContentMask,
                  options, useCompatibilityMode)
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
        /// <returns></returns>
        public JsonNetworkMessageAvroSchema(DataSetWriterModel dataSetWriter,
            string? name = null,
            NetworkMessageContentMask? networkMessageContentMask = null,
            SchemaOptions? options = null, bool useCompatibilityMode = false)
            : this(dataSetWriter.YieldReturn(), name,
                  networkMessageContentMask, options, useCompatibilityMode)
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
        /// <returns></returns>
        internal JsonNetworkMessageAvroSchema(
            IEnumerable<DataSetWriterModel> dataSetWriters, string? name,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaOptions? options, bool useCompatibilityMode) :
            base(dataSetWriters, name, networkMessageContentMask, options,
                useCompatibilityMode)
        {
        }

        /// <inheritdoc/>
        protected override Schema GetDataSetSchema(DataSetWriterModel writer,
            bool hasDataSetMessageHeader, SchemaOptions options,
            HashSet<string> uniqueNames, bool useCompatibilityMode)
        {
            return new JsonDataSetMessageAvroSchema(writer, hasDataSetMessageHeader,
                options, useCompatibilityMode, uniqueNames).Schema;
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
    }
}
