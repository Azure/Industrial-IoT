﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Avro;
    using Azure.IIoT.OpcUa.Encoders;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Avro Network message
    /// </summary>
    public class AvroNetworkMessage : BaseNetworkMessage
    {
        /// <inheritdoc/>
        public override string MessageSchema =>
            MessageSchemaTypes.NetworkMessageAvro;

        /// <inheritdoc/>
        public override string ContentType => UseGzipCompression ?
            Encoders.ContentType.AvroGzip : Encoders.ContentType.Avro;

        /// <inheritdoc/>
        public override string ContentEncoding => Encoding.UTF8.WebName;

        /// <summary>
        /// Ua data message type
        /// </summary>
        public const string MessageTypeUaData = "ua-data";

        /// <summary>
        /// Message schema
        /// </summary>
        public Schema? Schema { get; private set; }

        /// <summary>
        /// Message id
        /// </summary>
        public Func<string> MessageId { get; set; } =
            () => Guid.NewGuid().ToString();

        /// <summary>
        /// Message type
        /// </summary>
        internal string MessageType { get; set; } = MessageTypeUaData;

        /// <summary>
        /// Get flag that indicates if message has network message header
        /// </summary>
        public bool HasNetworkMessageHeader
            => (NetworkMessageContentMask & (uint)JsonNetworkMessageContentMask.NetworkMessageHeader) != 0;

        /// <summary>
        /// Flag that indicates if the Network message contains a single dataset message
        /// </summary>
        public bool HasSingleDataSetMessage
            => (NetworkMessageContentMask & (uint)JsonNetworkMessageContentMask.SingleDataSetMessage) != 0;

        /// <summary>
        /// Flag that indicates if the Network message dataSets have header
        /// </summary>
        public bool HasDataSetMessageHeader
            => (NetworkMessageContentMask & (uint)JsonNetworkMessageContentMask.DataSetMessageHeader) != 0;

        /// <summary>
        /// Use gzip compression
        /// </summary>
        public bool UseGzipCompression { get; set; }

        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="schema"></param>
        public AvroNetworkMessage(Schema? schema)
        {
            Schema = schema;
            MessageId = () => _messageId ?? string.Empty;
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
            if (!Utils.IsEqual(wrapper.MessageId(), MessageId()) ||
                !Utils.IsEqual(wrapper.DataSetWriterGroup, DataSetWriterGroup))
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
        public override bool TryDecode(IServiceMessageContext context,
            Queue<ReadOnlySequence<byte>> reader, IDataSetMetaDataResolver? resolver = null)
        {
            // Decodes a single buffer
            if (Schema == null)
            {
                return false;
            }
            if (reader.TryPeek(out var buffer))
            {
                using (var memoryStream = buffer.IsSingleSegment ?
                    Memory.GetStream(buffer.FirstSpan) :
                    Memory.GetStream(buffer.ToArray()))
                {
                    var compression = UseGzipCompression ?
                        new GZipStream(memoryStream, CompressionMode.Decompress, leaveOpen: true) : null;
                    try
                    {
                        using var decoder = new AvroDecoder((Stream?)compression ?? memoryStream,
                            Schema, context);
                        while (memoryStream.Position != memoryStream.Length)
                        {
                            if (!TryReadNetworkMessage(decoder))
                            {
                                return false;
                            }
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
            }
            return false;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<ReadOnlySequence<byte>> Encode(IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver? resolver = null)
        {
            var chunks = new List<ReadOnlySequence<byte>>();
            var messages = Messages.OfType<AvroDataSetMessage>().ToArray().AsSpan();
            var messageId = MessageId;
            try
            {
                if (HasSingleDataSetMessage)
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

            void EncodeMessages(Span<AvroDataSetMessage> messages)
            {
                ReadOnlySequence<byte> messageBuffer;
                using (var memoryStream = Memory.GetStream())
                {
                    var compression = UseGzipCompression ?
                        new GZipStream(memoryStream,
                        CompressionLevel.Optimal, leaveOpen: true) : null;
                    try
                    {
                        var stream = (Stream?)compression ?? memoryStream;
                        if (Schema == null)
                        {
                            using var encoder = new AvroSchemaBuilder(stream, context);
                            WriteMessages(encoder, messages);
                            Schema = encoder.Schema;
                        }
                        else
                        {
                            using var encoder = new AvroEncoder(stream, Schema, context);
                            WriteMessages(encoder, messages);
                        }
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
        private void WriteMessages(BaseAvroEncoder encoder, Span<AvroDataSetMessage> messages)
        {
            var messagesToInclude = messages.ToArray();
            WriteNetworkMessage(encoder, messagesToInclude);
        }

        /// <summary>
        /// Try decode
        /// </summary>
        /// <param name="decoder"></param>
        /// <returns></returns>
        private bool TryReadNetworkMessage(AvroDecoder decoder)
        {
            // Reset
            DataSetWriterGroup = null;
            DataSetClassId = default;
            NetworkMessageContentMask = 0;
            MessageId = () => Guid.NewGuid().ToString();
            PublisherId = null;

            return decoder.ReadObject(null, schema =>
            {
                if (schema is RecordSchema recordSchema &&
                    recordSchema.Fields.Count == 6 &&
                    recordSchema.Fields[0].Name == nameof(MessageId) &&
                    recordSchema.Fields[5].Name == nameof(Messages))
                {
                    // Read network message header
                    NetworkMessageContentMask |= (uint)JsonNetworkMessageContentMask.NetworkMessageHeader;
                    _messageId = decoder.ReadString(nameof(MessageId));
                    var messageType = decoder.ReadString(nameof(MessageType));
                    if (!string.Equals(messageType, MessageTypeUaData,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        // Not a dataset network message
                        return false;
                    }
                    PublisherId = decoder.ReadString(nameof(PublisherId));
                    DataSetClassId = decoder.ReadGuid(nameof(DataSetClassId));
                    DataSetWriterGroup = decoder.ReadString(nameof(DataSetWriterGroup));

                    return TryReadDataSetMessages(decoder, nameof(Messages));
                }

                // No header, read object
                return TryReadDataSetMessages(decoder, null);
            });

            bool TryReadDataSetMessages(AvroDecoder decoder, string? fieldName)
            {
                return decoder.ReadObject(fieldName, schema =>
                {
                    // If schema is array, read array, if schema is not, read single message

                    if (schema is not ArraySchema arraySchema)
                    {
                        NetworkMessageContentMask |= (uint)JsonNetworkMessageContentMask.SingleDataSetMessage;
                        var message = new AvroDataSetMessage();
                        if (message.TryDecode(decoder, nameof(Messages), HasDataSetMessageHeader))
                        {
                            Messages.Clear();
                            Messages.Add(message);
                            return true;
                        }
                        return false;
                    }

                    var messages = decoder.ReadArray<BaseDataSetMessage?>(
                        nameof(Messages), () =>
                    {
                        var message = new AvroDataSetMessage();
                        if (!message.TryDecode(decoder, null, HasDataSetMessageHeader))
                        {
                            return null;
                        }
                        return message;
                    });

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
                    return true;
                });
            }
        }

        /// <summary>
        /// Encode with set messages
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="messages"></param>
        private void WriteNetworkMessage(BaseAvroEncoder encoder,
            AvroDataSetMessage[] messages)
        {
            if (!HasNetworkMessageHeader)
            {
                messages[0].Encode(encoder, null, HasDataSetMessageHeader);
                return;
            }

            // Write network message
            encoder.WriteObject(null, nameof(AvroNetworkMessage), () =>
            {
                encoder.WriteString(nameof(MessageId), MessageId());
                encoder.WriteString(nameof(MessageType), MessageType);
                encoder.WriteString(nameof(PublisherId), PublisherId);
                encoder.WriteGuid(nameof(DataSetClassId), DataSetClassId);
                encoder.WriteString(nameof(DataSetWriterGroup), DataSetWriterGroup);
                if (HasSingleDataSetMessage)
                {
                    Debug.Assert(messages.Length == 1);
                    messages[0].Encode(encoder, nameof(Messages), HasDataSetMessageHeader);
                }
                else
                {
                    encoder.WriteArray(nameof(Messages), messages,
                        v => v.Encode(encoder, null, HasDataSetMessageHeader));
                }
            });
        }

        /// <summary> To update message id </summary>
        protected string? _messageId;
    }
}