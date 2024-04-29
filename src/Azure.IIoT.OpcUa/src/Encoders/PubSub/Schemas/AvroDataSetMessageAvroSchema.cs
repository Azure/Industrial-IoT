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
    /// Avro Dataset message avro schema
    /// </summary>
    public sealed class AvroDataSetMessageAvroSchema : BaseDataSetMessageAvroSchema
    {
        /// <inheritdoc/>
        public override Schema Schema { get; }

        /// <inheritdoc/>
        protected override BaseDataSetSchema<Schema> DataSetSchema { get; }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        internal AvroDataSetMessageAvroSchema(DataSetWriterModel dataSetWriter,
            NetworkMessageContentMask networkMessageContentMask,
            SchemaOptions options, HashSet<string> uniqueNames)
        {
            DataSetSchema = new AvroDataSetAvroSchema(dataSetWriter, options, uniqueNames);
            Schema = Compile(dataSetWriter.DataSet?.Name ?? dataSetWriter.DataSetWriterName,
                dataSetWriter.MessageSettings?.DataSetMessageContentMask, uniqueNames,
                networkMessageContentMask, options);
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            DataSetContentMask dataSetMessageContentMask,
            NetworkMessageContentMask networkMessageContentMask, Schema valueSchema)
        {
            var encoding = new AvroBuiltInAvroSchemas();
            var version = RecordSchema.Create(nameof(ConfigurationVersionDataType),
                new List<Field>
            {
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    "MajorVersion", 0),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    "MinorVersion", 1)
            }, SchemaUtils.NamespaceZeroName,
                new[] { "i_" + DataTypes.ConfigurationVersionDataType });

            return new List<Field>
            {
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(DataSetContentMask.MessageType), 0),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                    nameof(DataSetContentMask.DataSetWriterName), 1),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt16),
                    nameof(DataSetContentMask.DataSetWriterId), 2),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                    nameof(DataSetContentMask.SequenceNumber), 3),
                new(version,
                    nameof(DataSetContentMask.MetaDataVersion), 4),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.DateTime),
                    nameof(DataSetContentMask.Timestamp), 5),
                new(encoding.GetSchemaForBuiltInType(BuiltInType.StatusCode),
                    nameof(DataSetContentMask.Status), 6),

                new(valueSchema, nameof(AvroDataSetMessage.Payload), 7)
            };
        }
    }
}
