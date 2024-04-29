// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Monitored item message avro schema
    /// </summary>
    public sealed class MonitoredItemMessageAvroSchema : BaseDataSetMessageAvroSchema
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
        internal MonitoredItemMessageAvroSchema(DataSetWriterModel dataSetWriter,
            NetworkMessageContentMask networkMessageContentMask,
            SchemaOptions options, HashSet<string> uniqueNames)
        {
            DataSetSchema = new JsonDataSetAvroSchema(dataSetWriter, options, uniqueNames);
            _dataSetFieldContentMask = dataSetWriter.DataSetFieldContentMask ?? 0;
            Schema = Compile(dataSetWriter.DataSet?.Name ?? dataSetWriter.DataSetWriterName,
                dataSetWriter.MessageSettings?.DataSetMessageContentMask, uniqueNames,
                networkMessageContentMask, options);
        }

        /// <inheritdoc/>
        protected override Schema Compile(string? typeName,
            DataSetContentMask? dataSetMessageContentMask, HashSet<string> uniqueNames,
            NetworkMessageContentMask networkMessageContentMask, SchemaOptions options)
        {
            var dataSetContentMask = dataSetMessageContentMask ?? default;
            if (DataSetSchema.Schema is not RecordSchema dataSetSchema)
            {
                return AvroSchema.Null;
            }

            // TODO: Events are encoded as dictionary, not single values

            // For each value in the data set, compile a message from it
            var items = new List<Schema>();
            foreach (var property in dataSetSchema.Fields)
            {
                var fields = CollectFields(dataSetContentMask, networkMessageContentMask,
                    property.Schema);
                var name = GetTypeName(typeName, uniqueNames, nameof(MonitoredItemMessage));
                var ns = options.Namespace != null ?
                    SchemaUtils.NamespaceUriToNamespace(options.Namespace) :
                    SchemaUtils.PublisherNamespace;
                items.Add(RecordSchema.Create(name, fields.ToList(), ns));
            }
            return items.AsUnion();
        }

        /// <inheritdoc/>
        protected override IEnumerable<Field> CollectFields(
            DataSetContentMask dataSetMessageContentMask,
            NetworkMessageContentMask networkMessageContentMask, Schema valueSchema)
        {
            var encoding = new JsonBuiltInAvroSchemas(false, true);
            var pos = 0;
            var fields = new List<Field>();

            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.NodeId))
            {
                fields.Add(
                   new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                       nameof(MonitoredItemMessage.NodeId), pos++));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.EndpointUrl))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                       nameof(MonitoredItemMessage.EndpointUrl), pos++));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.ApplicationUri))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                        nameof(MonitoredItemMessage.ApplicationUri), pos++));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.DisplayName))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                        nameof(MonitoredItemMessage.DisplayName), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Timestamp))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.DateTime).AsNullable(),
                        nameof(MonitoredItemMessage.Timestamp), pos++));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Status))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                        nameof(MonitoredItemMessage.Status), pos++));
            }

            fields.Add(new(valueSchema, nameof(MonitoredItemMessage.Value), pos++));

            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.SequenceNumber))
            {
                fields.Add(
                  new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.UInt32).AsNullable(),
                      nameof(MonitoredItemMessage.SequenceNumber), pos++));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentMask.ExtensionFields))
            {
                fields.Add(
                    new(MapSchema.CreateMap(
                        encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.Variant)).AsNullable(),
                        nameof(MonitoredItemMessage.ExtensionFields), pos++));
            }
            return fields;
        }

        private readonly DataSetFieldContentMask _dataSetFieldContentMask;
    }
}
