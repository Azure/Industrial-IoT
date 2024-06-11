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
        /// <param name="dataSetMessage"></param>
        /// <param name="networkMessageContentFlags"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        internal MonitoredItemMessageAvroSchema(PublishedDataSetMessageSchemaModel dataSetMessage,
            NetworkMessageContentFlags networkMessageContentFlags,
            SchemaOptions options, HashSet<string> uniqueNames)
        {
            DataSetSchema = new JsonDataSetAvroSchema(dataSetMessage.MetaData,
                dataSetMessage.DataSetFieldContentFlags, options, uniqueNames);
            _dataSetFieldContentMask = dataSetMessage.DataSetFieldContentFlags;
            Schema = Compile(dataSetMessage.TypeName, dataSetMessage.DataSetMessageContentFlags,
                uniqueNames, networkMessageContentFlags, options);
        }

        /// <inheritdoc/>
        protected override Schema Compile(string? typeName,
            DataSetMessageContentFlags dataSetMessageContentFlags, HashSet<string> uniqueNames,
            NetworkMessageContentFlags networkMessageContentFlags, SchemaOptions options)
        {
            if (DataSetSchema.Schema is not RecordSchema dataSetSchema)
            {
                return AvroSchema.Null;
            }

            // TODO: Events are encoded as dictionary, not single values

            // For each value in the data set, compile a message from it
            var items = new List<Schema>();
            foreach (var property in dataSetSchema.Fields)
            {
                var fields = CollectFields(dataSetMessageContentFlags, networkMessageContentFlags,
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
            DataSetMessageContentFlags dataSetMessageContentFlags,
            NetworkMessageContentFlags networkMessageContentFlags, Schema valueSchema)
        {
            var encoding = new JsonBuiltInAvroSchemas(false, true);
            var pos = 0;
            var fields = new List<Field>();

            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.NodeId))
            {
                fields.Add(
                   new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                       nameof(MonitoredItemMessage.NodeId), pos++));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.EndpointUrl))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                       nameof(MonitoredItemMessage.EndpointUrl), pos++));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.ApplicationUri))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                        nameof(MonitoredItemMessage.ApplicationUri), pos++));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.DisplayName))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                        nameof(MonitoredItemMessage.DisplayName), pos++));
            }
            if (dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.Timestamp))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.DateTime).AsNullable(),
                        nameof(MonitoredItemMessage.Timestamp), pos++));
            }
            if (dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.Status))
            {
                fields.Add(
                    new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.String).AsNullable(),
                        nameof(MonitoredItemMessage.Status), pos++));
            }

            fields.Add(new(valueSchema, nameof(MonitoredItemMessage.Value), pos++));

            if (dataSetMessageContentFlags.HasFlag(DataSetMessageContentFlags.SequenceNumber))
            {
                fields.Add(
                  new(encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.UInt32).AsNullable(),
                      nameof(MonitoredItemMessage.SequenceNumber), pos++));
            }
            if (_dataSetFieldContentMask.HasFlag(DataSetFieldContentFlags.ExtensionFields))
            {
                fields.Add(
                    new(MapSchema.CreateMap(
                        encoding.GetSchemaForBuiltInType(Opc.Ua.BuiltInType.Variant)).AsNullable(),
                        nameof(MonitoredItemMessage.ExtensionFields), pos++));
            }
            return fields;
        }

        private readonly DataSetFieldContentFlags _dataSetFieldContentMask;
    }
}
