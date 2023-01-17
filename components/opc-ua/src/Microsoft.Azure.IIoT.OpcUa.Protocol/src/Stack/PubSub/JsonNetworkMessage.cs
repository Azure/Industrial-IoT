﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Opc.Ua.Encoders;
    using System;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Json Network message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class JsonNetworkMessage : BaseNetworkMessage {

        /// <inheritdoc/>
        public override string MessageSchema => HasSamplesPayload ?
            MessageSchemaTypes.MonitoredItemMessageJson : MessageSchemaTypes.NetworkMessageJson;

        /// <inheritdoc/>
        public override string ContentType => UseGzipCompression ?
            ContentMimeType.JsonGzip : ContentMimeType.Json;

        /// <inheritdoc/>
        public override string ContentEncoding => Encoding.UTF8.EncodingName;

        /// <summary>
        /// Ua data message type
        /// </summary>
        public const string MessageTypeUaData = "ua-data";

        /// <summary>
        /// Message id
        /// </summary>
        public Func<string> MessageId { get; set; } = () => Guid.NewGuid().ToString();

        /// <summary>
        /// Dataset writerGroup
        /// </summary>
        public string DataSetWriterGroup { get; set; }

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
        /// Flag that indicates if the Network message payload is monitored item samples
        /// </summary>
        public bool HasSamplesPayload {
            get {
                if (_hasSamplesPayload == null) {
                    if (Messages.Count > 0) {
                        _hasSamplesPayload = Messages.Any(m => m is MonitoredItemMessage);
                    }
                    else {
                        return false;
                    }
                }
                return _hasSamplesPayload.Value;
            }
            set {
                _hasSamplesPayload = value;
            }
        }

        /// <summary>
        /// Sets the message schema to use
        /// </summary>
        internal string MessageSchemaToUse {
            get {
                return MessageSchema;
            }
            set {
                if (value != null &&
                    value.Equals(MessageSchemaTypes.MonitoredItemMessageJson, StringComparison.OrdinalIgnoreCase)) {
                    HasSamplesPayload = true;
                }
                else {
                    HasSamplesPayload = false;
                }
            }
        }

        /// <summary>
        /// Flag that indicates if advanced encoding should be used
        /// </summary>
        public bool UseAdvancedEncoding { get; set; }

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
        private JsonEncoderEx.JsonEncoding JsonEncoderStartingState {
            get {
                if (!HasNetworkMessageHeader) {
                    if (!HasSingleDataSetMessage || UseArrayEnvelope) {
                        return JsonEncoderEx.JsonEncoding.Array;
                    }
                    if (!HasDataSetMessageHeader) {
                        return JsonEncoderEx.JsonEncoding.Token;
                    }
                }
                if (UseArrayEnvelope) {
                    return JsonEncoderEx.JsonEncoding.Array;
                }
                return JsonEncoderEx.JsonEncoding.Object;
            }
        }

        /// <summary>
        /// Create message
        /// </summary>
        public JsonNetworkMessage() {
            MessageId = () => _messageId;
        }

        /// <inheritdoc/>
        public override bool Equals(object value) {
            if (ReferenceEquals(this, value)) {
                return true;
            }
            if (!(value is JsonNetworkMessage wrapper)) {
                return false;
            }
            if (!base.Equals(value)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.MessageId(), MessageId()) ||
                !Utils.IsEqual(wrapper.DataSetWriterGroup, DataSetWriterGroup)) {
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
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override bool TryDecode(IServiceMessageContext context, Queue<byte[]> reader,
            IDataSetMetaDataResolver resolver = null) {
            // Decodes a single buffer
            if (reader.TryPeek(out var buffer)) {
                using (var memoryStream = Memory.GetStream(buffer)) {
                    var compression = UseGzipCompression ?
                        new GZipStream(memoryStream, CompressionMode.Decompress, leaveOpen: true) : null;
                    try {
                        using var decoder = new JsonDecoderEx(UseGzipCompression ?
                            compression : memoryStream, context, useJsonLoader: false);
                        if (!decoder.ReadArray(null, () => TryReadNetworkMessage(decoder)).All(s => s)) {
                            return false;
                        }
                        // Complete the buffer
                        reader.Dequeue();
                        return true;
                    }
                    finally {
                        compression?.Dispose();
                    }
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<byte[]> Encode(IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver resolver = null) {
            var chunks = new List<byte[]>();
            var messages = Messages.OfType<JsonDataSetMessage>().ToArray().AsSpan();
            var messageId = MessageId;
            try {
                if (HasSingleDataSetMessage && !UseArrayEnvelope) {
                    for (var i = 0; i < messages.Length; i++) {
                        EncodeMessages(messages.Slice(i, 1));
                    }
                }
                else {
                    EncodeMessages(messages);
                }
            }
            finally {
                MessageId = messageId;
            }
            return chunks;

            void EncodeMessages(Span<JsonDataSetMessage> messages) {
                byte[] messageBuffer;
                using (var memoryStream = Memory.GetStream()) {
                    var compression = UseGzipCompression ?
                        new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true) : null;
                    try {
                        using var encoder = new JsonEncoderEx(
                            UseGzipCompression ? compression : memoryStream, context, JsonEncoderStartingState) {
                            UseAdvancedEncoding = UseAdvancedEncoding,
                            UseUriEncoding = UseAdvancedEncoding,
                            IgnoreDefaultValues = true,
                            IgnoreNullValues = true,
                            UseReversibleEncoding = false
                        };
                        WriteMessages(encoder, messages);
                    }
                    finally {
                        compression?.Dispose();
                    }

                    // TODO: instead of copy using ToArray we shall include the
                    // stream with the message and dispose it later when it is
                    // consumed.
                    messageBuffer = memoryStream.ToArray();
                }

                if (messageBuffer.Length < maxChunkSize) {
                    chunks.Add(messageBuffer);
                }
                else if (messages.Length == 1) {
                    chunks.Add(null);
                }
                else {
                    // Split
                    var len = messages.Length / 2;
                    var first = messages.Slice(0, len);
                    var second = messages.Slice(len);

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
        private void WriteMessages(JsonEncoderEx encoder, Span<JsonDataSetMessage> messages) {
            var messagesToInclude = messages.ToArray();
            if (UseArrayEnvelope) {
                if (HasSingleDataSetMessage || HasNetworkMessageHeader) {
                    // Legacy compatibility - n network messages with 1 message each inside array
                    for (var i = 0; i < messages.Length; i++) {
                        var single = messages.Slice(i, 1).ToArray();
                        if (HasDataSetMessageHeader || HasNetworkMessageHeader) {
                            encoder.WriteObject(null, Messages, _ => WriteNetworkMessage(encoder, single));
                        }
                        else {
                            // Write single messages into the array envelope
                            WriteNetworkMessage(encoder, single);
                        }
                    }
                }
                else {
                    // Write all messages into the array envelope
                    WriteNetworkMessage(encoder, messagesToInclude);
                }
            }
            else {
                WriteNetworkMessage(encoder, messagesToInclude);
            }
        }

        /// <summary>
        /// Try decode
        /// </summary>
        /// <param name="decoder"></param>
        /// <returns></returns>
        private bool TryReadNetworkMessage(JsonDecoderEx decoder) {
            if (!HasSamplesPayload && TryReadNetworkMessageHeader(decoder, out var networkMessageContentMask)) {
                if (decoder.IsObject(nameof(Messages))) {
                    // Single message
                    networkMessageContentMask |= (uint)JsonNetworkMessageContentMask.SingleDataSetMessage;
                }
                else if (!decoder.IsArray(nameof(Messages))) {
                    // Messages property is neither object nor array. We might be inside a single dataset
                    // TODO: Should we throw?
                    return false;
                }
                NetworkMessageContentMask = networkMessageContentMask;
                return TryReadDataSetMessages(decoder, nameof(Messages));
            }
            else {
                // Reset
                NetworkMessageContentMask = 0;
                DataSetWriterGroup = null;
                DataSetClassId = default;
                MessageId = null;
                PublisherId = null;

                if (decoder.IsObject(null)) {
                    // Treat this object as the single message
                    NetworkMessageContentMask |= (uint)JsonNetworkMessageContentMask.SingleDataSetMessage;
                }
                else if (!decoder.IsArray(null)) {
                    // This object we are reading is neither an object nor array
                    return false;
                }
                return TryReadDataSetMessages(decoder, null);
            }

            bool TryReadDataSetMessages(JsonDecoderEx decoder, string property) {
                var hasDataSetMessageHeader = false;
                string publisherId = null;
                var messages = decoder.ReadArray<BaseDataSetMessage>(property, () => {
                    var message = !HasSamplesPayload ? new JsonDataSetMessage() : new MonitoredItemMessage();
                    if (!message.TryDecode(decoder, property, ref hasDataSetMessageHeader, ref publisherId)) {
                        return null;
                    }
                    return message;
                });
                // Add decoded messages to messages array
                foreach (var message in messages) {
                    if (message == null) {
                        // Reset
                        Messages.Clear();
                        return false;
                    }
                    Messages.Add(message);
                }
                if (hasDataSetMessageHeader) {
                    NetworkMessageContentMask |= (uint)JsonNetworkMessageContentMask.DataSetMessageHeader;
                }
                if (publisherId != null) {
                    NetworkMessageContentMask |= (uint)JsonNetworkMessageContentMask.PublisherId;
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
        private void WriteNetworkMessage(JsonEncoderEx encoder, JsonDataSetMessage[] messages) {
            var publisherId =
                (NetworkMessageContentMask & (uint)JsonNetworkMessageContentMask.PublisherId) == 0
                    ? null : PublisherId;
            if (HasNetworkMessageHeader) {
                WriteNetworkMessageHeader(encoder);

                if (HasSingleDataSetMessage) {
                    if (HasDataSetMessageHeader) {
                        // Write as a single object under messages property
                        encoder.WriteObject(nameof(Messages), messages[0],
                            v => v.Encode(encoder, publisherId, true, null));
                    }
                    else {
                        // Write raw data set object under messages property
                        messages[0].Encode(encoder, publisherId, false, nameof(Messages));
                    }
                }
                else if (HasDataSetMessageHeader) {
                    // Write as array of objects
                    encoder.WriteArray(nameof(Messages), messages, v =>
                        encoder.WriteObject(null, v, v => v.Encode(encoder, publisherId, true, null)));
                }
                else {
                    // Write as array of dataset payload tokens
                    encoder.WriteArray(nameof(Messages), messages,
                        v => v.Encode(encoder, publisherId, false, null));
                }
            }
            else {
                // The encoder was set up as array or object beforehand
                if (HasSingleDataSetMessage) {
                    // Write object content to current object
                    messages[0].Encode(encoder, publisherId, HasDataSetMessageHeader, null);
                }
                else if (HasDataSetMessageHeader) {
                    // Write each object to the array that is the initial state of the encoder
                    foreach (var message in messages) {
                        // Write as array of dataset messages with payload
                        encoder.WriteObject(null, message, v => v.Encode(encoder, publisherId, true, null));
                    }
                }
                else {
                    // Writes dataset directly the encoder was set up as token
                    foreach (var message in messages) {
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
        private bool TryReadNetworkMessageHeader(JsonDecoderEx decoder, out uint networkMessageContentMask) {
            networkMessageContentMask = 0;
            if (!decoder.HasField(nameof(MessageId)) || HasSamplesPayload) {
                return false;
            }
            _messageId = decoder.ReadString(nameof(MessageId));
            if (MessageId == null) {
                // Field is there but not of type string, cannot be a network message header
                return false;
            }
            var messageType = decoder.ReadString(nameof(MessageType));
            if (!messageType.Equals(MessageTypeUaData, StringComparison.InvariantCultureIgnoreCase)) {
                // Not a dataset network message
                return false;
            }
            networkMessageContentMask |= (uint)JsonNetworkMessageContentMask.NetworkMessageHeader;

            if (decoder.HasField(nameof(PublisherId))) {
                PublisherId = decoder.ReadString(nameof(PublisherId));
                if (PublisherId != null) {
                    networkMessageContentMask |= (uint)JsonNetworkMessageContentMask.PublisherId;
                }
                else {
                    // publisher is not string type,
                    // TODO
                    return false;
                }
            }

            if (decoder.HasField(nameof(DataSetClassId))) {
                var dataSetClassId = decoder.ReadString(nameof(DataSetClassId));
                if (dataSetClassId != null && Guid.TryParse(dataSetClassId, out var result)) {
                    DataSetClassId = result;
                    networkMessageContentMask |= (uint)JsonNetworkMessageContentMask.DataSetClassId;
                }
                else {
                    // class id is not guid or string
                    return false;
                }
            }

            if (decoder.HasField(nameof(DataSetWriterGroup))) {
                DataSetWriterGroup = decoder.ReadString(nameof(DataSetWriterGroup));
                if (DataSetWriterGroup == null) {
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
        private void WriteNetworkMessageHeader(JsonEncoderEx encoder) {
            // The encoder was set up as object beforehand based on IsJsonArray result
            encoder.WriteString(nameof(MessageId), MessageId());
            encoder.WriteString(nameof(MessageType), MessageType);
            if ((NetworkMessageContentMask & (uint)JsonNetworkMessageContentMask.PublisherId) != 0) {
                encoder.WriteString(nameof(PublisherId), PublisherId);
            }
            if ((NetworkMessageContentMask & (uint)JsonNetworkMessageContentMask.DataSetClassId) != 0 &&
                DataSetClassId != Guid.Empty) {
                encoder.WriteString(nameof(DataSetClassId), DataSetClassId.ToString());
            }
            if (!string.IsNullOrEmpty(DataSetWriterGroup)) {
                encoder.WriteString(nameof(DataSetWriterGroup), DataSetWriterGroup);
            }
        }

        private bool? _hasSamplesPayload;
        /// <summary> To update message id </summary>
        protected string _messageId;
    }
}