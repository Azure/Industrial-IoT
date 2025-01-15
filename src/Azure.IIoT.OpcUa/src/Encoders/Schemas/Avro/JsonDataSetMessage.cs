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
    /// Dataset message avro schema
    /// </summary>
    public sealed class JsonDataSetMessage : BaseDataSetMessage
    {
        /// <inheritdoc/>
        public override Schema Schema { get; }

        /// <inheritdoc/>
        protected override BaseDataSetSchema<Schema> DataSetSchema { get; }

        /// <summary>
        /// Get avro schema for a writer
        /// </summary>
        /// <param name="dataSetMessage"></param>
        /// <param name="networkMessageContentFlags"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        internal JsonDataSetMessage(PublishedDataSetMessageSchemaModel dataSetMessage,
            NetworkMessageContentFlags networkMessageContentFlags,
            SchemaOptions options, HashSet<string> uniqueNames)
        {
            DataSetSchema = new JsonDataSet(dataSetMessage.MetaData,
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
            var useCompatibilityMode = networkMessageContentFlags
                .HasFlag(NetworkMessageContentFlags.UseCompatibilityMode);

            var encoding = new JsonBuiltInSchemas(true, true);
            var pos = 0;
            var fields = new List<Field>();
            if (dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.DataSetWriterId))
            {
                if (!useCompatibilityMode)
                {
                    fields.Add(
                        new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt16),
                            nameof(DataSetMessageContentFlags.DataSetWriterId), pos++));
                }
                else
                {
                    // Up to version 2.8 we wrote the string id as id which is not per standard
                    fields.Add(
                        new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                            nameof(DataSetMessageContentFlags.DataSetWriterId), pos++));
                }
            }
            if (dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.SequenceNumber))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                        nameof(DataSetMessageContentFlags.SequenceNumber), pos++));
            }
            if (dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.MetaDataVersion))
            {
                var version = RecordSchema.Create(nameof(ConfigurationVersionDataType),
                    [
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                        "MajorVersion", 0),
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                        "MinorVersion", 1)
                ], SchemaUtils.NamespaceZeroName,
                    new[] { "i_" + DataTypes.ConfigurationVersionDataType });
                fields.Add(new(version, nameof(DataSetMessageContentFlags.MetaDataVersion), pos++));
            }
            if (dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.Timestamp))
            {
                fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.DateTime),
                    nameof(DataSetMessageContentFlags.Timestamp), pos++));
            }
            if (dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.Status))
            {
                if (!useCompatibilityMode)
                {
                    fields.Add(new(encoding.GetSchemaForBuiltInType(BuiltInType.StatusCode),
                        nameof(DataSetMessageContentFlags.Status), pos++));
                }
                else
                {
                    // Up to version 2.8 we wrote the full status code
                    fields.Add(new(new JsonBuiltInSchemas(false, false)
                        .GetSchemaForBuiltInType(BuiltInType.StatusCode),
                            nameof(DataSetMessageContentFlags.Status), pos++));
                }
            }
            if (dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.MessageType))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(DataSetMessageContentFlags.MessageType), pos++));
            }
            if (!useCompatibilityMode &&
                dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.DataSetWriterName))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(BuiltInType.String),
                        nameof(DataSetMessageContentFlags.DataSetWriterName), pos++));
            }

            fields.Add(new(valueSchema, nameof(PubSub.JsonDataSetMessage.Payload), pos++));
            return fields;
        }
    }
}
