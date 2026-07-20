// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Json discovery metdata message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class JsonMetaDataMessage : PubSubMessage
    {
        /// <inheritdoc/>
        public override string MessageSchema
            => MessageSchemaTypes.NetworkMessageJson;

        /// <inheritdoc/>
        public override string ContentType
            => UseGzipCompression ? Encoders.ContentType.JsonGzip : ContentMimeType.Json;

        /// <inheritdoc/>
        public override string ContentEncoding => Encoding.UTF8.WebName;

        /// <summary>
        /// Ua meta data message type
        /// </summary>
        public const string MessageTypeUaMetadata = "ua-metadata";

        /// <summary>
        /// Message type
        /// </summary>
        internal string MessageType { get; set; } = MessageTypeUaMetadata;

        /// <summary>
        /// Flag that indicates if advanced encoding should be used
        /// </summary>
        public bool UseAdvancedEncoding { get; set; }

        /// <summary>
        /// Namespace format to use
        /// </summary>
        public NamespaceFormat NamespaceFormat { get; set; }

        /// <summary>
        /// Use gzip compression
        /// </summary>
        public bool UseGzipCompression { get; set; }

        /// <summary>
        /// Message id
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        /// Data set writer name in case of ua-metadata message
        /// </summary>
        public ushort DataSetWriterId { get; set; }

        /// <summary>
        /// Data set writer name in case of ua-metadata message
        /// </summary>
        public string? DataSetWriterName { get; set; }

        /// <summary>
        /// Data set metadata in case this is a metadata message
        /// </summary>
        public PublishedDataSetMetaDataModel? MetaData { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }
            if (obj is not JsonMetaDataMessage wrapper)
            {
                return false;
            }
            if (!Opc.Ua.Utils.IsEqual(wrapper.MessageId, MessageId) ||
                !Opc.Ua.Utils.IsEqual(wrapper.DataSetWriterGroup, DataSetWriterGroup) ||
                !Opc.Ua.Utils.IsEqual(wrapper.DataSetWriterName, DataSetWriterName) ||
                !wrapper.MetaData.IsSameAs(MetaData) ||
                !Opc.Ua.Utils.IsEqual(wrapper.DataSetWriterId, DataSetWriterId))
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(MessageId);
            hash.Add(DataSetWriterGroup);
            hash.Add(DataSetWriterName);
            hash.Add(DataSetWriterId);
            hash.Add(MetaData);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override bool TryDecode(Opc.Ua.IServiceMessageContext context, Stream stream,
            IDataSetMetaDataResolver? resolver)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override bool TryDecode(Opc.Ua.IServiceMessageContext context,
            Queue<ReadOnlySequence<byte>> reader, IDataSetMetaDataResolver? resolver)
        {
            if (reader.TryPeek(out var buffer))
            {
                using var memoryStream = buffer.IsSingleSegment ?
                    Memory.GetStream(buffer.FirstSpan) :
                    Memory.GetStream(buffer.ToArray());
                var compression = UseGzipCompression ?
                    new GZipStream(memoryStream, CompressionMode.Decompress, leaveOpen: true) : null;
                try
                {
                    using var streamReader = new StreamReader((Stream?)compression ?? memoryStream,
                        Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    var json = streamReader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return false;
                    }
                    var node = NormalizeMetaDataForDecoder(
                        context, JsonNode.Parse(json));
                    using var decoder = new Opc.Ua.JsonDecoder(
                        node?.ToJsonString() ?? "null", context);
                    if (TryDecode(decoder))
                    {
                        reader.Dequeue();
                        return true;
                    }
                }
                finally
                {
                    compression?.Dispose();
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<ReadOnlySequence<byte>> Encode(
            Opc.Ua.IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver? resolver)
        {
            var chunks = new List<ReadOnlySequence<byte>>();
            var bytes = Encoding.UTF8.GetBytes(EncodeToText(context));
            if (UseGzipCompression)
            {
                using var memoryStream = new MemoryStream();
                using (var gzip = new GZipStream(memoryStream, CompressionLevel.Optimal,
                    leaveOpen: true))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                bytes = memoryStream.ToArray();
            }
            if (bytes.Length < maxChunkSize)
            {
                chunks.Add(new ReadOnlySequence<byte>(bytes));
            }
            else
            {
                chunks.Add(default);
            }
            return chunks;
        }

        /// <summary>
        /// Encode metadata to a json object text using the 2.0 stack codec.
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="Azure.IIoT.OpcUa.Encoders.EncodingException"></exception>
        private string EncodeToText(Opc.Ua.IServiceMessageContext context)
        {
            if (MetaData == null)
            {
                throw new Azure.IIoT.OpcUa.Encoders.EncodingException("No metadata to encode.");
            }
            using var encoder = new Opc.Ua.JsonEncoder(context, Opc.Ua.JsonEncoderOptions.Compact);
            encoder.WriteString(nameof(MessageId), MessageId);
            encoder.WriteString(nameof(MessageType), MessageType);
            if (!string.IsNullOrEmpty(PublisherId))
            {
                encoder.WriteString(nameof(PublisherId), PublisherId);
            }
            if (DataSetWriterId != 0)
            {
                encoder.WriteUInt16(nameof(DataSetWriterId), DataSetWriterId);
            }
            if (!string.IsNullOrEmpty(DataSetWriterGroup))
            {
                encoder.WriteString(nameof(DataSetWriterGroup), DataSetWriterGroup);
            }
            var dataSetMetaData = MetaData.ToStackModel(context);
            encoder.WriteEncodeable(nameof(MetaData), dataSetMetaData);
            if (!string.IsNullOrEmpty(DataSetWriterName))
            {
                encoder.WriteString(nameof(DataSetWriterName), DataSetWriterName);
            }
            var encoded = JsonNode.Parse(encoder.CloseAndReturnText());
                return NormalizeMetaDataNode(context, encoded)?.ToJsonString() ?? "null";
        }

        private JsonNode? NormalizeMetaDataNode(
            Opc.Ua.IServiceMessageContext context, JsonNode? node)
        {
            if (node is JsonArray array)
            {
                var normalized = new JsonArray();
                foreach (var item in array)
                {
                    normalized.Add(NormalizeMetaDataNode(context, item));
                }
                return normalized;
            }
            if (node is not JsonObject obj)
            {
                return node?.DeepClone();
            }

            var result = new JsonObject();
            foreach (var property in obj)
            {
                if (property.Key == "StructureType" &&
                    property.Value is JsonValue structureType &&
                    structureType.TryGetValue<int>(out var rawStructureType))
                {
                    result[property.Key] =
                        $"{(Opc.Ua.StructureType)rawStructureType}_{rawStructureType}";
                }
                else if (kNodeIdFields.Contains(property.Key) &&
                    property.Value is JsonValue value &&
                    value.GetValueKind() == System.Text.Json.JsonValueKind.String)
                {
                    result[property.Key] = JsonPubSubCodec.NormalizeNodeId(
                        context, property.Value, reversible: false,
                        useAdvancedEncoding: UseAdvancedEncoding,
                        namespaceFormat: NamespaceFormat);
                }
                else
                {
                    result[property.Key] =
                        NormalizeMetaDataNode(context, property.Value);
                }
            }
            return result;
        }

        private static JsonNode? NormalizeMetaDataForDecoder(
            Opc.Ua.IServiceMessageContext context, JsonNode? node)
        {
            if (node is JsonArray array)
            {
                var normalized = new JsonArray();
                foreach (var item in array)
                {
                    normalized.Add(NormalizeMetaDataForDecoder(context, item));
                }
                return normalized;
            }
            if (node is not JsonObject obj)
            {
                return node?.DeepClone();
            }

            var result = new JsonObject();
            foreach (var property in obj)
            {
                result[property.Key] = kNodeIdFields.Contains(property.Key)
                    ? JsonPubSubCodec.NormalizeNodeIdForDecoder(
                        context, property.Value, expanded: false)
                    : NormalizeMetaDataForDecoder(context, property.Value);
            }
            return result;
        }

        /// <summary>
        /// Decode the metadata message from a json decoder.
        /// </summary>
        /// <param name="decoder"></param>
        private bool TryDecode(Opc.Ua.IDecoder decoder)
        {
            MessageId = decoder.ReadString(nameof(MessageId));
            var messageType = decoder.ReadString(nameof(MessageType));
            if (messageType == null ||
                !messageType.Equals(MessageTypeUaMetadata, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            PublisherId = decoder.ReadString(nameof(PublisherId));
            DataSetWriterId = decoder.ReadUInt16(nameof(DataSetWriterId));
            var dataSetMetaData = decoder.ReadEncodeable<Opc.Ua.DataSetMetaDataType>(nameof(MetaData));
            MetaData = dataSetMetaData.ToServiceModel(decoder.Context);
            DataSetWriterName = decoder.ReadString(nameof(DataSetWriterName));
            return true;
        }

        private static readonly HashSet<string> kNodeIdFields =
        [
            "DataType",
            "DataTypeId",
            "BaseDataType",
            "DefaultEncodingId",
            "TypeId"
        ];
    }
}
