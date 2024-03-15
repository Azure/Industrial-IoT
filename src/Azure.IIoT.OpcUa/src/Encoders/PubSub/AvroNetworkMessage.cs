// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using global::Avro;
    using Azure.IIoT.OpcUa.Encoders;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
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
        public Schema Schema { get; }

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
        public AvroNetworkMessage(Schema schema)
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
                        using var decoder = new AvroDecoderCore((Stream?)compression ?? memoryStream,
                            context);
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
                EncodeMessages(messages);
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
                        using var encoder = new AvroEncoderCore(
                            (Stream?)compression ?? memoryStream, context);
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
        private void WriteMessages(AvroEncoderCore encoder, Span<AvroDataSetMessage> messages)
        {
            var messagesToInclude = messages.ToArray();
            WriteNetworkMessage(encoder, messagesToInclude);
        }

        /// <summary>
        /// Try decode
        /// </summary>
        /// <param name="decoder"></param>
        /// <returns></returns>
        private bool TryReadNetworkMessage(AvroDecoderCore decoder)
        {
            // Reset
            DataSetWriterGroup = null;
            DataSetClassId = default;
            MessageId = () => Guid.NewGuid().ToString();
            PublisherId = null;

            if (HasNetworkMessageHeader && !TryReadNetworkMessageHeader(decoder))
            {
                return false;
            }

            var messages = decoder.ReadCollection<BaseDataSetMessage?>(() =>
            {
                var message = new AvroDataSetMessage(decoder.Schema);
                if (!message.TryDecode(decoder, HasDataSetMessageHeader))
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
        }

        /// <summary>
        /// Encode with set messages
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="messages"></param>
        private void WriteNetworkMessage(AvroEncoderCore encoder, AvroDataSetMessage[] messages)
        {
            var publisherId = PublisherId;
            if (HasNetworkMessageHeader)
            {
                WriteNetworkMessageHeader(encoder);
            }
            encoder.WriteArray(messages, v => v.Encode(encoder, HasDataSetMessageHeader));
        }

        /// <summary>
        /// Read network message header
        /// </summary>
        /// <param name="decoder"></param>
        /// <returns></returns>
        private bool TryReadNetworkMessageHeader(AvroDecoderCore decoder)
        {
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
            return true;
        }

        /// <summary>
        /// Write network message header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteNetworkMessageHeader(AvroEncoderCore encoder)
        {
            encoder.WriteString(nameof(MessageId), MessageId());
            encoder.WriteString(nameof(MessageType), MessageType);
            encoder.WriteString(nameof(PublisherId), PublisherId);
            encoder.WriteString(nameof(DataSetClassId), DataSetClassId.ToString());
            encoder.WriteString(nameof(DataSetWriterGroup), DataSetWriterGroup);
        }

        /// <summary> To update message id </summary>
        protected string? _messageId;
    }
}
