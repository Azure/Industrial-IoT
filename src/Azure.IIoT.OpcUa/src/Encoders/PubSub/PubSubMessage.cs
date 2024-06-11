// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Avro;
    using Furly;
    using Microsoft.IO;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Encodeable PubSub messages
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public abstract class PubSubMessage
    {
        /// <summary>
        /// Message schema
        /// </summary>
        public abstract string MessageSchema { get; }

        /// <summary>
        /// Content type
        /// </summary>
        public abstract string ContentType { get; }

        /// <summary>
        /// Content encoding
        /// </summary>
        public abstract string? ContentEncoding { get; }

        /// <summary>
        /// Publisher identifier
        /// </summary>
        public string? PublisherId { get; set; }

        /// <summary>
        /// Dataset writerGroup
        /// </summary>
        public string? DataSetWriterGroup { get; set; }

        /// <summary>
        /// Memory stream manager
        /// </summary>
        protected static RecyclableMemoryStreamManager Memory { get; }
            = new RecyclableMemoryStreamManager();

        /// <summary>
        /// Decode the network message from the wire representation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reader"></param>
        /// <param name="resolver"></param>
        public abstract bool TryDecode(IServiceMessageContext context,
            Queue<ReadOnlySequence<byte>> reader,
            IDataSetMetaDataResolver? resolver = null);

        /// <summary>
        /// Decode the network message from the wire representation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="stream"></param>
        /// <param name="resolver"></param>
        public abstract bool TryDecode(IServiceMessageContext context,
            Stream stream, IDataSetMetaDataResolver? resolver = null);

        /// <summary>
        /// Encode the network message into network message chunks
        /// wire representation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="maxChunkSize"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public abstract IReadOnlyList<ReadOnlySequence<byte>> Encode(
            IServiceMessageContext context, int maxChunkSize,
            IDataSetMetaDataResolver? resolver = null);

        /// <summary>
        /// Decode pub sub messages from a single network buffer. If the message
        /// was chunked, the message might not be fully reconstituted.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="contentType"></param>
        /// <param name="context"></param>
        /// <param name="resolver"></param>
        /// <param name="messageSchema"></param>
        /// <returns></returns>
        public static PubSubMessage? Decode(ReadOnlySequence<byte> buffer, string contentType,
            IServiceMessageContext context, IDataSetMetaDataResolver? resolver = null,
            string? messageSchema = null)
        {
            var reader = new Queue<ReadOnlySequence<byte>>();
            reader.Enqueue(buffer);
            return DecodeOne(reader, contentType, context, resolver, messageSchema);
        }

        /// <summary>
        /// Decode all messages from the provided chunk reader. The reader
        /// is a queue of byte sequences, each representing a network buffer.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="contentType"></param>
        /// <param name="context"></param>
        /// <param name="resolver"></param>
        /// <param name="messageSchema"></param>
        /// <returns></returns>
        public static IEnumerable<PubSubMessage> Decode(Queue<ReadOnlySequence<byte>> reader,
            string contentType, IServiceMessageContext context,
            IDataSetMetaDataResolver? resolver = null, string? messageSchema = null)
        {
            while (true)
            {
                var message = DecodeOne(reader, contentType, context, resolver, messageSchema);
                if (message == null)
                {
                    yield break;
                }
                else
                {
                    yield return message;
                }
            }
        }

        /// <summary>
        /// Decode messages from stream. The stream will be read until the
        /// entire message has been loaded. UADP messages can be chunked
        /// the stream therefore must include all chunks to decode successfully.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="contentType"></param>
        /// <param name="context"></param>
        /// <param name="resolver"></param>
        /// <param name="messageSchema"></param>
        /// <returns></returns>
        public static IEnumerable<PubSubMessage> Decode(Stream stream,
            string contentType, IServiceMessageContext context,
            IDataSetMetaDataResolver? resolver = null, string? messageSchema = null)
        {
            while (stream.Position != stream.Length)
            {
                PubSubMessage message;
                long pos;
#pragma warning disable CA1308 // Normalize strings to uppercase
                switch (contentType.ToLowerInvariant())
                {
                    case Encoders.ContentType.JsonGzip:
                    case ContentMimeType.Json:
                    case Encoders.ContentType.UaJson:
                    case Encoders.ContentType.UaLegacyPublisher:
                    case Encoders.ContentType.UaNonReversibleJson:
                        message = new JsonNetworkMessage
                        {
                            MessageSchemaToUse = messageSchema,
                            UseGzipCompression = contentType.Equals(
                                Encoders.ContentType.JsonGzip,
                                StringComparison.OrdinalIgnoreCase)
                        };
                        pos = stream.Position;
                        if (message.TryDecode(context, stream, resolver))
                        {
                            yield return message;
                        }
                        else
                        {
                            stream.Position = pos;
                            message = new JsonMetaDataMessage();
                            if (message.TryDecode(context, stream, resolver))
                            {
                                yield return message;
                            }
                            else
                            {
                                yield break;
                            }
                        }
                        break;
                    case ContentMimeType.Binary:
                    case Encoders.ContentType.Uadp:
                        message = new UadpNetworkMessage();
                        pos = stream.Position;
                        if (message.TryDecode(context, stream, resolver))
                        {
                            yield return message;
                        }
                        else
                        {
                            stream.Position = pos;
                            message = new UadpDiscoveryMessage();
                            if (message.TryDecode(context, stream, resolver))
                            {
                                yield return message;
                            }
                            else
                            {
                                yield break;
                            }
                        }
                        break;

                    case Encoders.ContentType.Avro:
                    case Encoders.ContentType.AvroGzip:
                    case ContentMimeType.AvroBinary:
                        message = new AvroNetworkMessage
                        {
                            Schema = Schema.Parse(messageSchema),
                            UseGzipCompression = contentType.Equals(
                                Encoders.ContentType.AvroGzip, StringComparison.OrdinalIgnoreCase)
                        };
                        if (message.TryDecode(context, stream, resolver))
                        {
                            yield return message;
                        }
                        else
                        {
                            yield break;
                        }
                        break;
                    default:
                        break;
                }
#pragma warning restore CA1308 // Normalize strings to uppercase
            }
        }

        /// <summary>
        /// Decode one pub sub messages from the chunks provided by the reader
        /// Avro and JSON do not support chunking hence only ever one chunk is
        /// pulled from the reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="contentType"></param>
        /// <param name="context"></param>
        /// <param name="resolver"></param>
        /// <param name="messageSchema"></param>
        /// <returns></returns>
        internal static PubSubMessage? DecodeOne(Queue<ReadOnlySequence<byte>> reader,
            string contentType, IServiceMessageContext context,
            IDataSetMetaDataResolver? resolver, string? messageSchema = null)
        {
            if (reader.Count == 0)
            {
                return null;
            }
            PubSubMessage message;
#pragma warning disable CA1308 // Normalize strings to uppercase
            switch (contentType.ToLowerInvariant())
            {
                case Encoders.ContentType.JsonGzip:
                case ContentMimeType.Json:
                case Encoders.ContentType.UaJson:
                case Encoders.ContentType.UaLegacyPublisher:
                case Encoders.ContentType.UaNonReversibleJson:
                    message = new JsonNetworkMessage
                    {
                        MessageSchemaToUse = messageSchema,
                        UseGzipCompression = contentType.Equals(
                            Encoders.ContentType.JsonGzip, StringComparison.OrdinalIgnoreCase)
                    };
                    if (message.TryDecode(context, reader, resolver))
                    {
                        return message;
                    }
                    if (reader.Count == 0)
                    {
                        return null;
                    }
                    message = new JsonMetaDataMessage();
                    if (message.TryDecode(context, reader, resolver))
                    {
                        return message;
                    }
                    break;
                case ContentMimeType.Binary:
                case Encoders.ContentType.Uadp:
                    message = new UadpNetworkMessage();
                    if (message.TryDecode(context, reader, resolver))
                    {
                        return message;
                    }
                    if (reader.Count == 0)
                    {
                        return null;
                    }
                    message = new UadpDiscoveryMessage();
                    if (message.TryDecode(context, reader, resolver))
                    {
                        return message;
                    }
                    break;

                case Encoders.ContentType.Avro:
                case Encoders.ContentType.AvroGzip:
                case ContentMimeType.AvroBinary:
                    message = new AvroNetworkMessage
                    {
                        Schema = Schema.Parse(messageSchema),
                        UseGzipCompression = contentType.Equals(
                            Encoders.ContentType.AvroGzip, StringComparison.OrdinalIgnoreCase)
                    };
                    if (message.TryDecode(context, reader, resolver))
                    {
                        return message;
                    }
                    if (reader.Count == 0)
                    {
                        return null;
                    }
                    break;
                default:
                    break;
            }
#pragma warning restore CA1308 // Normalize strings to uppercase
            // Failed
            return null;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not PubSubMessage wrapper)
            {
                return false;
            }
            if (!Utils.IsEqual(wrapper.PublisherId, PublisherId))
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(PublisherId);
            return hash.ToHashCode();
        }
    }
}
