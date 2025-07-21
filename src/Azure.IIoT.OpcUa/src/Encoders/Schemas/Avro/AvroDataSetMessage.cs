// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Avro
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using global::Avro;
    using Opc.Ua;
    using System.Collections.Generic;

    /// <summary>
    /// Avro Dataset message avro schema
    /// </summary>
    public sealed class AvroDataSetMessage : BaseDataSetMessage
    {
        /// <inheritdoc/>
        public override Schema Schema { get; }

        /// <inheritdoc/>
        protected override BaseDataSetSchema<Schema> DataSetSchema { get; }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSetMessage"></param>
        /// <param name="networkMessageContentFlags"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        internal AvroDataSetMessage(PublishedDataSetMessageSchemaModel dataSetMessage,
            NetworkMessageContentFlags networkMessageContentFlags,
            SchemaOptions options, HashSet<string> uniqueNames) : base(dataSetMessage.Id)
        {
            DataSetSchema = new AvroDataSet(dataSetMessage.Id, dataSetMessage.MetaData,
                dataSetMessage.DataSetFieldContentFlags, options, uniqueNames);
            Schema = Compile(dataSetMessage.TypeName, dataSetMessage.DataSetMessageContentFlags
                    ?? PubSubMessage.DefaultDataSetMessageContentFlags,
                uniqueNames, networkMessageContentFlags, options);
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            DataSetMessageContentFlags dataSetMessageContentFlags,
            NetworkMessageContentFlags networkMessageContentFlags, Schema valueSchema)
        {
            var encoding = new AvroBuiltInSchemas();
            var version = RecordSchema.Create(nameof(ConfigurationVersionDataType),
                [
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    "MajorVersion", 0),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    "MinorVersion", 1)
            ], SchemaUtils.NamespaceZeroName,
                new[] { "i_" + DataTypes.ConfigurationVersionDataType });

            return new List<Field>
            {
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(DataSetMessageContentFlags.MessageType), 0),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(DataSetMessageContentFlags.DataSetWriterName), 1),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt16),
                    nameof(DataSetMessageContentFlags.DataSetWriterId), 2),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    nameof(DataSetMessageContentFlags.SequenceNumber), 3),
                new(version,
                    nameof(DataSetMessageContentFlags.MetaDataVersion), 4),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.DateTime),
                    nameof(DataSetMessageContentFlags.Timestamp), 5),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.StatusCode),
                    nameof(DataSetMessageContentFlags.Status), 6),

                new(valueSchema, nameof(PubSub.AvroDataSetMessage.Payload), 7)
            };
        }
    }
}
