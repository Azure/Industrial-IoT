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
    using DataSetFieldContentMask = Publisher.Models.DataSetFieldContentMask;
    using System.Collections.Generic;

    /// <summary>
    /// Avro Dataset message avro schema
    /// </summary>
    public class AvroDataSetMessageAvroSchema : BaseDataSetMessageAvroSchema
    {
        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public AvroDataSetMessageAvroSchema(DataSetWriterModel dataSetWriter,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            HashSet<string>? uniqueNames = null)
            : base(dataSetWriter, new AvroDataSetAvroSchema(
                dataSetWriter, options, uniqueNames), withDataSetMessageHeader,
                  options, uniqueNames)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public AvroDataSetMessageAvroSchema(PublishedDataSetModel dataSet,
            DataSetFieldContentMask? dataSetFieldContentMask = null,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            HashSet<string>? uniqueNames = null)
            : base(dataSet, new AvroDataSetAvroSchema(null, dataSet,
                dataSetFieldContentMask, options, uniqueNames), null,
                  withDataSetMessageHeader, options, uniqueNames)
        {
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            DataSetContentMask dataSetMessageContentMask, bool useCompatibilityMode)
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

            var fields = new List<Field>
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

                new(DataSetSchema, nameof(AvroDataSetMessage.Payload), 7)
            };

            return fields;
        }
    }
}
