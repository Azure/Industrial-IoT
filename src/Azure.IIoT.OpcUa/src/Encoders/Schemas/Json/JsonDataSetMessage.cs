// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Messaging;
    using global::Json.Schema;
    using Opc.Ua;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// DataSet message Json schema
    /// </summary>
    public sealed class JsonDataSetMessage : IEventSchema
    {
        /// <inheritdoc/>
        public string Type => ContentMimeType.Json;

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public ulong Version { get; }

        /// <inheritdoc/>
        public string? Id { get; }

        /// <inheritdoc/>
        string IEventSchema.Schema => ToString()!;

        /// <summary>
        /// Compatibility with 2.8 when encoding and decoding
        /// </summary>
        public bool UseCompatibilityMode { get; }

        /// <summary>
        /// Schema reference
        /// </summary>
        public JsonSchema? Ref { get; }

        /// <summary>
        /// Definitions
        /// </summary>
        internal Dictionary<string, JsonSchema> Definitions => _dataSet.Definitions;

        /// <summary>
        /// Definitions
        /// </summary>
        internal ServiceMessageContext Context => _dataSet.Context;

        /// <summary>
        /// Get json schema for a writer
        /// </summary>
        /// <param name="dataSetMessage"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        internal JsonDataSetMessage(PublishedDataSetMessageSchemaModel dataSetMessage,
            bool withDataSetMessageHeader, SchemaOptions options,
            Dictionary<string, JsonSchema> definitions, bool useCompatibilityMode,
            HashSet<string> uniqueNames)
        {
            _options = options;
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new JsonDataSet(dataSetMessage.MetaData,
                dataSetMessage.DataSetFieldContentFlags, options, definitions, uniqueNames);
            UseCompatibilityMode = useCompatibilityMode;
            Name = GetName(dataSetMessage.TypeName, uniqueNames);
            Ref = Compile(dataSetMessage.DataSetMessageContentFlags
                ?? PubSubMessage.DefaultDataSetMessageContentFlags);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return new JsonSchema
            {
                Type = Ref == null ? SchemaType.Null : SchemaType.None,
                Definitions = Definitions,
                Reference = Ref?.Reference
            }.ToJsonString();
        }

        /// <summary>
        /// Compile the data set message schema
        /// </summary>
        /// <param name="dataSetMessageContentMask"></param>
        /// <returns></returns>
        private JsonSchema? Compile(DataSetMessageContentFlags dataSetMessageContentMask)
        {
            if (_dataSet.Ref == null)
            {
                return null;
            }

            if (!_withDataSetMessageHeader)
            {
                // Not a data set message
                return _dataSet.Ref;
            }

            var encoding = new JsonBuiltInSchemas(true, true, Definitions);
            var properties = new Dictionary<string, JsonSchema>();
            if (dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.DataSetWriterId))
            {
                if (!UseCompatibilityMode)
                {
                    properties.Add(nameof(DataSetMessageContentFlags.DataSetWriterId),
                        encoding.GetSchemaForBuiltInType(BuiltInType.UInt16));
                }
                else
                {
                    // Up to version 2.8 we wrote the string id as id which is not per standard
                    properties.Add(nameof(DataSetMessageContentFlags.DataSetWriterId),
                        encoding.GetSchemaForBuiltInType(BuiltInType.String));
                }
            }
            if (dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.SequenceNumber))
            {
                properties.Add(nameof(DataSetMessageContentFlags.SequenceNumber),
                    encoding.GetSchemaForBuiltInType(BuiltInType.UInt32));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.MetaDataVersion))
            {
                var version = Definitions.Reference(
                    DataTypeIds.ConfigurationVersionDataType.GetSchemaId(Context),
                    id => new JsonSchema
                    {
                        Id = id,
                        Type = SchemaType.Object,
                        Properties = new Dictionary<string, JsonSchema>
                        {
                            ["MajorVersion"] = encoding.GetSchemaForBuiltInType(BuiltInType.UInt32),
                            ["MinorVersion"] = encoding.GetSchemaForBuiltInType(BuiltInType.UInt32)
                        }
                    });
                properties.Add(nameof(DataSetMessageContentFlags.MetaDataVersion), version);
            }
            if (dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Timestamp))
            {
                properties.Add(nameof(DataSetMessageContentFlags.Timestamp),
                    encoding.GetSchemaForBuiltInType(BuiltInType.DateTime));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.Status))
            {
                if (!UseCompatibilityMode)
                {
                    properties.Add(nameof(DataSetMessageContentFlags.Status),
                        encoding.GetSchemaForBuiltInType(BuiltInType.StatusCode));
                }
                else
                {
                    // Up to version 2.8 we wrote the full status code
                    properties.Add(nameof(DataSetMessageContentFlags.Status),
                        new JsonBuiltInSchemas(false, false, Definitions)
                        .GetSchemaForBuiltInType(BuiltInType.StatusCode));
                }
            }
            if (dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.MessageType))
            {
                properties.Add(nameof(DataSetMessageContentFlags.MessageType),
                    encoding.GetSchemaForBuiltInType(BuiltInType.String));
            }
            if (!UseCompatibilityMode &&
                dataSetMessageContentMask.HasFlag(DataSetMessageContentFlags.DataSetWriterName))
            {
                properties.Add(nameof(DataSetMessageContentFlags.DataSetWriterName),
                    encoding.GetSchemaForBuiltInType(BuiltInType.String));
            }

            properties.Add(nameof(PubSub.JsonDataSetMessage.Payload), _dataSet.Ref);

            return Definitions.Reference(_options.GetSchemaId(Name), id => new JsonSchema
            {
                Id = id,
                Type = SchemaType.Object,
                AdditionalProperties = new JsonSchema { Allowed = false },
                Properties = properties,
                Required = properties.Keys.ToList()
            });
        }

        /// <summary>
        /// Get name of the type
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="uniqueNames"></param>
        /// <returns></returns>
        private static string GetName(string? typeName, HashSet<string>? uniqueNames)
        {
            // Type name of the message record
            typeName ??= string.Empty;
            typeName += BaseDataSetMessage.kMessageTypeName;
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

        private readonly JsonDataSet _dataSet;
        private readonly SchemaOptions _options;
        private readonly bool _withDataSetMessageHeader;
    }
}
