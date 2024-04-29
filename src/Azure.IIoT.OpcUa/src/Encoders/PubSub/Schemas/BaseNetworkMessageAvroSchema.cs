// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Furly;
    using Furly.Extensions.Messaging;
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
        public abstract Schema Schema { get; }

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
        /// <param name="networkMessageContentMask"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        protected virtual Schema Compile(string? typeName,
            IEnumerable<DataSetWriterModel> dataSetWriters,
            NetworkMessageContentMask? networkMessageContentMask,
            SchemaOptions? options)
        {
            options ??= new SchemaOptions();
            var contentMask = networkMessageContentMask ?? default;
            var MonitoredItemMessage = contentMask
                .HasFlag(NetworkMessageContentMask.MonitoredItemMessage);
            if (MonitoredItemMessage)
            {
                contentMask &= ~NetworkMessageContentMask.NetworkMessageHeader;
            }

            var dataSetMessageSchemas = dataSetWriters
                .Where(writer => writer.DataSet != null)
                .OrderBy(writer => writer.DataSetWriterId)
                .Select(writer => (writer.DataSetWriterId,
                    Schema: GetDataSetMessageSchema(writer, contentMask,
                        options, _uniqueNames)))
                .ToList();

            if (dataSetMessageSchemas.Count == 0)
            {
                return AvroSchema.Null;
            }

            Schema? payloadType;
            if (contentMask.HasFlag(NetworkMessageContentMask.MonitoredItemMessage))
            {
                return dataSetMessageSchemas
                    .SelectMany(kv => kv.Schema is UnionSchema u ?
                        u.Schemas : kv.Schema.YieldReturn())
                    .AsUnion();
            }

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
                payloadType = unionSchemas.AsUnion();
            }
            else
            {
                payloadType = dataSetMessageSchemas[0].Schema;
            }


            var HasSingleDataSetMessage = contentMask
                .HasFlag(NetworkMessageContentMask.SingleDataSetMessage);
            var HasNetworkMessageHeader = contentMask
                .HasFlag(NetworkMessageContentMask.NetworkMessageHeader);
            if (!HasNetworkMessageHeader && HasSingleDataSetMessage)
            {
                // No network message header
                return payloadType;
            }

            payloadType = payloadType.AsArray();
            var fields = CollectFields(contentMask, payloadType);

            var ns = options.Namespace != null ?
                SchemaUtils.NamespaceUriToNamespace(options.Namespace) :
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
        /// <param name="contentMask"></param>
        /// <param name="options"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        protected abstract Schema GetDataSetMessageSchema(DataSetWriterModel writer,
            NetworkMessageContentMask contentMask, SchemaOptions options,
            HashSet<string> uniqueNames);

        /// <summary>
        /// Get name of the type
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private string GetName(string? typeName)
        {
            // Type name of the message record
            typeName ??= string.Empty;
            typeName = SchemaUtils.Escape(typeName) + kMessageTypeName;
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

        internal const string kMessageTypeName = "NetworkMessage";
        private readonly HashSet<string> _uniqueNames = new();
    }
}
