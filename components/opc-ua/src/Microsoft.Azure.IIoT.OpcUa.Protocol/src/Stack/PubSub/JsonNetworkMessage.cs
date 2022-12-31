// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Opc.Ua.Encoders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    /// Json Network message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class JsonNetworkMessage : BaseNetworkMessage {

        /// <summary>
        /// Create network message
        /// </summary>
        /// <param name="initialContentMask"></param>
        public JsonNetworkMessage(JsonNetworkMessageContentMask initialContentMask = 0) {
            NetworkMessageContentMask = (uint)initialContentMask;
        }

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

        /// <inheritdoc/>
        public override bool TryDecode(IServiceMessageContext context, IEnumerable<byte[]> reader) {

            return false;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<byte[]> Encode(IServiceMessageContext context, int maxChunkSize) {
            var chunks = new List<byte[]>();
            var messages = Messages.ToArray().AsSpan();
            if (HasSingleDataSetMessage && !UseArrayEnvelope) {
                for (var i = 0; i < messages.Length; i++) {
                    EncodeMessages(messages.Slice(i, 1));
                }
            }
            else {
                EncodeMessages(messages);
            }
            return chunks;

            void EncodeMessages(Span<BaseDataSetMessage> messages) {
                byte[] messageBuffer;
                using (var memoryStream = new MemoryStream()) {
                    var compression = UseGzipCompression ?
                        new GZipStream(memoryStream, CompressionLevel.Fastest, true) : null;
                    try {
                        using var encoder = new JsonEncoderEx(
                            UseGzipCompression ? compression : memoryStream, context, JsonEncoderStartingState) {
                            UseAdvancedEncoding = UseAdvancedEncoding,
                            UseUriEncoding = UseAdvancedEncoding,
                            IgnoreDefaultValues = true,
                            IgnoreNullValues = true,
                            UseReversibleEncoding = false
                        };
                        var messagesToInclude = messages.ToArray();
                        if (UseArrayEnvelope) {
                            if (HasSingleDataSetMessage || HasNetworkMessageHeader) {
                                // Legacy compatibility - n network messages with 1 message each inside array
                                for (var i = 0; i < messages.Length; i++) {
                                    var single = messages.Slice(i, 1).ToArray();
                                    if (HasDataSetMessageHeader || HasNetworkMessageHeader) {
                                        encoder.WriteObject(null, Messages, _ => Encode(encoder, single));
                                    }
                                    else {
                                        // Write single messages into the array envelope
                                        Encode(encoder, single);
                                    }
                                }
                            }
                            else {
                                // Write all messages into the array envelope
                                Encode(encoder, messagesToInclude);
                            }
                        }
                        else {
                            Encode(encoder, messagesToInclude);
                        }
                    }
                    finally {
                        compression?.Dispose();
                    }
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
                //
                // Assign a new message id for other messages
                // The first message should have the original
                // message id.
                //
                MessageId = Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Encode with set messages
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="messages"></param>
        private void Encode(JsonEncoderEx encoder, BaseDataSetMessage[] messages) {
            if (HasNetworkMessageHeader) {
                // The encoder was set up as object beforehand based on IsJsonArray result
                encoder.WriteString(nameof(MessageId), MessageId);
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

                if (HasSingleDataSetMessage) {
                    if (HasDataSetMessageHeader) {
                        // Write as a single object under messages property
                        encoder.WriteObject(nameof(Messages), messages[0],
                            v => v.Encode(encoder, true));
                    }
                    else {
                        // Write raw data set object under messages property
                        messages[0].Encode(encoder, false, nameof(Messages));
                    }
                }
                else if (HasDataSetMessageHeader) {
                    // Write as array of objects
                    encoder.WriteArray(nameof(Messages), messages, v =>
                        encoder.WriteObject(null, v, v => v.Encode(encoder, true)));
                }
                else {
                    // Write as array of dataset payload tokens
                    encoder.WriteArray(nameof(Messages), messages,
                        v => v.Encode(encoder, false));
                }
            }
            else {
                // The encoder was set up as array or object beforehand
                if (HasSingleDataSetMessage) {
                    // Write object content to current object
                    messages[0].Encode(encoder, HasDataSetMessageHeader);
                }
                else if (HasDataSetMessageHeader) {
                    // Write each object to the array that is the initial state of the encoder
                    foreach (var message in messages) {
                        // Write as array of dataset messages with payload
                        encoder.WriteObject(null, message, v => v.Encode(encoder, true));
                    }
                }
                else {
                    // Writes dataset directly the encoder was set up as token
                    foreach (var message in messages) {
                        message.Encode(encoder, false);
                    }
                }
            }
        }

        /// <inheritdoc/>
        internal void Decode(JsonDecoderEx decoder) {
            if (HasNetworkMessageHeader) {
                MessageId = decoder.ReadString(nameof(MessageId));

                var messageType = decoder.ReadString(nameof(MessageType));

                if (!messageType.Equals(MessageTypeUaData, StringComparison.InvariantCultureIgnoreCase)) {
                    throw ServiceResultException.Create(StatusCodes.BadTcpMessageTypeInvalid,
                        "Received incorrect message type {0}", messageType);
                }
                if ((NetworkMessageContentMask & (uint)JsonNetworkMessageContentMask.PublisherId) != 0) {
                    PublisherId = decoder.ReadString(nameof(PublisherId));
                }
                if ((NetworkMessageContentMask & (uint)JsonNetworkMessageContentMask.DataSetClassId) != 0) {
                    var dataSetClassId = decoder.ReadString(nameof(DataSetClassId));
                    if (dataSetClassId != null && Guid.TryParse(dataSetClassId, out var result)) {
                        DataSetClassId = result;
                    }
                }
                DataSetWriterGroup = decoder.ReadString(nameof(DataSetWriterGroup));

                //        if (HasSingleDataSetMessage) {
                //            if (HasDataSetMessageHeader) {
                //                // Write as a single object under messages property
                //                decoder.ReadObject(nameof(Messages), messages[0],
                //                    v => v.Encode(encoder, true));
                //            }
                //            else {
                //                // Write raw data set object under messages property
                //                messages[0].Decode(decoder, false, nameof(Messages));
                //            }
                //        }
                //        else if (HasDataSetMessageHeader) {
                //            // Write as array of objects
                //            decoder.ReadArray(nameof(Messages), messages, v =>
                //                decoder.ReadObject(null, v, v => v.Encode(encoder, true)));
                //        }
                //        else {
                //            // Write as array of dataset payload tokens
                //            decoder.ReadArray(nameof(Messages), messages,
                //                v => v.Decode(decoder, false));
                //        }
                //    }
                //    else {
                //        // The encoder was set up as array or object beforehand
                //        if (HasSingleDataSetMessage) {
                //            // Write object content to current object
                //            messages[0].Decode(decoder, HasDataSetMessageHeader);
                //        }
                //        else if (HasDataSetMessageHeader) {
                //            // Write each object to the array that is the initial state of the encoder
                //            foreach (var message in messages) {
                //                // Write as array of dataset messages with payload
                //                decoder.ReadObject(null, message, v => v.Decode(decoder, true));
                //            }
                //        }
                //        else {
                //            // Writes dataset directly the encoder was set up as token
                //            foreach (var message in messages) {
                //                message.Decode(decoder, false);
                //            }
                //        }
                //    }
            }
        }
    }
}