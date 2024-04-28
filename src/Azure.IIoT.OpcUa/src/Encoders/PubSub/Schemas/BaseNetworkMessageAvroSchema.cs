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
    using Furly;
    using Furly.Extensions.Messaging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Network message avro schema
    /// </summary>
    public abstract class BaseNetworkMessageAvroSchema : IEventSchema
    {
        /// <inheritdoc/>
        public string Type => ContentMimeType.AvroSchema;

        /// <inheritdoc/>
        public string Name => Schema.Fullname;

        /// <inheritdoc/>
        public ulong Version { get; }

        /// <inheritdoc/>
        string IEventSchema.Schema => Schema.ToString();

        /// <inheritdoc/>
        public string? Id => SchemaNormalization
            .ParsingFingerprint64(Schema)
            .ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// The actual schema
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// Get avro schema for a dataset encoded in json
        /// </summary>
        /// <param name="dataSetWriters"></param>
        /// <param name="name"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        protected BaseNetworkMessageAvroSchema(
            IEnumerable<DataSetWriterModel> dataSetWriters, string? name,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaOptions? options, bool useCompatibilityMode = false)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriters);

            _options = options ?? new SchemaOptions();

            Schema = Compile(name, dataSetWriters
                .Where(writer => writer.DataSet != null)
                .ToList(), networkMessageContentMask ?? 0u, useCompatibilityMode);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToString();
        }

        /// <summary>
        /// Compile the schema for the data sets
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="dataSetWriters"></param>
        /// <param name="contentMask"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        private Schema Compile(string? typeName, List<DataSetWriterModel> dataSetWriters,
            NetworkMessageContentMask contentMask, bool useCompatibilityMode)
        {
            var HasDataSetMessageHeader = contentMask
                .HasFlag(NetworkMessageContentMask.DataSetMessageHeader);
            var HasNetworkMessageHeader = contentMask
                .HasFlag(NetworkMessageContentMask.NetworkMessageHeader);

            var dataSetMessageSchemas = dataSetWriters
                .Where(writer => writer.DataSet != null)
                .OrderBy(writer => writer.DataSetWriterId)
                .Select(writer => (writer.DataSetWriterId,
                    Schema: GetDataSetSchema(writer, HasDataSetMessageHeader,
                        _options, _uniqueNames, useCompatibilityMode)))
                .ToList();

            if (dataSetMessageSchemas.Count == 0)
            {
                return AvroSchema.Null;
            }

            Schema? payloadType;
            if (dataSetMessageSchemas.Count > 1)
            {
                // Use the index of the data set writer as union index
                var length = dataSetMessageSchemas.Max(i => i.DataSetWriterId) + 1;
                Debug.Assert(length < ushort.MaxValue);
                var unionSchemas = Enumerable.Range(0, length)
                    .Select(i => (Schema)AvroSchema.CreatePlaceHolder(
                        "Empty" + i, SchemaUtils.PublisherNamespace))
                    .ToList();
                dataSetMessageSchemas
                    .ForEach(kv => unionSchemas[kv.DataSetWriterId] = kv.Schema);
                payloadType = AvroSchema.CreateUnion(unionSchemas);
            }
            else
            {
                payloadType = dataSetMessageSchemas[0].Schema;
            }

            var HasSingleDataSetMessage = contentMask
                .HasFlag(NetworkMessageContentMask.SingleDataSetMessage);
            if (!HasNetworkMessageHeader && HasSingleDataSetMessage)
            {
                // No network message header
                return payloadType;
            }

            payloadType = ArraySchema.Create(payloadType);
            var fields = CollectFields(contentMask, payloadType);

            var ns = _options.Namespace != null ?
                SchemaUtils.NamespaceUriToNamespace(_options.Namespace) :
                SchemaUtils.PublisherNamespace;
            return RecordSchema.Create(GetName(typeName), fields.ToList(), ns);
        }

        /// <summary>
        /// Collect fields
        /// </summary>
        /// <param name="contentMask"></param>
        /// <param name="payloadType"></param>
        /// <returns></returns>
        protected abstract IEnumerable<Field> CollectFields(
            NetworkMessageContentMask contentMask, Schema? payloadType);

        /// <summary>
        /// Get schema
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="hasDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        protected abstract Schema GetDataSetSchema(DataSetWriterModel writer,
            bool hasDataSetMessageHeader, SchemaOptions options,
            HashSet<string> uniqueNames, bool useCompatibilityMode);

        /// <summary>
        /// Get name of the type
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private string GetName(string? typeName)
        {
            // Type name of the message record
            if (string.IsNullOrEmpty(typeName))
            {
                // Type name of the message record
                typeName = "NetworkMessage";
            }
            else
            {
                typeName = SchemaUtils.Escape(typeName) + "NetworkMessage";
            }
            return MakeUnique(typeName);
        }

        /// <summary>
        /// Make unique
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string MakeUnique(string name)
        {
            var uniqueName = name;
            for (var index = 1; _uniqueNames.Contains(uniqueName); index++)
            {
                uniqueName = name + index;
            }
            _uniqueNames.Add(uniqueName);
            return uniqueName;
        }

        private readonly SchemaOptions _options;
        private readonly HashSet<string> _uniqueNames = new();
    }
}
