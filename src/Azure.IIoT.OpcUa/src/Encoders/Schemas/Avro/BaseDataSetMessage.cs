// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Avro
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using global::Avro;
    using Furly;
    using Furly.Extensions.Messaging;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Base Dataset message avro schema
    /// </summary>
    public abstract class BaseDataSetMessage : IEventSchema, IAvroSchema
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

        /// <inheritdoc/>
        public abstract Schema Schema { get; }

        /// <summary>
        /// The data set schema
        /// </summary>
        protected abstract BaseDataSetSchema<Schema> DataSetSchema { get; }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema.ToString();
        }

        /// <summary>
        /// Compile the data set message schema
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="dataSetMessageContentFlags"></param>
        /// <param name="uniqueNames"></param>
        /// <param name="networkMessageContentFlags"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        protected virtual Schema Compile(string? typeName,
            DataSetMessageContentFlags dataSetMessageContentFlags, HashSet<string> uniqueNames,
            NetworkMessageContentFlags networkMessageContentFlags, SchemaOptions options)
        {
            if (!networkMessageContentFlags.HasFlag(NetworkMessageContentFlags.DataSetMessageHeader))
            {
                // Not a data set message
                return DataSetSchema.Schema;
            }

            var fields = CollectFields(dataSetMessageContentFlags, networkMessageContentFlags,
                DataSetSchema.Schema);

            typeName = GetTypeName(typeName, uniqueNames);
            var ns = options.Namespace != null ?
                SchemaUtils.NamespaceUriToNamespace(options.Namespace) :
                SchemaUtils.PublisherNamespace;
            return RecordSchema.Create(typeName, fields.ToList(), ns);
        }

        /// <summary>
        /// Collect fields of the message schema
        /// </summary>
        /// <param name="dataSetMessageContentFlags"></param>
        /// <param name="networkMessageContentFlags"></param>
        /// <param name="valueSchema"></param>
        /// <returns></returns>
        protected abstract IEnumerable<Field> CollectFields(
            DataSetMessageContentFlags dataSetMessageContentFlags,
            NetworkMessageContentFlags networkMessageContentFlags, Schema valueSchema);

        /// <summary>
        /// Create a type name
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="uniqueNames"></param>
        /// <param name="defaultName"></param>
        /// <returns></returns>
        protected static string GetTypeName(string? typeName, HashSet<string>? uniqueNames,
            string? defaultName = null)
        {
            // Type name of the message record
            typeName ??= string.Empty;
            typeName = SchemaUtils.Escape(typeName) + (defaultName ?? PubSub.BaseDataSetMessage.kMessageTypeName);

            if (uniqueNames != null)
            {
                var uniqueName = typeName;
                for (var index = 1; uniqueNames.Contains(uniqueName); index++)
                {
                    uniqueName = typeName + index;
                }
                uniqueNames.Add(uniqueName);
                typeName = uniqueName;
            }
            return typeName;
        }
    }
}
