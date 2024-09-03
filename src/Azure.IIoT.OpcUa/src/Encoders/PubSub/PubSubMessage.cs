// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Microsoft.IO;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    /// <summary>
    /// Encodeable PubSub messages
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public abstract class PubSubMessage
    {
        /// <summary>
        /// Network message defaults
        /// </summary>
        public const NetworkMessageContentFlags DefaultNetworkMessageContentFlags =
            NetworkMessageContentFlags.NetworkMessageHeader |
            NetworkMessageContentFlags.NetworkMessageNumber |
            NetworkMessageContentFlags.DataSetMessageHeader |
            NetworkMessageContentFlags.PublisherId |
            NetworkMessageContentFlags.DataSetClassId;

        /// <summary>
        /// Default field mask
        /// </summary>
        public const DataSetFieldContentFlags DefaultDataSetFieldContentFlags =
            DataSetFieldContentFlags.StatusCode |
            DataSetFieldContentFlags.SourcePicoSeconds |
            DataSetFieldContentFlags.SourceTimestamp |
            DataSetFieldContentFlags.ServerPicoSeconds |
            DataSetFieldContentFlags.ServerTimestamp;

        /// <summary>
        /// Default message flags
        /// </summary>
        public const DataSetMessageContentFlags DefaultDataSetMessageContentFlags =
            DataSetMessageContentFlags.DataSetWriterId |
            DataSetMessageContentFlags.DataSetWriterName |
            DataSetMessageContentFlags.MetaDataVersion |
            DataSetMessageContentFlags.MajorVersion |
            DataSetMessageContentFlags.MinorVersion |
            DataSetMessageContentFlags.SequenceNumber |
            DataSetMessageContentFlags.Timestamp |
            DataSetMessageContentFlags.MessageType |
            DataSetMessageContentFlags.Status;

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
        public abstract bool TryDecode(Opc.Ua.IServiceMessageContext context,
            Queue<ReadOnlySequence<byte>> reader,
            IDataSetMetaDataResolver? resolver = null);

        /// <summary>
        /// Decode the network message from the wire representation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="stream"></param>
        /// <param name="resolver"></param>
        public abstract bool TryDecode(Opc.Ua.IServiceMessageContext context,
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
            Opc.Ua.IServiceMessageContext context, int maxChunkSize,
            IDataSetMetaDataResolver? resolver = null);

        /// <summary>
        /// Create metadata message
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="publisherId"></param>
        /// <param name="writerGroupName"></param>
        /// <param name="dataSetWriterName"></param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="metaData"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="standardsCompliant"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool TryCreateMetaDataMessage(MessageEncoding encoding,
            string? publisherId, string writerGroupName, string dataSetWriterName,
            ushort dataSetWriterId, PublishedDataSetMetaDataModel metaData,
            NamespaceFormat namespaceFormat, bool standardsCompliant,
            [NotNullWhen(true)] out PubSubMessage? message)
        {
            if (encoding.HasFlag(MessageEncoding.Json))
            {
                message = new JsonMetaDataMessage
                {
                    UseAdvancedEncoding = !standardsCompliant,
                    NamespaceFormat = namespaceFormat,
                    UseGzipCompression = encoding.HasFlag(MessageEncoding.IsGzipCompressed),
                    DataSetWriterId = dataSetWriterId,
                    MetaData = metaData,
                    MessageId = Guid.NewGuid().ToString(),
                    DataSetWriterName = dataSetWriterName,
                    PublisherId = publisherId,
                    DataSetWriterGroup = writerGroupName
                };
            }
            else if (encoding.HasFlag(MessageEncoding.Uadp))
            {
                message = new UadpMetaDataMessage
                {
                    DataSetWriterId = dataSetWriterId,
                    MetaData = metaData,
                    PublisherId = publisherId,
                    DataSetWriterGroup = writerGroupName
                };
            }
            else
            {
                message = default;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create dataset message
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="dataSetWriterName"></param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="dataSetMessageContentFlags"></param>
        /// <param name="messageType"></param>
        /// <param name="payload"></param>
        /// <param name="timestamp"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="standardsCompliant"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="applicationUri"></param>
        /// <param name="metaData"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool TryCreateDataSetMessage(MessageEncoding encoding,
            string dataSetWriterName, ushort dataSetWriterId,
            DataSetMessageContentFlags? dataSetMessageContentFlags,
            MessageType messageType, DataSet payload, DateTimeOffset? timestamp,
            uint sequenceNumber, bool standardsCompliant, string? endpointUrl,
            string? applicationUri, PublishedDataSetMetaDataModel? metaData,
            [NotNullWhen(true)] out BaseDataSetMessage? message)
        {
            dataSetMessageContentFlags ??= DefaultDataSetMessageContentFlags;
            var version = new Opc.Ua.ConfigurationVersionDataType
            {
                MajorVersion = metaData?.DataSetMetaData.MajorVersion ?? 1,
                MinorVersion = metaData?.MinorVersion ?? 0
            };

            if (encoding.HasFlag(MessageEncoding.Json))
            {
                message = new JsonDataSetMessage
                {
                    UseCompatibilityMode = !standardsCompliant,
                    DataSetWriterName = dataSetWriterName,
                    DataSetWriterId = dataSetWriterId,
                    MessageType = messageType,
                    MetaDataVersion = version,
                    ApplicationUri = applicationUri,
                    EndpointUrl = endpointUrl,
                    DataSetMessageContentMask = dataSetMessageContentFlags.Value,
                    Timestamp = timestamp,
                    SequenceNumber = sequenceNumber,
                    Payload = payload
                };
            }
            else if (encoding.HasFlag(MessageEncoding.Avro))
            {
                message = new AvroDataSetMessage
                {
                    DataSetWriterName = dataSetWriterName,
                    DataSetWriterId = dataSetWriterId,
                    MessageType = messageType,
                    MetaDataVersion = version,
                    DataSetMessageContentMask = dataSetMessageContentFlags.Value,
                    Timestamp = timestamp,
                    SequenceNumber = sequenceNumber,
                    Payload = payload
                };
            }
            else if (encoding.HasFlag(MessageEncoding.Uadp))
            {
                message = new UadpDataSetMessage
                {
                    DataSetWriterId = dataSetWriterId,
                    MessageType = messageType,
                    MetaDataVersion = version,
                    DataSetMessageContentMask = dataSetMessageContentFlags.Value,
                    Timestamp = timestamp,
                    SequenceNumber = sequenceNumber,
                    Payload = payload
                };
            }
            else
            {
                message = default;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create monitored item message
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="writerGroupName"></param>
        /// <param name="dataSetMessageContentFlags"></param>
        /// <param name="messageType"></param>
        /// <param name="timestamp"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="payload"></param>
        /// <param name="nodeId"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="applicationUri"></param>
        /// <param name="standardsCompliant"></param>
        /// <param name="extensionFields"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool TryCreateMonitoredItemMessage(MessageEncoding encoding,
            string? writerGroupName, DataSetMessageContentFlags? dataSetMessageContentFlags,
            MessageType messageType, DateTimeOffset? timestamp, uint sequenceNumber,
            DataSet payload, string? nodeId, string? endpointUrl, string? applicationUri,
            bool standardsCompliant, IDictionary<string, VariantValue>? extensionFields,
            [NotNullWhen(true)] out BaseDataSetMessage? message)
        {
            if (encoding.HasFlag(MessageEncoding.Json))
            {
                message = new MonitoredItemMessage
                {
                    UseCompatibilityMode = !standardsCompliant,
                    ApplicationUri = applicationUri,
                    EndpointUrl = endpointUrl,
                    NodeId = nodeId,
                    ExtensionFields = extensionFields,
                    WriterGroupId = writerGroupName,
                    MessageType = messageType,
                    DataSetMessageContentMask = dataSetMessageContentFlags
                      ?? DefaultDataSetMessageContentFlags,
                    Timestamp = timestamp,
                    SequenceNumber = sequenceNumber,
                    Payload = payload
                };
            }
            else
            {
                message = default;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create network message
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="publisherId"></param>
        /// <param name="writerGroupName"></param>
        /// <param name="networkMessageContentFlags"></param>
        /// <param name="dataSetClassId"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="timestamp"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="standardsCompliant"></param>
        /// <param name="isBatched"></param>
        /// <param name="schema"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool TryCreateNetworkMessage(MessageEncoding encoding,
            string publisherId, string writerGroupName,
            NetworkMessageContentFlags? networkMessageContentFlags,
            Guid dataSetClassId, Func<ushort> sequenceNumber, DateTimeOffset timestamp,
            NamespaceFormat namespaceFormat, bool standardsCompliant, bool isBatched,
            IEventSchema? schema, [NotNullWhen(true)] out BaseNetworkMessage? message)
        {
            if (encoding.HasFlag(MessageEncoding.Json))
            {
                message = new JsonNetworkMessage
                {
                    UseAdvancedEncoding = !standardsCompliant,
                    UseGzipCompression = encoding.HasFlag(MessageEncoding.IsGzipCompressed),
                    NamespaceFormat = namespaceFormat,
                    UseArrayEnvelope = !standardsCompliant && isBatched,
                    MessageId = () => Guid.NewGuid().ToString(),
                    NetworkMessageContentMask = networkMessageContentFlags
                        ?? DefaultNetworkMessageContentFlags,
                    PublisherId = publisherId,
                    DataSetClassId = dataSetClassId,
                    DataSetWriterGroup = writerGroupName
                };
            }
            else if (encoding.HasFlag(MessageEncoding.Avro))
            {
                message = new AvroNetworkMessage
                {
                    Schema = (schema is Schemas.Avro.IAvroSchema s) ? s.Schema :
                        schema == null ? null : Avro.Schema.Parse(schema.Schema),
                    UseGzipCompression = encoding.HasFlag(MessageEncoding.IsGzipCompressed),
                    MessageId = () => Guid.NewGuid().ToString(),
                    NetworkMessageContentMask = networkMessageContentFlags
                        ?? DefaultNetworkMessageContentFlags,
                    PublisherId = publisherId,
                    DataSetClassId = dataSetClassId,
                    DataSetWriterGroup = writerGroupName
                };
            }
            else if (encoding.HasFlag(MessageEncoding.Uadp))
            {
                message = new UadpNetworkMessage
                {
                    //   WriterGroupId = writerGroup.Index,
                    //   GroupVersion = writerGroup.Version,
                    SequenceNumber = sequenceNumber,
                    Timestamp = timestamp,
                    PicoSeconds = 0,
                    NetworkMessageContentMask = networkMessageContentFlags
                        ?? DefaultNetworkMessageContentFlags,
                    PublisherId = publisherId,
                    DataSetClassId = dataSetClassId,
                    DataSetWriterGroup = writerGroupName
                };
            }
            else
            {
                message = default;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create network message schema
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="networkMessage"></param>
        /// <param name="schema"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static bool TryCreateNetworkMessageSchema(MessageEncoding encoding,
            PublishedNetworkMessageSchemaModel networkMessage,
            [NotNullWhen(true)] out IEventSchema? schema, SchemaOptions? options = null)
        {
            if (encoding.HasFlag(MessageEncoding.Json) && options?.PreferAvroOverJsonSchema == true)
            {
                schema = new Schemas.Avro.JsonNetworkMessage(networkMessage, options);
            }
            else if (encoding.HasFlag(MessageEncoding.Json))
            {
                schema = new Schemas.Json.JsonNetworkMessage(networkMessage, options);
            }
            else if (encoding.HasFlag(MessageEncoding.Avro))
            {
                schema = new Schemas.Avro.AvroNetworkMessage(networkMessage, options);
            }
            else if (encoding.HasFlag(MessageEncoding.Uadp))
            {
                schema = new Schemas.Uadp.UadpNetworkMessage(networkMessage);
            }
            else
            {
                schema = default;
                return false;
            }
            return true;
        }

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
            Opc.Ua.IServiceMessageContext context, IDataSetMetaDataResolver? resolver = null,
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
            string contentType, Opc.Ua.IServiceMessageContext context,
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
            string contentType, Opc.Ua.IServiceMessageContext context,
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
                            Schema = Avro.Schema.Parse(messageSchema),
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
            string contentType, Opc.Ua.IServiceMessageContext context,
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
                        Schema = Avro.Schema.Parse(messageSchema),
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
            if (!Opc.Ua.Utils.IsEqual(wrapper.PublisherId, PublisherId))
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
