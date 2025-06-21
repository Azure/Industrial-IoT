// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Avro;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
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
        public Schema? Schema { get; set; }

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
        /// Use gzip compression
        /// </summary>
        public bool UseGzipCompression { get; set; }

        /// <summary>
        /// Setting to generate concise schemas during encoding
        /// </summary>
        internal bool EmitConciseSchema { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not AvroNetworkMessage wrapper)
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
            if (Schema == null)
            {
                return false;
            }
            var compression = UseGzipCompression ?
                new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true) : null;
            try
            {
                using var decoder = new AvroDecoder((Stream?)compression ?? stream,
                    Schema, context, true);

                return TryReadNetworkMessage(decoder);
            }
            finally
            {
                compression?.Dispose();
            }
        }

        /// <inheritdoc/>
        public override bool TryDecode(Opc.Ua.IServiceMessageContext context,
            Queue<ReadOnlySequence<byte>> reader, IDataSetMetaDataResolver? resolver)
        {
            // Decodes a single buffer
            if (Schema == null)
            {
                return false;
            }
            if (reader.TryPeek(out var buffer))
            {
                using var memoryStream = buffer.IsSingleSegment ?
                    Memory.GetStream(buffer.FirstSpan) :
                    Memory.GetStream(buffer.ToArray());
                var compression = UseGzipCompression ?
                    new GZipStream(memoryStream, CompressionMode.Decompress, leaveOpen: true) : null;
                try
                {
                    using var decoder = new AvroDecoder((Stream?)compression ?? memoryStream,
                        Schema, context);

                    if (!TryReadNetworkMessage(decoder) ||
                        memoryStream.Position != memoryStream.Length)
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
        public override IReadOnlyList<ReadOnlySequence<byte>> Encode(
            Opc.Ua.IServiceMessageContext context,
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
                            using var encoder = new AvroSchemaBuilder(stream, context,
                                emitConciseSchemas: EmitConciseSchema);
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
            NetworkMessageContentMask = NetworkMessageContentFlags.DataSetMessageHeader;
            MessageId = () => Guid.NewGuid().ToString();
            PublisherId = null;
            Messages.Clear();

            var current = decoder.Current;
            var result = decoder.ReadObject(null, schema =>
            {
                if (schema is not RecordSchema recordSchema)
                {
                    // Should always be a record at the start
                    return (bool?)null;
                }

                if (recordSchema.Fields.Count == 6 &&
                    recordSchema.Fields[0].Name == nameof(MessageId) &&
                    recordSchema.Fields[5].Name == nameof(Messages))
                {
                    // Read network message header
                    NetworkMessageContentMask |= NetworkMessageContentFlags.NetworkMessageHeader;

                    var messageId = decoder.ReadString(nameof(MessageId));
                    if (messageId != null)
                    {
                        MessageId = () => messageId;
                    }
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

                    // Header and content is array of messages
                    return TryReadDataSetMessageArray(decoder, nameof(Messages));
                }

                if (recordSchema.Fields.Count == 1 &&
                    recordSchema.Fields[0].Name == nameof(Messages))
                {
                    // No header and content is array of messages
                    return TryReadDataSetMessageArray(decoder, nameof(Messages));
                }

                // No header thus content must be single data set message
                NetworkMessageContentMask |= NetworkMessageContentFlags.SingleDataSetMessage;
                return null;
            });

            if (!result.HasValue)
            {
                // Reposition the schema
                decoder.Push(current);
                return TryReadDataSetMessage(decoder);
            }
            return result.Value;

            bool TryReadDataSetMessageArray(AvroDecoder decoder, string fieldName)
            {
                // Read objects from field name
                var result = decoder.ReadArray(fieldName, () => TryReadDataSetMessage(decoder));
                if (result.Length == 1)
                {
                    NetworkMessageContentMask |= NetworkMessageContentFlags.SingleDataSetMessage;
                }
                return result.All(s => s);
            }

            bool TryReadDataSetMessage(AvroDecoder decoder)
            {
                var message = new AvroDataSetMessage();
                if (message.TryDecode(decoder, HasDataSetMessageHeader))
                {
                    if (!message.WithDataSetHeader)
                    {
                        NetworkMessageContentMask &= ~NetworkMessageContentFlags.DataSetMessageHeader;
                    }
                    Messages.Add(message);
                    return true;
                }
                return false;
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
            if (!HasNetworkMessageHeader && HasSingleDataSetMessage)
            {
                Debug.Assert(messages.Length == 1);
                // Writes data set message or just data set
                messages[0].Encode(encoder, HasDataSetMessageHeader);
                return;
            }

            var typeName = nameof(AvroNetworkMessage);
            if (encoder is AvroEncoder schemas)
            {
                var schema = schemas.Current;
                if (schema is ArraySchema arr)
                {
                    schema = arr.ItemSchema;
                }
                typeName = schema.Name;
            }

            // Write network message
            encoder.WriteObject(null, typeName, () =>
            {
                if (HasNetworkMessageHeader)
                {
                    encoder.WriteString(nameof(MessageId), MessageId());
                    encoder.WriteString(nameof(MessageType), MessageType);
                    encoder.WriteString(nameof(PublisherId), PublisherId);
                    encoder.WriteGuid(nameof(DataSetClassId), DataSetClassId);
                    encoder.WriteString(nameof(DataSetWriterGroup), DataSetWriterGroup);
                }

                // We write array regardless of single or multi message
                encoder.WriteArray(nameof(Messages), messages,
                    v => v.Encode(encoder, HasDataSetMessageHeader));
            });
        }
    }
}
