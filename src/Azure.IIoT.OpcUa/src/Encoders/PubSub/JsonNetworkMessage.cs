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
    using System.Linq;
    using System.Text;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Json Network message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class JsonNetworkMessage : BaseNetworkMessage
    {
        /// <inheritdoc/>
        public override string MessageSchema => HasSamplesPayload ?
            MessageSchemaTypes.MonitoredItemMessageJson : MessageSchemaTypes.NetworkMessageJson;

        /// <inheritdoc/>
        public override string ContentType => UseGzipCompression ?
            Encoders.ContentType.JsonGzip : ContentMimeType.Json;

        /// <inheritdoc/>
        public override string ContentEncoding => Encoding.UTF8.WebName;

        /// <summary>
        /// Ua data message type
        /// </summary>
        public const string MessageTypeUaData = "ua-data";

        /// <summary>
        /// Message id
        /// </summary>
        public Func<string> MessageId { get; set; } = () => Guid.NewGuid().ToString();

        /// <summary>
        /// Message type
        /// </summary>
        internal string MessageType { get; set; } = MessageTypeUaData;

        /// <summary>
        /// Get flag that indicates if message has network message header
        /// </summary>
        public bool HasNetworkMessageHeader
            => (NetworkMessageContentMask & NetworkMessageContentFlags.NetworkMessageHeader) != 0;

        /// <summary>
        /// Flag that indicates if the Network message contains a single dataset message
        /// </summary>
        public bool HasSingleDataSetMessage
            => (NetworkMessageContentMask & NetworkMessageContentFlags.SingleDataSetMessage) != 0;

        /// <summary>
        /// Flag that indicates if the Network message dataSets have header
        /// </summary>
        public bool HasDataSetMessageHeader
            => (NetworkMessageContentMask & NetworkMessageContentFlags.DataSetMessageHeader) != 0;

        /// <summary>
        /// Flag that indicates if the Network message payload is monitored item samples
        /// </summary>
        public bool HasSamplesPayload
        {
            get
            {
                if (_hasSamplesPayload == null)
                {
                    if (Messages.Count > 0)
                    {
                        _hasSamplesPayload = Messages.Any(m => m is MonitoredItemMessage);
                    }
                    else
                    {
                        return false;
                    }
                }
                return _hasSamplesPayload.Value;
            }
            set => _hasSamplesPayload = value;
        }

        /// <summary>
        /// Sets the message schema to use
        /// </summary>
        internal string? MessageSchemaToUse
        {
            get => MessageSchema;
            set
            {
                HasSamplesPayload = value?.Equals(
                    MessageSchemaTypes.MonitoredItemMessageJson, StringComparison.OrdinalIgnoreCase) == true;
            }
        }

        /// <summary>
        /// Flag that indicates if advanced encoding should be used
        /// </summary>
        public bool UseAdvancedEncoding { get; set; }

        /// <summary>
        /// Namespace format to use
        /// </summary>
        public NamespaceFormat NamespaceFormat { get; set; }

        /// <summary>
        /// Wrap the resulting message into an array. This is for legacy compatiblity
        /// where we used to encode a set of network messages in arrays. This is the
        /// default in OPC Publisher 2.+ if strict compliance with standard is not
        /// enabled.
        /// </summary>
        public bool UseArrayEnvelope { get; set; }

        /// <summary>
        /// Use gzip compression
        /// </summary>
        public bool UseGzipCompression { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not JsonNetworkMessage wrapper)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Opc.Ua.Utils.IsEqual(wrapper.MessageId(), MessageId()) ||
                !Opc.Ua.Utils.IsEqual(wrapper.DataSetWriterGroup, DataSetWriterGroup))
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
            return hash.ToHashCode();
        }


        /// <inheritdoc/>
        public override bool TryDecode(Opc.Ua.IServiceMessageContext context, Stream stream,
            IDataSetMetaDataResolver? resolver)
        {
            var root = ReadRoot(stream);
            if (root == null)
            {
                return false;
            }
            return TryDecodeRoot(context, root);
        }

        /// <inheritdoc/>
        public override bool TryDecode(Opc.Ua.IServiceMessageContext context,
            Queue<ReadOnlySequence<byte>> reader, IDataSetMetaDataResolver? resolver = null)
        {
            // Decodes a single buffer
            if (reader.TryPeek(out var buffer))
            {
                using var memoryStream = buffer.IsSingleSegment ?
                    Memory.GetStream(buffer.FirstSpan) :
                    Memory.GetStream(buffer.ToArray());
                var root = ReadRoot(memoryStream);
                if (root == null || !TryDecodeRoot(context, root))
                {
                    return false;
                }
                // Complete the buffer
                reader.Dequeue();
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<ReadOnlySequence<byte>> Encode(Opc.Ua.IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver? resolver = null)
        {
            var chunks = new List<ReadOnlySequence<byte>>();
            var messages = Messages.OfType<JsonDataSetMessage>().ToArray();
            if (HasSingleDataSetMessage && !UseArrayEnvelope)
            {
                foreach (var message in messages)
                {
                    EncodeChunk(new[] { message });
                }
            }
            else
            {
                EncodeChunk(messages);
            }
            return chunks;

            void EncodeChunk(JsonDataSetMessage[] subset)
            {
                var root = BuildRoot(context, subset);
                var buffer = Serialize(root);
                if (buffer.Length < maxChunkSize)
                {
                    chunks.Add(new ReadOnlySequence<byte>(buffer));
                }
                else if (subset.Length <= 1)
                {
                    chunks.Add(default);
                }
                else
                {
                    var len = subset.Length / 2;
                    EncodeChunk(subset[..len]);
                    EncodeChunk(subset[len..]);
                }
            }
        }

        /// <summary>
        /// Serialize a network message root node to a (optionally gzipped) utf-8 buffer.
        /// </summary>
        /// <param name="root"></param>
        private byte[] Serialize(JsonNode? root)
        {
            var json = root?.ToJsonString() ?? "null";
            var bytes = Encoding.UTF8.GetBytes(json);
            if (!UseGzipCompression)
            {
                return bytes;
            }
            using var memoryStream = new MemoryStream();
            using (var gzip = new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Read the root json node from a (optionally gzipped) stream.
        /// </summary>
        /// <param name="stream"></param>
        private JsonNode? ReadRoot(Stream stream)
        {
            try
            {
                var compression = UseGzipCompression ?
                    new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true) : null;
                try
                {
                    using var reader = new StreamReader((Stream?)compression ?? stream,
                        Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    var json = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return null;
                    }
                    return JsonNode.Parse(json);
                }
                finally
                {
                    compression?.Dispose();
                }
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Build the root json node for a subset of dataset messages honoring the
        /// array-envelope / network-message-header / single-message flags.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="messages"></param>
        private JsonNode? BuildRoot(Opc.Ua.IServiceMessageContext context,
            JsonDataSetMessage[] messages)
        {
            if (!UseArrayEnvelope)
            {
                return BuildNetworkMessage(context, messages);
            }
            var array = new JsonArray();
            if (HasSingleDataSetMessage || HasNetworkMessageHeader)
            {
                // Legacy compatibility - n network messages with 1 message each inside array
                foreach (var message in messages)
                {
                    array.Add(BuildNetworkMessage(context, new[] { message }));
                }
            }
            else
            {
                // Write all messages into the array envelope
                var node = BuildNetworkMessage(context, messages);
                if (node is JsonArray inner)
                {
                    foreach (var element in inner.ToArray())
                    {
                        inner.Remove(element);
                        array.Add(element);
                    }
                }
                else
                {
                    array.Add(node);
                }
            }
            return array;
        }

        /// <summary>
        /// Build a single network message node (with or without the network message
        /// header envelope) for the provided dataset messages.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="messages"></param>
        private JsonNode? BuildNetworkMessage(Opc.Ua.IServiceMessageContext context,
            JsonDataSetMessage[] messages)
        {
            var publisherId =
                (NetworkMessageContentMask & NetworkMessageContentFlags.PublisherId) == 0
                    ? null : PublisherId;
            if (!HasNetworkMessageHeader)
            {
                return BuildMessagesPayload(context, messages, publisherId);
            }
            var header = new JsonObject
            {
                [nameof(MessageId)] = MessageId(),
                [nameof(MessageType)] = MessageType
            };
            if ((NetworkMessageContentMask & NetworkMessageContentFlags.PublisherId) != 0)
            {
                header[nameof(PublisherId)] = PublisherId;
            }
            if ((NetworkMessageContentMask & NetworkMessageContentFlags.DataSetClassId) != 0 &&
                DataSetClassId != Guid.Empty)
            {
                header[nameof(DataSetClassId)] = DataSetClassId.ToString();
            }
            if (!string.IsNullOrEmpty(DataSetWriterGroup))
            {
                header[nameof(DataSetWriterGroup)] = DataSetWriterGroup;
            }
            header[nameof(Messages)] = BuildMessagesPayload(context, messages, publisherId);
            return header;
        }

        /// <summary>
        /// Build the Messages payload node - either a single dataset message / payload,
        /// or an array of dataset messages / payloads.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="messages"></param>
        /// <param name="publisherId"></param>
        private JsonNode? BuildMessagesPayload(Opc.Ua.IServiceMessageContext context,
            JsonDataSetMessage[] messages, string? publisherId)
        {
            if (HasSingleDataSetMessage)
            {
                return messages.Length == 0 ? null : messages[0].EncodeToNode(
                    context, publisherId, HasDataSetMessageHeader,
                    UseAdvancedEncoding, NamespaceFormat);
            }
            var array = new JsonArray();
            foreach (var message in messages)
            {
                array.Add(message.EncodeToNode(
                    context, publisherId, HasDataSetMessageHeader,
                    UseAdvancedEncoding, NamespaceFormat));
            }
            return array;
        }

        /// <summary>
        /// Decode a root node (single network message or array of network messages).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="root"></param>
        private bool TryDecodeRoot(Opc.Ua.IServiceMessageContext context, JsonNode root)
        {
            if (root is JsonArray array)
            {
                if (array.Count == 0)
                {
                    return false;
                }
                foreach (var element in array)
                {
                    if (!TryReadNetworkMessage(context, element))
                    {
                        return false;
                    }
                }
                return true;
            }
            return TryReadNetworkMessage(context, root);
        }

        /// <summary>
        /// Try read a network message from a json node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        private bool TryReadNetworkMessage(Opc.Ua.IServiceMessageContext context, JsonNode? node)
        {
            if (!HasSamplesPayload && node is JsonObject obj &&
                TryReadNetworkMessageHeader(obj, out var networkMessageContentMask))
            {
                var messagesNode = obj[nameof(Messages)];
                if (messagesNode is JsonObject)
                {
                    // Single message
                    networkMessageContentMask |= NetworkMessageContentFlags.SingleDataSetMessage;
                }
                else if (messagesNode is not JsonArray)
                {
                    return false;
                }
                NetworkMessageContentMask = networkMessageContentMask;
                return TryReadDataSetMessages(context, messagesNode);
            }

            // Reset
            NetworkMessageContentMask = 0;
            DataSetWriterGroup = null;
            DataSetClassId = default;
            MessageId = () => Guid.NewGuid().ToString();
            PublisherId = null;

            if (node is JsonObject)
            {
                // Treat this object as the single message
                NetworkMessageContentMask |= NetworkMessageContentFlags.SingleDataSetMessage;
            }
            else if (node is not JsonArray)
            {
                // This node is neither an object nor array
                return false;
            }
            return TryReadDataSetMessages(context, node);
        }

        /// <summary>
        /// Read the dataset messages from a node (single object or array).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        private bool TryReadDataSetMessages(Opc.Ua.IServiceMessageContext context, JsonNode? node)
        {
            var hasDataSetMessageHeader = false;
            string? publisherId = null;
            var elements = node is JsonArray array
                ? (IEnumerable<JsonNode?>)array : new[] { node };
            foreach (var element in elements)
            {
                BaseDataSetMessage message = !HasSamplesPayload
                    ? new JsonDataSetMessage() : new MonitoredItemMessage();
                var decoded = message is MonitoredItemMessage samples
                    ? samples.TryDecodeFromNode(context, element,
                        ref hasDataSetMessageHeader, ref publisherId)
                    : ((JsonDataSetMessage)message).TryDecodeFromNode(context, element,
                        ref hasDataSetMessageHeader, ref publisherId);
                if (!decoded)
                {
                    Messages.Clear();
                    return false;
                }
                Messages.Add(message);
            }
            if (hasDataSetMessageHeader)
            {
                NetworkMessageContentMask |= NetworkMessageContentFlags.DataSetMessageHeader;
            }
            if (publisherId != null)
            {
                NetworkMessageContentMask |= NetworkMessageContentFlags.PublisherId;
                PublisherId = null;
            }
            return true;
        }

        /// <summary>
        /// Read the network message header from a json object.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="networkMessageContentMask"></param>
        private bool TryReadNetworkMessageHeader(JsonObject obj,
            out NetworkMessageContentFlags networkMessageContentMask)
        {
            networkMessageContentMask = 0;
            if (!obj.TryGetPropertyValue(nameof(MessageId), out var messageIdNode) ||
                HasSamplesPayload)
            {
                return false;
            }
            var messageId = messageIdNode?.GetValue<string>();
            if (messageId == null)
            {
                return false;
            }
            MessageId = () => messageId;
            var messageType = obj[nameof(MessageType)]?.GetValue<string>();
            if (!string.Equals(messageType, MessageTypeUaData, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            networkMessageContentMask |= NetworkMessageContentFlags.NetworkMessageHeader;

            if (obj.ContainsKey(nameof(PublisherId)))
            {
                PublisherId = obj[nameof(PublisherId)]?.GetValue<string>();
                if (PublisherId != null)
                {
                    networkMessageContentMask |= NetworkMessageContentFlags.PublisherId;
                }
                else
                {
                    return false;
                }
            }
            if (obj.ContainsKey(nameof(DataSetClassId)))
            {
                var dataSetClassId = obj[nameof(DataSetClassId)]?.GetValue<string>();
                if (dataSetClassId != null && Guid.TryParse(dataSetClassId, out var result))
                {
                    DataSetClassId = result;
                    networkMessageContentMask |= NetworkMessageContentFlags.DataSetClassId;
                }
                else
                {
                    return false;
                }
            }
            if (obj.ContainsKey(nameof(DataSetWriterGroup)))
            {
                DataSetWriterGroup = obj[nameof(DataSetWriterGroup)]?.GetValue<string>();
                if (DataSetWriterGroup == null)
                {
                    return false;
                }
            }
            return obj.ContainsKey(nameof(Messages));
        }

        private bool? _hasSamplesPayload;
    }
}
