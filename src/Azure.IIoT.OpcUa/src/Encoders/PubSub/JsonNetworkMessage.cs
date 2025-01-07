// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;

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

        /// <summary>
        /// Returns the starting state of the json encoder
        /// </summary>
        private JsonEncoderEx.JsonEncoding JsonEncoderStartingState
        {
            get
            {
                if (!HasNetworkMessageHeader)
                {
                    if (!HasSingleDataSetMessage || UseArrayEnvelope)
                    {
                        return JsonEncoderEx.JsonEncoding.Array;
                    }
                    if (!HasDataSetMessageHeader)
                    {
                        return JsonEncoderEx.JsonEncoding.Token;
                    }
                }
                if (UseArrayEnvelope)
                {
                    return JsonEncoderEx.JsonEncoding.Array;
                }
                return JsonEncoderEx.JsonEncoding.StartObject;
            }
        }

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
            var compression = UseGzipCompression ?
                new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true) : null;
            try
            {
                using var decoder = new JsonDecoderEx((Stream?)compression ?? stream,
                    context, useJsonLoader: false);
                var readArray = decoder.ReadArray(null, () => TryReadNetworkMessage(decoder));
                if (readArray?.All(s => s) != true ||
                    stream.Length != stream.Position)
                {
                    return false;
                }
                return true;
            }
            finally
            {
                compression?.Dispose();
            }
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
                var compression = UseGzipCompression ?
                    new GZipStream(memoryStream, CompressionMode.Decompress, leaveOpen: true) : null;
                try
                {
                    using var decoder = new JsonDecoderEx((Stream?)compression ?? memoryStream,
                        context, useJsonLoader: false);
                    var readArray = decoder.ReadArray(null, () => TryReadNetworkMessage(decoder));
                    if (readArray?.All(s => s) != true)
                    {
                        return false;
                    }
                    // Complete the buffer
                    reader.Dequeue();
                    return true;
                }
                finally
                {
                    compression?.Dispose();
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<ReadOnlySequence<byte>> Encode(Opc.Ua.IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver? resolver = null)
        {
            var chunks = new List<ReadOnlySequence<byte>>();
            var messages = Messages.OfType<JsonDataSetMessage>().ToArray().AsSpan();
            var messageId = MessageId;
            try
            {
                if (HasSingleDataSetMessage && !UseArrayEnvelope)
                {
                    for (var i = 0; i < messages.Length; i++)
                    {
                        EncodeMessages(messages.Slice(i, 1));
                    }
                }
                else
                {
                    EncodeMessages(messages);
                }
            }
            finally
            {
                MessageId = messageId;
            }
            return chunks;

            void EncodeMessages(Span<JsonDataSetMessage> messages)
            {
                ReadOnlySequence<byte> messageBuffer;
                using (var memoryStream = Memory.GetStream())
                {
                    var compression = UseGzipCompression ?
                        new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true) : null;
                    try
                    {
                        using var encoder = new JsonEncoderEx(
                            (Stream?)compression ?? memoryStream, context, JsonEncoderStartingState)
                        {
                            UseAdvancedEncoding = UseAdvancedEncoding,
                            UseUriEncoding = UseAdvancedEncoding,
                            NamespaceFormat = NamespaceFormat,
                            IgnoreDefaultValues = true,
                            IgnoreNullValues = true,
                            UseReversibleEncoding = false
                        };
                        WriteMessages(encoder, messages);
                    }
                    finally
                    {
                        compression?.Dispose();
                    }

                    messageBuffer = memoryStream.GetReadOnlySequence();

                    // TODO: instead of copy using ToArray we shall include the
                    // stream with the message and dispose it later when it is
                    // consumed.
                    messageBuffer = new ReadOnlySequence<byte>(messageBuffer.ToArray());
                }

                if (messageBuffer.Length < maxChunkSize)
                {
                    chunks.Add(messageBuffer);
                }
                else if (messages.Length == 1)
                {
                    chunks.Add(default);
                }
                else
                {
                    // Split
                    var len = messages.Length / 2;
                    var first = messages[..len];
                    var second = messages[len..];

                    EncodeMessages(first);
                    EncodeMessages(second);
                }
            }
        }

        /// <summary>
        /// Write message span
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="messages"></param>
        private void WriteMessages(JsonEncoderEx encoder, Span<JsonDataSetMessage> messages)
        {
            var messagesToInclude = messages.ToArray();
            if (UseArrayEnvelope)
            {
                if (HasSingleDataSetMessage || HasNetworkMessageHeader)
                {
                    // Legacy compatibility - n network messages with 1 message each inside array
                    for (var i = 0; i < messages.Length; i++)
                    {
                        var single = messages.Slice(i, 1).ToArray();
                        if (HasDataSetMessageHeader || HasNetworkMessageHeader)
                        {
                            encoder.WriteObject(null, Messages, _ => WriteNetworkMessage(encoder, single));
                        }
                        else
                        {
                            // Write single messages into the array envelope
                            WriteNetworkMessage(encoder, single);
                        }
                    }
                }
                else
                {
                    // Write all messages into the array envelope
                    WriteNetworkMessage(encoder, messagesToInclude);
                }
            }
            else
            {
                WriteNetworkMessage(encoder, messagesToInclude);
            }
        }

        /// <summary>
        /// Try decode
        /// </summary>
        /// <param name="decoder"></param>
        /// <returns></returns>
        private bool TryReadNetworkMessage(JsonDecoderEx decoder)
        {
            if (!HasSamplesPayload && TryReadNetworkMessageHeader(decoder, out var networkMessageContentMask))
            {
                if (decoder.IsObject(nameof(Messages)))
                {
                    // Single message
                    networkMessageContentMask |= NetworkMessageContentFlags.SingleDataSetMessage;
                }
                else if (!decoder.IsArray(nameof(Messages)))
                {
                    // Messages property is neither object nor array. We might be inside a single dataset
                    // TODO: Should we throw?
                    return false;
                }
                NetworkMessageContentMask = networkMessageContentMask;
                return TryReadDataSetMessages(decoder, nameof(Messages));
            }
            // Reset
            NetworkMessageContentMask = 0;
            DataSetWriterGroup = null;
            DataSetClassId = default;
            MessageId = () => Guid.NewGuid().ToString();
            PublisherId = null;

            if (decoder.IsObject(null))
            {
                // Treat this object as the single message
                NetworkMessageContentMask |= NetworkMessageContentFlags.SingleDataSetMessage;
            }
            else if (!decoder.IsArray(null))
            {
                // This object we are reading is neither an object nor array
                return false;
            }

            return TryReadDataSetMessages(decoder, null);

            bool TryReadDataSetMessages(JsonDecoderEx decoder, string? property)
            {
                var hasDataSetMessageHeader = false;
                string? publisherId = null;
                var messages = decoder.ReadArray<BaseDataSetMessage?>(property, () =>
                {
                    var message = !HasSamplesPayload ? new JsonDataSetMessage() : new MonitoredItemMessage();
                    if (!message.TryDecode(decoder, property, ref hasDataSetMessageHeader, ref publisherId))
                    {
                        return null;
                    }
                    return message;
                });
                if (messages == null)
                {
                    return false;
                }
                // Add decoded messages to messages array
                foreach (var message in messages)
                {
                    if (message == null)
                    {
                        // Reset
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
        }

        /// <summary>
        /// Encode with set messages
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="messages"></param>
        private void WriteNetworkMessage(JsonEncoderEx encoder, JsonDataSetMessage[] messages)
        {
            var publisherId =
                (NetworkMessageContentMask & NetworkMessageContentFlags.PublisherId) == 0
                    ? null : PublisherId;
            if (HasNetworkMessageHeader)
            {
                WriteNetworkMessageHeader(encoder);

                if (HasSingleDataSetMessage)
                {
                    if (HasDataSetMessageHeader)
                    {
                        // Write as a single object under messages property
                        encoder.WriteObject(nameof(Messages), messages[0],
                            v => v.Encode(encoder, publisherId, true, null));
                    }
                    else
                    {
                        // Write raw data set object under messages property
                        messages[0].Encode(encoder, publisherId, false, nameof(Messages));
                    }
                }
                else if (HasDataSetMessageHeader)
                {
                    // Write as array of objects
                    encoder.WriteArray(nameof(Messages), messages, v =>
                        encoder.WriteObject(null, v, v => v.Encode(encoder, publisherId, true, null)));
                }
                else
                {
                    // Write as array of dataset payload tokens
                    encoder.WriteArray(nameof(Messages), messages,
                        v => v.Encode(encoder, publisherId, false, null));
                }
            }
            else
            {
                // The encoder was set up as array or object beforehand
                if (HasSingleDataSetMessage)
                {
                    // Write object content to current object
                    messages[0].Encode(encoder, publisherId, HasDataSetMessageHeader, null);
                }
                else if (HasDataSetMessageHeader)
                {
                    // Write each object to the array that is the initial state of the encoder
                    foreach (var message in messages)
                    {
                        // Write as array of dataset messages with payload
                        encoder.WriteObject(null, message, v => v.Encode(encoder, publisherId, true, null));
                    }
                }
                else
                {
                    // Writes dataset directly the encoder was set up as token
                    foreach (var message in messages)
                    {
                        message.Encode(encoder, publisherId, false, null);
                    }
                }
            }
        }

        /// <summary>
        /// Read network message header
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <returns></returns>
        private bool TryReadNetworkMessageHeader(JsonDecoderEx decoder,
            out NetworkMessageContentFlags networkMessageContentMask)
        {
            networkMessageContentMask = 0;
            if (!decoder.HasField(nameof(MessageId)) || HasSamplesPayload)
            {
                return false;
            }
            var messageId = decoder.ReadString(nameof(MessageId));
            if (messageId == null)
            {
                // Field is there but not of type string, cannot be a network message header
                return false;
            }
            MessageId = () => messageId;
            var messageType = decoder.ReadString(nameof(MessageType));
            if (!string.Equals(messageType, MessageTypeUaData, StringComparison.OrdinalIgnoreCase))
            {
                // Not a dataset network message
                return false;
            }
            networkMessageContentMask |= NetworkMessageContentFlags.NetworkMessageHeader;

            if (decoder.HasField(nameof(PublisherId)))
            {
                PublisherId = decoder.ReadString(nameof(PublisherId));
                if (PublisherId != null)
                {
                    networkMessageContentMask |= NetworkMessageContentFlags.PublisherId;
                }
                else
                {
                    // publisher is not string type,
                    // TODO
                    return false;
                }
            }

            if (decoder.HasField(nameof(DataSetClassId)))
            {
                var dataSetClassId = decoder.ReadString(nameof(DataSetClassId));
                if (dataSetClassId != null && Guid.TryParse(dataSetClassId, out var result))
                {
                    DataSetClassId = result;
                    networkMessageContentMask |= NetworkMessageContentFlags.DataSetClassId;
                }
                else
                {
                    // class id is not guid or string
                    return false;
                }
            }

            if (decoder.HasField(nameof(DataSetWriterGroup)))
            {
                DataSetWriterGroup = decoder.ReadString(nameof(DataSetWriterGroup));
                if (DataSetWriterGroup == null)
                {
                    // writer group is not string type
                    // TODO
                    return false;
                }
            }
            return decoder.HasField(nameof(Messages));
        }

        /// <summary>
        /// Write network message header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteNetworkMessageHeader(JsonEncoderEx encoder)
        {
            // The encoder was set up as object beforehand based on IsJsonArray result
            encoder.WriteString(nameof(MessageId), MessageId());
            encoder.WriteString(nameof(MessageType), MessageType);
            if ((NetworkMessageContentMask & NetworkMessageContentFlags.PublisherId) != 0)
            {
                encoder.WriteString(nameof(PublisherId), PublisherId);
            }
            if ((NetworkMessageContentMask & NetworkMessageContentFlags.DataSetClassId) != 0 &&
                DataSetClassId != Guid.Empty)
            {
                encoder.WriteString(nameof(DataSetClassId), DataSetClassId.ToString());
            }
            if (!string.IsNullOrEmpty(DataSetWriterGroup))
            {
                encoder.WriteString(nameof(DataSetWriterGroup), DataSetWriterGroup);
            }
        }

        private bool? _hasSamplesPayload;
    }
}
