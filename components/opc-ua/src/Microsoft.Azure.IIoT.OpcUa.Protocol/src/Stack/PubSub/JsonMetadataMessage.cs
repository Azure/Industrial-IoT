// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Opc.Ua.Encoders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    /// <summary>
    /// Json discovery metdata message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class JsonMetaDataMessage : PubSubMessage {

        /// <inheritdoc/>
        public override string MessageSchema => MessageSchemaTypes.NetworkMessageJson;

        /// <inheritdoc/>
        public override string ContentType => UseGzipCompression ? ContentMimeType.JsonGzip : ContentMimeType.Json;

        /// <inheritdoc/>
        public override string ContentEncoding => Encoding.UTF8.EncodingName;

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
        /// Use gzip compression
        /// </summary>
        public bool UseGzipCompression { get; set; }

        /// <summary>
        /// Message id
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Dataset writerGroup
        /// </summary>
        public string DataSetWriterGroup { get; set; }

        /// <summary>
        /// Data set writer name in case of ua-metadata message
        /// </summary>
        public ushort DataSetWriterId { get; set; }

        /// <summary>
        /// Data set writer name in case of ua-metadata message
        /// </summary>
        public string DataSetWriterName { get; set; }

        /// <summary>
        /// Data set metadata in case this is a metadata message
        /// </summary>
        public DataSetMetaDataType MetaData { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object value) {
            if (ReferenceEquals(this, value)) {
                return true;
            }
            if (!(value is JsonMetaDataMessage wrapper)) {
                return false;
            }
            if (!base.Equals(value)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.MessageId, MessageId) ||
                !Utils.IsEqual(wrapper.DataSetWriterGroup, DataSetWriterGroup) ||
                !Utils.IsEqual(wrapper.DataSetWriterName, DataSetWriterName) ||
                !Utils.IsEqual(wrapper.DataSetWriterId, DataSetWriterId) ||
                !Utils.IsEqual(wrapper.MetaData, MetaData)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
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
        public override bool TryDecode(IServiceMessageContext context,
            Queue<byte[]> reader, IDataSetMetaDataResolver resolver) {
            if (reader.TryPeek(out var buffer)) {
                using var memoryStream = Memory.GetStream(buffer);
                var compression = UseGzipCompression ?
                    new GZipStream(memoryStream, CompressionMode.Decompress, leaveOpen: true) : null;
                try {
                    using var decoder = new JsonDecoderEx(
                        UseGzipCompression ? compression : memoryStream, context, useJsonLoader: false); ;
                    if (TryDecode(decoder)) {
                        reader.Dequeue();
                    }
                }
                finally {
                    compression?.Dispose();
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<byte[]> Encode(IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver resolver) {
            var chunks = new List<byte[]>();
            using var memoryStream = Memory.GetStream();
            var compression = UseGzipCompression ?
                new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true) : null;
            try {
                using var encoder = new JsonEncoderEx(
                    UseGzipCompression ? compression : memoryStream, context) {
                    UseAdvancedEncoding = UseAdvancedEncoding,
                    UseUriEncoding = UseAdvancedEncoding,
                    IgnoreDefaultValues = UseAdvancedEncoding,
                    IgnoreNullValues = true,
                    UseReversibleEncoding = false,
                };
                Encode(encoder);
            }
            finally {
                compression?.Dispose();
            }
            // TODO: instead of copy using ToArray we shall include the
            // stream with the message and dispose it later when it is
            // consumed.
            var messageBuffer = memoryStream.ToArray();
            if (messageBuffer.Length < maxChunkSize) {
                chunks.Add(messageBuffer);
            }
            else {
                chunks.Add(null);
            }
            return chunks;
        }

        /// <summary>
        /// Encode metadata
        /// </summary>
        /// <param name="encoder"></param>
        internal void Encode(IEncoder encoder) {
            encoder.WriteString(nameof(MessageId), MessageId);
            encoder.WriteString(nameof(MessageType), MessageType);

            if (!string.IsNullOrEmpty(PublisherId)) {
                encoder.WriteString(nameof(PublisherId), PublisherId);
            }
            if (DataSetWriterId != 0) {
                encoder.WriteUInt16(nameof(DataSetWriterId), DataSetWriterId);
            }
            if (!string.IsNullOrEmpty(DataSetWriterGroup)) {
                encoder.WriteString(nameof(DataSetWriterGroup), DataSetWriterGroup);
            }
            encoder.WriteEncodeable(nameof(MetaData), MetaData, typeof(DataSetMetaDataType));
            if (!string.IsNullOrEmpty(DataSetWriterName)) {
                encoder.WriteString(nameof(DataSetWriterName), DataSetWriterName);
            }
        }

        /// <inheritdoc/>
        internal bool TryDecode(IDecoder decoder) {
            MessageId = decoder.ReadString(nameof(MessageId));
            var messageType = decoder.ReadString(nameof(MessageType));
            if (!messageType.Equals(MessageTypeUaMetadata, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }
            PublisherId = decoder.ReadString(nameof(PublisherId));
            DataSetWriterId = decoder.ReadUInt16(nameof(DataSetWriterId));
            MetaData = (DataSetMetaDataType)decoder.ReadEncodeable(nameof(MetaData), typeof(DataSetMetaDataType));
            DataSetWriterName = decoder.ReadString(nameof(DataSetWriterName));
            return true;
        }
    }
}