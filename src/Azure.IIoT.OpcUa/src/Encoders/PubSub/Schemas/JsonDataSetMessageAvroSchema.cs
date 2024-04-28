// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Opc.Ua;
    using DataSetFieldContentMask = Publisher.Models.DataSetFieldContentMask;
    using System.Collections.Generic;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public class JsonDataSetMessageAvroSchema : BaseDataSetMessageAvroSchema
    {
        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public JsonDataSetMessageAvroSchema(DataSetWriterModel dataSetWriter,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            bool useCompatibilityMode = false, HashSet<string>? uniqueNames = null)
            : base(dataSetWriter, new JsonDataSetAvroSchema(dataSetWriter, options, uniqueNames),
                  withDataSetMessageHeader, options, uniqueNames, useCompatibilityMode)
        {
        }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="dataSetContentMask"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        public JsonDataSetMessageAvroSchema(PublishedDataSetModel dataSet,
            DataSetContentMask? dataSetContentMask = null,
            DataSetFieldContentMask? dataSetFieldContentMask = null,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            bool useCompatibilityMode = false, HashSet<string>? uniqueNames = null)
            : base(dataSet,
                  new JsonDataSetAvroSchema(null, dataSet,
                dataSetFieldContentMask, options, uniqueNames), dataSetContentMask,
                  withDataSetMessageHeader, options, uniqueNames, useCompatibilityMode)
        {
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            DataSetContentMask dataSetMessageContentMask, bool useCompatibilityMode)
        {
            var encoding = new JsonBuiltInAvroSchemas(true, true);
            var pos = 0;
            var fields = new List<Field>();
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.DataSetWriterId))
            {
                if (!useCompatibilityMode)
                {
                    fields.Add(
                        new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt16),
                            nameof(DataSetContentMask.DataSetWriterId), pos++));
                }
                else
                {
                    // Up to version 2.8 we wrote the string id as id which is not per standard
                    fields.Add(
                        new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                            nameof(DataSetContentMask.DataSetWriterId), pos++));
                }
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.SequenceNumber))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                        nameof(DataSetContentMask.SequenceNumber), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.MetaDataVersion))
            {
                var version = RecordSchema.Create(nameof(ConfigurationVersionDataType),
                    new List<Field>
                {
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                        "MajorVersion", 0),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                        "MinorVersion", 1)
                }, SchemaUtils.NamespaceZeroName,
                    new[] { "i_" + DataTypes.ConfigurationVersionDataType });
                fields.Add(new(version, nameof(DataSetContentMask.MetaDataVersion), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Timestamp))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.DateTime),
                    nameof(DataSetContentMask.Timestamp), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Status))
            {
                if (!useCompatibilityMode)
                {
                    fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.StatusCode),
                        nameof(DataSetContentMask.Status), pos++));
                }
                else
                {
                    // Up to version 2.8 we wrote the full status code
                    fields.Add(new(new JsonBuiltInAvroSchemas(false, false)
                        .GetSchemaForBuiltInType(BuiltInType.StatusCode),
                            nameof(DataSetContentMask.Status), pos++));
                }
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.MessageType))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(DataSetContentMask.MessageType), pos++));
            }
            if (!useCompatibilityMode &&
                dataSetMessageContentMask.HasFlag(DataSetContentMask.DataSetWriterName))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(DataSetContentMask.DataSetWriterName), pos++));
            }

            fields.Add(new(DataSetSchema, nameof(JsonDataSetMessage.Payload), pos++));
            return fields;
        }
    }
}
