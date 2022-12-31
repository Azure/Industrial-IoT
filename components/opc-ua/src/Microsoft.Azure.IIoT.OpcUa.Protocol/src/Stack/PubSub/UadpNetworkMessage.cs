// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Encodeable Network message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class UadpNetworkMessage : BaseNetworkMessage {

        /// <inheritdoc/>
        public override string MessageSchema => MessageSchemaTypes.NetworkMessageUadp;

        /// <inheritdoc/>
        public override string ContentType => ContentMimeType.Binary;

        /// <inheritdoc/>
        public override string ContentEncoding => null;

        /// <summary>
        /// Writer group id
        /// </summary>
        public ushort WriterGroupId { get; set; }

        /// <summary>
        /// Get and Set VersionTime type: it represents the time in seconds since the year 2000
        /// </summary>
        public uint GroupVersion { get; set; }

        /// <summary>
        /// Get and Set NetworkMessageNumber
        /// </summary>
        public ushort NetworkMessageNumber { get; set; }

        /// <summary>
        /// Get and Set SequenceNumber
        /// </summary>
        public Func<ushort> SequenceNumber { get; set; }

        /// <summary>
        /// Get and Set Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// PicoSeconds
        /// </summary>
        public ushort PicoSeconds { get; set; }

        /// <summary>
        /// Get and Set SecurityFlags
        /// </summary>
        internal SecurityFlagsEncodingMask SecurityFlags { get; set; }

        /// <summary>
        /// Get and Set SecurityTokenId has IntegerId type
        /// </summary>
        public uint SecurityTokenId { get; set; }

        /// <summary>
        /// Get and Set NonceLength
        /// </summary>
        public byte NonceLength { get; set; }

        /// <summary>
        /// Get and Set MessageNonce contains [NonceLength]
        /// </summary>
        public byte[] MessageNonce { get; set; }

        /// <summary>
        /// Get and Set SecurityFooterSize
        /// </summary>
        public ushort SecurityFooterSize { get; set; }

        /// <summary>
        /// Get and Set SecurityFooter
        /// </summary>
        public byte[] SecurityFooter { get; set; }

        /// <summary>
        /// Get and Set Signature
        /// </summary>
        public byte[] Signature { get; set; }

        /// <summary>
        /// The possible values for the NetworkMessage UADPFlags encoding byte.
        /// </summary>
        [Flags]
        internal enum UADPFlagsEncodingMask : byte {
            None = 0,
            VersionMask = 0xF,
            PublisherId = 16,
            GroupHeader = 32,
            PayloadHeader = 64,
            ExtendedFlags1 = 128,
        }

        /// <summary>
        /// The possible types of UADP network messages
        /// </summary>
        [Flags]
        internal enum UADPNetworkMessageType {
            DataSetMessage = 0,
            DiscoveryRequest = 4,
            DiscoveryResponse = 8
        }

        /// <summary>
        /// The possible values for the NetworkMessage ExtendedFlags1 encoding byte.
        /// </summary>
        [Flags]
        internal enum ExtendedFlags1EncodingMask : byte {
            None = 0,
            PublisherIdTypeByte = 0,
            PublisherIdTypeUInt16 = 1,
            PublisherIdTypeUInt32 = 2,
            PublisherIdTypeUInt64 = 3,
            PublisherIdTypeString = 4,
            PublisherIdTypeBits = 0x07,
            DataSetClassId = 8,
            Security = 16,
            Timestamp = 32,
            PicoSeconds = 64,
            ExtendedFlags2 = 128,
        }

        /// <summary>
        /// The possible values for the NetworkMessage ExtendedFlags2 encoding byte.
        /// </summary>
        [Flags]
        internal enum ExtendedFlags2EncodingMask : byte {
            None = 0,
            ChunkMessage = 1,
            PromotedFields = 2,
            NetworkMessageWithDiscoveryRequest = 4,
            NetworkMessageWithDiscoveryResponse = 8,
            Reserved = 16
        }

        /// <summary>
        /// The possible values for the NetworkMessage GroupFlags encoding byte.
        /// </summary>
        [Flags]
        internal enum GroupFlagsEncodingMask : byte {
            None = 0,
            WriterGroupId = 1,
            GroupVersion = 2,
            NetworkMessageNumber = 4,
            SequenceNumber = 8
        }

        /// <summary>
        /// The possible values for the NetworkMessage SecurityFlags encoding byte.
        /// </summary>
        [Flags]
        internal enum SecurityFlagsEncodingMask : byte {
            None = 0,
            NetworkMessageSigned = 1,
            NetworkMessageEncrypted = 2,
            SecurityFooter = 4,
            ForceKeyReset = 8,
            Reserved = 16
        }

        /// <summary>
        /// Set All flags before encode/decode for a NetworkMessage that contains DataSet messages
        /// </summary>
        internal UADPFlagsEncodingMask UadpFlags {
            get {
                if (_uadpFlags == null) {
                    // Bit range 0-3: Version of the UADP NetworkMessage, always 1.
                    _uadpFlags = (UADPFlagsEncodingMask)1;

                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PublisherId) != 0) {
                        // UADPFlags: Bit 4: PublisherId enabled
                        _uadpFlags |= UADPFlagsEncodingMask.PublisherId;
                    }
                    if ((NetworkMessageContentMask & (uint)(UadpNetworkMessageContentMask.GroupHeader |
                                                      UadpNetworkMessageContentMask.WriterGroupId |
                                                      UadpNetworkMessageContentMask.GroupVersion |
                                                      UadpNetworkMessageContentMask.NetworkMessageNumber |
                                                      UadpNetworkMessageContentMask.SequenceNumber)) != 0) {
                        // UADPFlags: Bit 5: GroupHeader enabled
                        _uadpFlags |= UADPFlagsEncodingMask.GroupHeader;
                    }
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PayloadHeader) != 0) {
                        // UADPFlags: Bit 6: PayloadHeader enabled
                        _uadpFlags |= UADPFlagsEncodingMask.PayloadHeader;
                    }

                    if ((NetworkMessageContentMask & (uint)(
                                                      UadpNetworkMessageContentMask.DataSetClassId |
                                                      UadpNetworkMessageContentMask.Timestamp |
                                                      UadpNetworkMessageContentMask.PicoSeconds |
                                                      UadpNetworkMessageContentMask.PromotedFields)) != 0) {
                        // UADPFlags: Bit 7: Enable ExtendedFlags1
                        _uadpFlags |= UADPFlagsEncodingMask.ExtendedFlags1;
                    }

                    if (!string.IsNullOrEmpty(PublisherId) &&
                        (NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PublisherId) != 0) {
                        // UADPFlags: Bit 7: Enable ExtendedFlags1
                        _uadpFlags |= UADPFlagsEncodingMask.ExtendedFlags1;
                    }
                }
                return _uadpFlags.Value;
            }
            set {
                _uadpFlags = value;

                // UADPFlags: Bit 4: PublisherId enabled
                if ((value & UADPFlagsEncodingMask.PublisherId) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.PublisherId;
                }
                // UADPFlags: Bit 6: PayloadHeader enabled
                if ((value & UADPFlagsEncodingMask.PayloadHeader) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.PayloadHeader;
                }
            }
        }

        /// <summary>
        /// Get and Set GroupFlags
        /// </summary>
        internal GroupFlagsEncodingMask GroupFlags {
            get {
                if (_groupFlags == null) {
                    _groupFlags = 0;
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.WriterGroupId) != 0) {
                        // GroupFlags: Bit 0: WriterGroupId enabled
                        _groupFlags |= GroupFlagsEncodingMask.WriterGroupId;
                    }
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.GroupVersion) != 0) {
                        // GroupFlags: Bit 1: GroupVersion enabled
                        _groupFlags |= GroupFlagsEncodingMask.GroupVersion;
                    }
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.NetworkMessageNumber) != 0) {
                        // GroupFlags: Bit 2: NetworkMessageNumber enabled
                        _groupFlags |= GroupFlagsEncodingMask.NetworkMessageNumber;
                    }
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.SequenceNumber) != 0) {
                        // GroupFlags: Bit 3: SequenceNumber enabled
                        _groupFlags |= GroupFlagsEncodingMask.SequenceNumber;
                    }
                }
                return _groupFlags.Value;
            }
            set {
                _groupFlags = value;

                // GroupFlags: Bit 0: WriterGroupId enabled
                if ((value & GroupFlagsEncodingMask.WriterGroupId) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.WriterGroupId;
                }
                // GroupFlags: Bit 1: GroupVersion enabled
                if ((value & GroupFlagsEncodingMask.GroupVersion) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.GroupVersion;
                }
                // GroupFlags: Bit 2: NetworkMessageNumber enabled
                if ((value & GroupFlagsEncodingMask.NetworkMessageNumber) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.NetworkMessageNumber;
                }
                // GroupFlags: Bit 3: SequenceNumber enabled
                if ((value & GroupFlagsEncodingMask.SequenceNumber) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.SequenceNumber;
                }
            }
        }

        /// <summary>
        /// Get and set ExtendedFlags2
        /// </summary>
        internal ExtendedFlags2EncodingMask ExtendedFlags2 {
            get {
                if (_extendedFlags2 == null) {
                    _extendedFlags2 = 0;
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PromotedFields) != 0) {
                        // ExtendedFlags2: Bit 1: PromotedFields enabled
                        _extendedFlags2 |= ExtendedFlags2EncodingMask.PromotedFields;
                    }

                    // Bit range 2-4: UADP NetworkMessage type
                    // 000 NetworkMessage with DataSetMessage payload for now
                }
                return _extendedFlags2.Value;
            }
            set {
                _extendedFlags2 = value;

                // ExtendedFlags2: Bit 1: PromotedFields enabled
                if ((value & ExtendedFlags2EncodingMask.PromotedFields) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.PromotedFields;
                }

                // Bit range 2-4: UADP NetworkMessage type
                // 000 NetworkMessage with DataSetMessage payload for now
            }
        }

        /// <summary>
        /// Set All flags before encode/decode for a NetworkMessage that contains DataSet messages
        /// </summary>
        internal ExtendedFlags1EncodingMask ExtendedFlags1 {
            get {
                if (_extendedFlags1 == null) {
                    _extendedFlags1 = 0;

                    // ExtendedFlags1: Bit range 0-2: PublisherId Type
                    if (!string.IsNullOrEmpty(PublisherId) &&
                        (NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PublisherId) != 0) {
                        if (byte.TryParse(PublisherId, out _)) {
                            _extendedFlags1 |= ExtendedFlags1EncodingMask.PublisherIdTypeByte;
                        }
                        else if (ushort.TryParse(PublisherId, out _)) {
                            _extendedFlags1 |= ExtendedFlags1EncodingMask.PublisherIdTypeUInt16;
                        }
                        else if (uint.TryParse(PublisherId, out _)) {
                            _extendedFlags1 |= ExtendedFlags1EncodingMask.PublisherIdTypeUInt32;
                        }
                        else if (ulong.TryParse(PublisherId, out _)) {
                            _extendedFlags1 |= ExtendedFlags1EncodingMask.PublisherIdTypeUInt64;
                        }
                        else {
                            _extendedFlags1 |= ExtendedFlags1EncodingMask.PublisherIdTypeString;
                        }
                    }
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.DataSetClassId) != 0) {
                        // ExtendedFlags1 Bit 3: DataSetClassId enabled
                        _extendedFlags1 |= ExtendedFlags1EncodingMask.DataSetClassId;
                    }
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.Timestamp) != 0) {
                        // ExtendedFlags1: Bit 5: Timestamp enabled
                        _extendedFlags1 |= ExtendedFlags1EncodingMask.Timestamp;
                    }
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PicoSeconds) != 0) {
                        // ExtendedFlags1: Bit 6: PicoSeconds enabled
                        _extendedFlags1 |= ExtendedFlags1EncodingMask.PicoSeconds;
                    }
                    if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PromotedFields) != 0) {
                        // ExtendedFlags1: Bit 7: ExtendedFlags2 enabled
                        _extendedFlags1 |= ExtendedFlags1EncodingMask.ExtendedFlags2;
                        // Bit range 2-4: UADP NetworkMessage type
                        // 000 NetworkMessage with DataSetMessage payload for now
                    }

                    // ExtendedFlags1: Bit 4: Security enabled
                    // Disable security for now
                    _extendedFlags1 &= ~ExtendedFlags1EncodingMask.Security;
                    // The security footer size shall be omitted if bit 2 of the SecurityFlags is false.
                    SecurityFlags &= ~SecurityFlagsEncodingMask.SecurityFooter;
                }
                return _extendedFlags1.Value;
            }
            set {
                _extendedFlags1 = value;

                // ExtendedFlags1: Bit range 0-2: PublisherId Type

                // ExtendedFlags1 Bit 3: DataSetClassId enabled
                if ((value & ExtendedFlags1EncodingMask.DataSetClassId) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.DataSetClassId;
                }
                // ExtendedFlags1 Bit 5: Timestamp enabled
                if ((value & ExtendedFlags1EncodingMask.Timestamp) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.Timestamp;
                }
                // ExtendedFlags1 Bit 6: PicoSeconds enabled
                if ((value & ExtendedFlags1EncodingMask.PicoSeconds) != 0) {
                    NetworkMessageContentMask |= (uint)UadpNetworkMessageContentMask.PicoSeconds;
                }
            }
        }

        /// <summary>
        /// Get Uadp version. Should always be 1
        /// </summary>
        internal byte UADPVersion => (byte)(UadpFlags & UADPFlagsEncodingMask.VersionMask);

        /// <summary>
        /// Create message
        /// </summary>
        public UadpNetworkMessage() {
            SequenceNumber = () => _sequenceNumber;
        }

        /// <inheritdoc/>
        public override bool TryDecode(IServiceMessageContext context, IEnumerable<byte[]> reader) {
            foreach (var message in reader) {
                using (var binaryDecoder = new BinaryDecoder(message, context)) {
                    // 1. decode network message header (PublisherId & DataSetClassId)
                    var messageType = DecodeNetworkMessageHeader(binaryDecoder);

                    // decode network messages according to their type
                    if (messageType == UADPNetworkMessageType.DataSetMessage) {
                        // decode bytes using dataset reader information
                        DecodeSubscribedDataSets(binaryDecoder);
                    }
                    else {
                        // Not a ua-data message
                        return false;
                    }
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<byte[]> Encode(IServiceMessageContext context, int maxChunkSize) {

            var messages = new List<byte[]>();
            bool isChunkMessage = false;
            var remainingChunks = EncodePayloadChunks(context).AsSpan();

            // Write one message even if it does not contain anything (heartbeat)
            do {
                // Re-evaluate flags every go around
                _uadpFlags = null;
                _groupFlags = null;
                _extendedFlags1 = null;
                _extendedFlags2 = null;

                var networkMessageNumber = NetworkMessageNumber++;

                using (var stream = new MemoryStream()) {
                    using (var encoder = new BinaryEncoder(stream, context)) {
                        //
                        // Try to write all unless we are writing chunk messages.
                        // Write span is the span of chunks that should go into
                        // the current message. We start with all remaining chunks
                        // and then limit it when trying to write payload.
                        //
                        var writeSpan = remainingChunks;

                        //
                        // There is a maximum of 256 messages per network message
                        // due to the payload header count field being just a byte.
                        //
                        if (writeSpan.Length > byte.MaxValue) {
                            writeSpan = writeSpan.Slice(0, byte.MaxValue);
                        }
#if DEBUG
                        var remaining = remainingChunks.Length;
#endif
                        while (true) {
                            WriteNetworkMessageHeader(encoder, isChunkMessage);
                            WriteGroupMessageHeader(encoder, networkMessageNumber);

                            WritePayloadHeader(encoder, writeSpan, isChunkMessage);
                            WriteExtendedNetworkMessageHeader(encoder);
                            WriteSecurityHeader(encoder);

                            if (!TryWritePayload(encoder, maxChunkSize, ref writeSpan,
                                ref remainingChunks, ref isChunkMessage)) {
                                encoder.Position = 0; // Restart writing
                                continue;
                            }

                            WriteSecurityFooter(encoder);
                            WriteSignature(encoder);
#if DEBUG
                            //
                            // Now remaining chunks should be equal (in case we are
                            // writing a chunk message) or less than when we started.
                            //
                            Debug.Assert(
                                (isChunkMessage && remaining == remainingChunks.Length) ||
                                (!isChunkMessage && remaining > remainingChunks.Length));
#endif
                            break;
                        }
                        stream.SetLength(encoder.Position);
                    }
                    messages.Add(stream.ToArray());
                }
            }
            while (remainingChunks.Length > 0);

            return messages;

            Message[] EncodePayloadChunks(IServiceMessageContext context) {
                var chunks = new Message[Messages.Count];
                for (var i = 0; i < Messages.Count; i++) {
                    var message = Messages[i];
                    using (var stream = new MemoryStream()) {
                        using (var encoder = new BinaryEncoder(stream, context)) {
                            message.Encode(encoder, true, null);
                        }
                        chunks[i] = new Message(stream.ToArray(), message.DataSetWriterId);
                    }
                }
                return chunks;
            }
        }

        /// <summary>
        /// Write Network Message Header
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="isChunkMessage"></param>
        protected void WriteNetworkMessageHeader(BinaryEncoder encoder, bool isChunkMessage) {

            if (isChunkMessage) {
                UadpFlags |= UADPFlagsEncodingMask.ExtendedFlags1;
                ExtendedFlags1 |= ExtendedFlags1EncodingMask.ExtendedFlags2;
                ExtendedFlags2 |= ExtendedFlags2EncodingMask.ChunkMessage;
            }

            encoder.WriteByte("UadpFlags", (byte)UadpFlags);
            if ((UadpFlags & UADPFlagsEncodingMask.ExtendedFlags1) != 0) {
                encoder.WriteByte("ExtendedFlags1", (byte)ExtendedFlags1);
            }
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.ExtendedFlags2) != 0) {
                encoder.WriteByte("ExtendedFlags2", (byte)ExtendedFlags2);
            }
            if ((UadpFlags & UADPFlagsEncodingMask.PublisherId) != 0) {
                if (PublisherId == null) {
                    throw ServiceResultException.Create(StatusCodes.BadEncodingError,
        "NetworkMessageHeader cannot be encoded. PublisherId is null but it is expected to be encoded.");
                }
                else {
                    switch (ExtendedFlags1 & ExtendedFlags1EncodingMask.PublisherIdTypeBits) {
                        case ExtendedFlags1EncodingMask.PublisherIdTypeByte:
                            encoder.WriteByte("PublisherId", byte.Parse(PublisherId));
                            break;
                        case ExtendedFlags1EncodingMask.PublisherIdTypeUInt16:
                            encoder.WriteUInt16("PublisherId", ushort.Parse(PublisherId));
                            break;
                        case ExtendedFlags1EncodingMask.PublisherIdTypeUInt32:
                            encoder.WriteUInt32("PublisherId", uint.Parse(PublisherId));
                            break;
                        case ExtendedFlags1EncodingMask.PublisherIdTypeUInt64:
                            encoder.WriteUInt64("PublisherId", ulong.Parse(PublisherId));
                            break;
                        case ExtendedFlags1EncodingMask.PublisherIdTypeString:
                            encoder.WriteString("PublisherId", PublisherId);
                            break;
                        default:
                            // Reserved - no type provided
                            break;
                    }
                }
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.DataSetClassId) != 0) {
                encoder.WriteGuid("DataSetClassId", DataSetClassId);
            }
        }

        /// <summary>
        /// Write Group Message Header
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="networkMessageNumber"></param>
        private void WriteGroupMessageHeader(BinaryEncoder encoder, ushort networkMessageNumber) {
            if ((NetworkMessageContentMask & (uint)
                    (UadpNetworkMessageContentMask.GroupHeader |
                     UadpNetworkMessageContentMask.WriterGroupId |
                     UadpNetworkMessageContentMask.GroupVersion |
                     UadpNetworkMessageContentMask.NetworkMessageNumber |
                     UadpNetworkMessageContentMask.SequenceNumber)) != 0) {
                encoder.WriteByte("GroupFlags", (byte)GroupFlags);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.WriterGroupId) != 0) {
                encoder.WriteUInt16("WriterGroupId", WriterGroupId);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.GroupVersion) != 0) {
                encoder.WriteUInt32("GroupVersion", GroupVersion);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.NetworkMessageNumber) != 0) {
                encoder.WriteUInt16("NetworkMessageNumber", networkMessageNumber);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.SequenceNumber) != 0) {
                encoder.WriteUInt16("SequenceNumber", SequenceNumber());
            }
        }

        /// <summary>
        /// Write Payload Header
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="messages"></param>
        /// <param name="isChunkMessage"></param>
        private void WritePayloadHeader(BinaryEncoder encoder, ReadOnlySpan<Message> messages,
            bool isChunkMessage) {
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PayloadHeader) != 0) {
                if (isChunkMessage) {
                    Debug.Assert(messages.Length == 1);
                    // https://reference.opcfoundation.org/Core/Part14/v104/docs/7.2.2.2.4

                    // Write chunked NetworkMessage Payload Header (Table 77)
                    encoder.WriteUInt16("DataSetWriterId", messages[0].DataSetWriterId);
                }
                else {
                    // Write data set message payload header
                    encoder.WriteByte("Count", (byte)messages.Length);
                    // Collect DataSetSetMessages headers
                    foreach (var message in messages) {
                        encoder.WriteUInt16("DataSetWriterId", message.DataSetWriterId);
                    }
                }
            }
        }

        /// <summary>
        /// Write extended network message header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteExtendedNetworkMessageHeader(BinaryEncoder encoder) {
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.Timestamp) != 0) {
                encoder.WriteDateTime("Timestamp", Timestamp);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PicoSeconds) != 0) {
                encoder.WriteUInt16("PicoSeconds", PicoSeconds);
            }
        }

        /// <summary>
        /// Write security header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteSecurityHeader(BinaryEncoder encoder) {
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.Security) != 0) {
                encoder.WriteByte("SecurityFlags", (byte)SecurityFlags);

                encoder.WriteUInt32("SecurityTokenId", SecurityTokenId);
                encoder.WriteByte("NonceLength", NonceLength);
                MessageNonce = new byte[NonceLength];
                encoder.WriteByteArray("MessageNonce", MessageNonce);

                if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                    encoder.WriteUInt16("SecurityFooterSize", SecurityFooterSize);
                }
            }
        }

        /// <summary>
        /// Write payload. The approach is to try and write everything into a single message
        /// first. If we get here and find we could not do it we split the messages into
        /// either a smaller set (writeSpan) or into a single message we write as chunked
        /// messages. If we return false we restart the entire encoding process.
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="maxMessageSize"></param>
        /// <param name="writeSpan">The sequence to write to the current message</param>
        /// <param name="remainingChunks">The remaining chunks to write after this
        /// message returns true</param>
        /// <param name="isChunkMessage">Sets chunk mode on or off</param>
        /// <returns></returns>
        protected bool TryWritePayload(BinaryEncoder encoder, int maxMessageSize,
            ref Span<Message> writeSpan, ref Span<Message> remainingChunks, ref bool isChunkMessage) {

            // MessageSequenceNumber + ChunkOffset + TotalOffset + ByteString Length field
            const int kChunkHeaderSize = 12;

            int payloadOffset = encoder.Position;
            var available = maxMessageSize - payloadOffset
                    - SecurityFooterSize - (Signature?.Length ?? 0) - kChunkHeaderSize;
            if (available < 0) {

                if (writeSpan.Length <= 1) {
                    // Nothing fits. We should not be here - fail catastrophically...
                    throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                        "Max message size too small for header for single message");
                }

                // Try to limit the number of messages by half
                writeSpan = writeSpan.Slice(0, writeSpan.Length / 2);
                return false;
            }

            if (writeSpan.Length == 0) {
                // Nothing to do
                return true;
            }

            var hasHeader = (NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PayloadHeader) != 0;
            var headerSize = hasHeader ? 2 : 0;
            var chunk = writeSpan[0];
            //
            // We break any message into chunks that a) does not fit in available space,
            // or b) exceeds the ushort size fields. We count the header size in here to
            // avoid dropping into the else and failing to fit the first chunk.
            //
            if (isChunkMessage ||
                (chunk.Remaining + headerSize) > available ||
                chunk.Remaining > ushort.MaxValue) {
                if (!isChunkMessage) {
                    isChunkMessage = true;
                    writeSpan = writeSpan.Slice(0, 1);
                    // Restart writing the complete buffer now in chunk message mode
                    return false;
                }

                available = Math.Min(available, ushort.MaxValue);

                // Write Chunked NetworkMessage Payload Fields (Table 78)
                // https://reference.opcfoundation.org/Core/Part14/v104/docs/7.2.2.2.4
                encoder.WriteUInt16("MessageSequenceNumber", chunk.MessageSequenceNumber);
                encoder.WriteUInt32("ChunkOffset", chunk.ChunkOffset);
                encoder.WriteUInt32("TotalSize", chunk.TotalSize);

                var chunkLength = Math.Min(chunk.Remaining, available);
                encoder.WriteInt32("ChunkLength", chunkLength); // Write byte string
                encoder.WriteRawBytes(chunk.ChunkData, (int)chunk.ChunkOffset, chunkLength);
                chunk.ChunkOffset += (uint)chunkLength;

                if (chunk.Remaining == 0) {
                    // Completed
                    writeSpan = remainingChunks = remainingChunks.Slice(1);
                    isChunkMessage = false;
                }
                else {
                    // Write more into next message
                    chunk.MessageSequenceNumber++;
                }
            }
            else {
                var writeCount = 0;
                for (; writeCount < writeSpan.Length; writeCount++) {
                    var currentChunk = writeSpan[writeCount];
                    if ((currentChunk.Remaining + headerSize) > available) {
                        break;
                    }
                    available -= (currentChunk.Remaining + headerSize);
                }

                if (writeSpan.Length != writeCount) {
                    // Restart by limiting the number of chunks we will write to this message to what fits
                    writeSpan = remainingChunks.Slice(0, writeCount);
                    return false;
                }

                // https://reference.opcfoundation.org/Core/Part14/v104/docs/7.2.2.3.3
                if (hasHeader) {
                    foreach (var buffer in writeSpan) {
                        encoder.WriteUInt16("Size", (ushort)buffer.TotalSize);
                    }
                }
                foreach (var buffer in writeSpan) {
                    encoder.WriteRawBytes(buffer.ChunkData, 0, (int)buffer.TotalSize);
                }
                remainingChunks = remainingChunks.Slice(writeCount);
                writeSpan = default;
            }
            // Write security header after and finish message
            return true;
        }

        /// <summary>
        /// Write security footer
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteSecurityFooter(BinaryEncoder encoder) {
            if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                encoder.WriteByteArray("SecurityFooter", SecurityFooter);
            }
        }

        /// <summary>
        /// Write signature
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteSignature(BinaryEncoder encoder) {
            if (Signature != null && Signature.Length > 0) {
                encoder.WriteByteArray("Signature", Signature);
            }
        }


        // TODO: Need to implement decoder correctly

        /// <summary>
        /// Decode the stream from decoder parameter and produce a Dataset
        /// </summary>
        /// <param name="binaryDecoder"></param>
        /// <returns></returns>
        public void DecodeSubscribedDataSets(BinaryDecoder binaryDecoder) {
            try {

                DecodeGroupMessageHeader(binaryDecoder);
                DecodePayloadHeader(binaryDecoder);
                DecodeExtendedNetworkMessageHeader(binaryDecoder);
                DecodeSecurityHeader(binaryDecoder);
                DecodePayloadSize(binaryDecoder);

                // the list of decode dataset messages for this network message
                var dataSetMessages = new List<BaseDataSetMessage>();

                //  /* 6.2.8.3 DataSetWriterId
                //  The parameter DataSetWriterId with DataType UInt16 defines the DataSet selected in the Publisher for the DataSetReader.
                //  If the value is 0 (null), the parameter shall be ignored and all received DataSetMessages pass the DataSetWriterId filter.*/
                //  foreach (var dataSetReader in dataSetReaders) {
                //      var uadpDataSetMessages = new List<BaseDataSetMessage>(Messages);
                //      //if there is no information regarding dataSet in network message, add dummy datasetMessage to try decoding
                //      if (uadpDataSetMessages.Count == 0) {
                //          uadpDataSetMessages.Add(new UadpDataSetMessage());
                //      }
                //
                //      // 6.2 Decode payload into DataSets
                //      // Restore the encoded fields (into dataset for now) for each possible dataset reader
                //      foreach (UadpDataSetMessage uadpDataSetMessage in uadpDataSetMessages) {
                //          if (uadpDataSetMessage.Payload.Count > 0) {
                //              continue; // this dataset message was already decoded
                //          }
                //
                //          if (dataSetReader.DataSetWriterId == 0 || uadpDataSetMessage.DataSetWriterId == dataSetReader.DataSetWriterId) {
                //              //atempt to decode dataset message using the reader
                //              uadpDataSetMessage.DecodePossibleDataSetReader(binaryDecoder, dataSetReader);
                //              if (uadpDataSetMessage.Payload.Count > 0) {
                //                  dataSetMessages.Add(uadpDataSetMessage);
                //              }
                //              else if (uadpDataSetMessage.IsMetadataMajorVersionChange) {
                //                  OnDataSetDecodeErrorOccurred(new DataSetDecodeErrorEventArgs(DataSetDecodeErrorReason.MetadataMajorVersion, this, dataSetReader));
                //              }
                //          }
                //      }
                //  }

                if (Messages.Count == 0) {
                    // set the list of dataset messages to the network message
                    Messages.AddRange(dataSetMessages);
                }
                else {
                    dataSetMessages = new List<BaseDataSetMessage>();
                    // check if DataSets are decoded into the existing dataSetMessages
                    foreach (var dataSetMessage in Messages) {
                        if (dataSetMessage.Payload.Count == 0) {
                            dataSetMessages.Add(dataSetMessage);
                        }
                    }
                    Messages.Clear();
                    Messages.AddRange(dataSetMessages);
                }

            }
            catch (Exception ex) {
                // Unexpected exception in DecodeSubscribedDataSets
                Utils.Trace(ex, "UadpNetworkMessage.DecodeSubscribedDataSets");
            }
        }

        /// <summary>
        /// Encode Network Message Header
        /// </summary>
        /// <param name="decoder"></param>
        private UADPNetworkMessageType DecodeNetworkMessageHeader(BinaryDecoder decoder) {
            UadpFlags = (UADPFlagsEncodingMask)decoder.ReadByte("UadpFlags");

            // Decode the ExtendedFlags1
            if ((UadpFlags & UADPFlagsEncodingMask.ExtendedFlags1) != 0) {
                ExtendedFlags1 = (ExtendedFlags1EncodingMask)decoder.ReadByte("ExtendedFlags1");
            }

            // Decode the ExtendedFlags2
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.ExtendedFlags2) != 0) {
                ExtendedFlags2 = (ExtendedFlags2EncodingMask)decoder.ReadByte("ExtendedFlags2");
            }

            // calculate UADPNetworkMessageType
            UADPNetworkMessageType messageType = UADPNetworkMessageType.DataSetMessage;
            if ((ExtendedFlags2 & ExtendedFlags2EncodingMask.NetworkMessageWithDiscoveryRequest) != 0) {
                messageType = UADPNetworkMessageType.DiscoveryRequest;
            }
            else if ((ExtendedFlags2 & ExtendedFlags2EncodingMask.NetworkMessageWithDiscoveryResponse) != 0) {
                messageType = UADPNetworkMessageType.DiscoveryResponse;
            }

            // Decode PublisherId
            if ((UadpFlags & UADPFlagsEncodingMask.PublisherId) != 0) {
                switch (ExtendedFlags1 & ExtendedFlags1EncodingMask.PublisherIdTypeBits) {
                    case ExtendedFlags1EncodingMask.PublisherIdTypeUInt16:
                        PublisherId = decoder.ReadUInt16("PublisherId").ToString();
                        break;
                    case ExtendedFlags1EncodingMask.PublisherIdTypeUInt32:
                        PublisherId = decoder.ReadUInt32("PublisherId").ToString();
                        break;
                    case ExtendedFlags1EncodingMask.PublisherIdTypeUInt64:
                        PublisherId = decoder.ReadUInt64("PublisherId").ToString();
                        break;
                    case ExtendedFlags1EncodingMask.PublisherIdTypeString:
                        PublisherId = decoder.ReadString("PublisherId");
                        break;
                    case ExtendedFlags1EncodingMask.PublisherIdTypeByte:
                        PublisherId = decoder.ReadByte("PublisherId").ToString();
                        break;
                }
            }

            // Decode DataSetClassId
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.DataSetClassId) != 0) {
                DataSetClassId = decoder.ReadGuid("DataSetClassId");
            }
            return messageType;
        }

        /// <summary>
        /// Decode Group Message Header
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodeGroupMessageHeader(BinaryDecoder decoder) {
            // Decode GroupHeader (that holds GroupFlags)
            if ((UadpFlags & UADPFlagsEncodingMask.GroupHeader) != 0) {
                GroupFlags = (GroupFlagsEncodingMask)decoder.ReadByte("GroupFlags");
            }
            // Decode WriterGroupId
            if ((GroupFlags & GroupFlagsEncodingMask.WriterGroupId) != 0) {
                WriterGroupId = decoder.ReadUInt16("WriterGroupId");
            }
            // Decode GroupVersion
            if ((GroupFlags & GroupFlagsEncodingMask.GroupVersion) != 0) {
                GroupVersion = decoder.ReadUInt32("GroupVersion");
            }
            // Decode NetworkMessageNumber
            if ((GroupFlags & GroupFlagsEncodingMask.NetworkMessageNumber) != 0) {
                NetworkMessageNumber = decoder.ReadUInt16("NetworkMessageNumber");
            }
            // Decode SequenceNumber
            if ((GroupFlags & GroupFlagsEncodingMask.SequenceNumber) != 0) {
                _sequenceNumber = decoder.ReadUInt16("SequenceNumber");
            }
        }

        /// <summary>
        /// Decode Payload Header
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodePayloadHeader(BinaryDecoder decoder) {
            // Decode PayloadHeader
            if ((UadpFlags & UADPFlagsEncodingMask.PayloadHeader) != 0) {
                var count = decoder.ReadByte("Count");
                for (var idx = 0; idx < count; idx++) {
                    Messages.Add(new UadpDataSetMessage());
                }
                // collect DataSetSetMessages headers
                foreach (UadpDataSetMessage uadpDataSetMessage in Messages) {
                    uadpDataSetMessage.DataSetWriterId = decoder.ReadUInt16("DataSetWriterId");
                }
            }
        }

        /// <summary>
        /// Decode extended network message header
        /// </summary>
        private void DecodeExtendedNetworkMessageHeader(BinaryDecoder decoder) {
            // Decode Timestamp
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.Timestamp) != 0) {
                Timestamp = decoder.ReadDateTime("Timestamp");
            }
            // Decode PicoSeconds
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.PicoSeconds) != 0) {
                PicoSeconds = decoder.ReadUInt16("PicoSeconds");
            }
        }

        /// <summary>
        /// Decode  payload size and prepare for decoding payload
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodePayloadSize(BinaryDecoder decoder) {
            if (Messages.Count > 1) {
                // Decode PayloadHeader Size
                if ((UadpFlags & UADPFlagsEncodingMask.PayloadHeader) != 0) {
                    foreach (UadpDataSetMessage uadpDataSetMessage in Messages) {
                        // Save the size
                        uadpDataSetMessage.PayloadSizeInStream = decoder.ReadUInt16("Size");
                    }
                }
            }
            var offset = 0;
            // set start position of dataset message in binary stream
            foreach (UadpDataSetMessage uadpDataSetMessage in Messages) {
                uadpDataSetMessage.StartPositionInStream = decoder.Position + offset;
                offset += uadpDataSetMessage.PayloadSizeInStream;
            }
        }

        /// <summary>
        /// Decode security header
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodeSecurityHeader(BinaryDecoder decoder) {
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.Security) != 0) {
                SecurityFlags = (SecurityFlagsEncodingMask)decoder.ReadByte("SecurityFlags");

                SecurityTokenId = decoder.ReadUInt32("SecurityTokenId");
                NonceLength = decoder.ReadByte("NonceLength");
                MessageNonce = decoder.ReadByteArray("MessageNonce").ToArray();

                if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                    SecurityFooterSize = decoder.ReadUInt16("SecurityFooterSize");
                }
            }
        }

        /// <summary>
        /// Decode security footer
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodeSecurityFooter(BinaryDecoder decoder) {
            if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                SecurityFooter = decoder.ReadByteArray("SecurityFooter").ToArray();
            }
        }

        /// <summary>
        /// Decode signature
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodeSignature(BinaryDecoder decoder) {
            Signature = decoder.ReadByteArray("Signature").ToArray();
        }

        /// <summary>
        /// Track messages
        /// </summary>
        protected sealed class Message {

            /// <summary>
            /// Data set writer of the dataset message
            /// </summary>
            public readonly ushort DataSetWriterId;

            /// <summary>
            /// Current message sequence number
            /// </summary>
            public ushort MessageSequenceNumber;

            /// <summary>
            /// Chunk offset
            /// </summary>
            public uint ChunkOffset;

            /// <summary>
            /// Total size
            /// </summary>
            public uint TotalSize => (uint)ChunkData.Length;

            /// <summary>
            /// Chunk length
            /// </summary>
            public int Remaining => ChunkData.Length - (int)ChunkOffset;

            /// <summary>
            /// Full message buffer
            /// </summary>
            public readonly byte[] ChunkData;

            /// <summary>
            /// Create chunk
            /// </summary>
            /// <param name="buffer"></param>
            /// <param name="dataSetWriterId"></param>
            public Message(byte[] buffer, ushort dataSetWriterId) {
                ChunkData = buffer;
                ChunkOffset = 0;
                DataSetWriterId = dataSetWriterId;
                MessageSequenceNumber = 1;
            }
        }

        private UADPFlagsEncodingMask? _uadpFlags;
        private GroupFlagsEncodingMask? _groupFlags;
        private ExtendedFlags2EncodingMask? _extendedFlags2;
        private ExtendedFlags1EncodingMask? _extendedFlags1;
        private ushort _sequenceNumber;
    }
}
