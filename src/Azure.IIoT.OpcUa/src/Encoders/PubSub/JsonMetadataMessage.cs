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
    using System.Text;

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
                // !Opc.Ua.Utils.IsEqual(wrapper.MetaData, MetaData) ||  TODO
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
                    using var decoder = new JsonDecoderEx(
                        (Stream?)compression ?? memoryStream, context, useJsonLoader: false);
                    if (TryDecode(decoder))
                    {
                        reader.Dequeue();
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
            using var memoryStream = Memory.GetStream();
            var compression = UseGzipCompression ?
                new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true) : null;
            try
            {
                using var encoder = new JsonEncoderEx(
                    (Stream?)compression ?? memoryStream, context)
                {
                    UseAdvancedEncoding = UseAdvancedEncoding,
                    NamespaceFormat = NamespaceFormat,
                    UseUriEncoding = UseAdvancedEncoding,
                    IgnoreDefaultValues = true,
                    IgnoreNullValues = true,
                    UseReversibleEncoding = false
                };
                Encode(encoder);
            }
            finally
            {
                compression?.Dispose();
            }
            var messageBuffer = memoryStream.GetReadOnlySequence();
            if (messageBuffer.Length < maxChunkSize)
            {
                // TODO: instead of copy using ToArray we shall include the
                // stream with the message and dispose it later when it is
                // consumed.
                chunks.Add(new ReadOnlySequence<byte>(messageBuffer.ToArray()));
            }
            else
            {
                chunks.Add(default);
            }
            return chunks;
        }

        /// <summary>
        /// Encode metadata
        /// </summary>
        /// <param name="encoder"></param>
        /// <exception cref="EncodingException"></exception>
        internal void Encode(Opc.Ua.IEncoder encoder)
        {
            if (MetaData == null)
            {
                throw new EncodingException("No metadata to encode.");
            }
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
            var dataSetMetaData = MetaData.ToStackModel(encoder.Context);
            encoder.WriteEncodeable(nameof(MetaData), dataSetMetaData,
                typeof(Opc.Ua.DataSetMetaDataType));
            if (!string.IsNullOrEmpty(DataSetWriterName))
            {
                encoder.WriteString(nameof(DataSetWriterName), DataSetWriterName);
            }
        }

        /// <inheritdoc/>
        internal bool TryDecode(Opc.Ua.IDecoder decoder)
        {
            MessageId = decoder.ReadString(nameof(MessageId));
            var messageType = decoder.ReadString(nameof(MessageType));
            if (!messageType.Equals(MessageTypeUaMetadata, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            PublisherId = decoder.ReadString(nameof(PublisherId));
            DataSetWriterId = decoder.ReadUInt16(nameof(DataSetWriterId));
            var dataSetMetaData = (Opc.Ua.DataSetMetaDataType)decoder.ReadEncodeable(
                nameof(MetaData), typeof(Opc.Ua.DataSetMetaDataType));
            MetaData = dataSetMetaData.ToServiceModel(decoder.Context);
            DataSetWriterName = decoder.ReadString(nameof(DataSetWriterName));
            return true;
        }
    }
}
