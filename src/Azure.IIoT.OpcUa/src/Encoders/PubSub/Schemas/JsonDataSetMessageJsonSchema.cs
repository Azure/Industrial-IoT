﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub.Schemas
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Json.Schema;
    using Furly;
    using Furly.Extensions.Messaging;
    using Opc.Ua;
    using System.Collections.Generic;
    using DataSetFieldContentMask = Publisher.Models.DataSetFieldContentMask;
    using System.Linq;

    /// <summary>
    /// DataSet message Json schema
    /// </summary>
    public sealed class JsonDataSetMessageJsonSchema : IEventSchema
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
        /// <param name="dataSetWriter"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <param name="useCompatibilityMode"></param>
        /// <returns></returns>
        public JsonDataSetMessageJsonSchema(DataSetWriterModel dataSetWriter,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            Dictionary<string, JsonSchema>? definitions = null,
            bool useCompatibilityMode = false)
        {
            _options = options ?? new SchemaOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new JsonDataSetJsonSchema(dataSetWriter, options, definitions);
            UseCompatibilityMode = useCompatibilityMode;
            Name = GetName(dataSetWriter.DataSet?.Name ?? dataSetWriter.DataSetWriterName);
            Ref = Compile(dataSetWriter.MessageSettings?.DataSetMessageContentMask ?? 0u);
        }

        /// <summary>
        /// Get avro schema for a dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="dataSetContentMask"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="withDataSetMessageHeader"></param>
        /// <param name="options"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        public JsonDataSetMessageJsonSchema(PublishedDataSetModel dataSet,
            DataSetContentMask? dataSetContentMask = null,
            DataSetFieldContentMask? dataSetFieldContentMask = null,
            bool withDataSetMessageHeader = true, SchemaOptions? options = null,
            Dictionary<string, JsonSchema>? definitions = null)
        {
            _options = options ?? new SchemaOptions();
            _withDataSetMessageHeader = withDataSetMessageHeader;
            _dataSet = new JsonDataSetJsonSchema(null, dataSet,
                dataSetFieldContentMask, options, definitions);
            Name = GetName(dataSet.Name);
            Ref = Compile(dataSetContentMask ?? default);
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
        private JsonSchema? Compile(DataSetContentMask dataSetMessageContentMask)
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

            var encoding = new JsonBuiltInJsonSchemas(true, true, Definitions);
            var properties = new Dictionary<string, JsonSchema>();
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.DataSetWriterId))
            {
                if (!UseCompatibilityMode)
                {
                    properties.Add(nameof(DataSetContentMask.DataSetWriterId),
                        encoding.GetSchemaForBuiltInType(BuiltInType.UInt16));
                }
                else
                {
                    // Up to version 2.8 we wrote the string id as id which is not per standard
                    properties.Add(nameof(DataSetContentMask.DataSetWriterId),
                        encoding.GetSchemaForBuiltInType(BuiltInType.String));
                }
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.SequenceNumber))
            {
                properties.Add(nameof(DataSetContentMask.SequenceNumber),
                    encoding.GetSchemaForBuiltInType(BuiltInType.UInt32));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.MetaDataVersion))
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
                properties.Add(nameof(DataSetContentMask.MetaDataVersion), version);
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Timestamp))
            {
                properties.Add(nameof(DataSetContentMask.Timestamp),
                    encoding.GetSchemaForBuiltInType(BuiltInType.DateTime));
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.Status))
            {
                if (!UseCompatibilityMode)
                {
                    properties.Add(nameof(DataSetContentMask.Status),
                        encoding.GetSchemaForBuiltInType(BuiltInType.StatusCode));
                }
                else
                {
                    // Up to version 2.8 we wrote the full status code
                    properties.Add(nameof(DataSetContentMask.Status),
                        new JsonBuiltInJsonSchemas(false, false, Definitions)
                        .GetSchemaForBuiltInType(BuiltInType.StatusCode));
                }
            }
            if (dataSetMessageContentMask.HasFlag(DataSetContentMask.MessageType))
            {
                properties.Add(nameof(DataSetContentMask.MessageType),
                    encoding.GetSchemaForBuiltInType(BuiltInType.String));
            }
            if (!UseCompatibilityMode &&
                dataSetMessageContentMask.HasFlag(DataSetContentMask.DataSetWriterName))
            {
                properties.Add(nameof(DataSetContentMask.DataSetWriterName),
                    encoding.GetSchemaForBuiltInType(BuiltInType.String));
            }

            properties.Add("Payload", _dataSet.Ref);

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
        /// <returns></returns>
        private static string GetName(string? typeName)
        {
            // Type name of the message record
            if (string.IsNullOrEmpty(typeName))
            {
                // Type name of the message record
                typeName = nameof(JsonDataSetMessage);
            }
            else
            {
                typeName += "DataSetMessage";
            }
            return typeName;
        }

        private readonly JsonDataSetJsonSchema _dataSet;
        private readonly SchemaOptions _options;
        private readonly bool _withDataSetMessageHeader;
    }
}