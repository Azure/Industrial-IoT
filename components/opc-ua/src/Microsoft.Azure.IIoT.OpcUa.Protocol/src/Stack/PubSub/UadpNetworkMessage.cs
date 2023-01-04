// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Microsoft.IO;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

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
            DiscoveryProbe = 4,
            DiscoveryAnnouncement = 8,
            DiscoveryTypeBits = 0x1c
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
        public override bool TryDecode(IServiceMessageContext context, Queue<byte[]> reader,
            IDataSetMetaDataResolver resolver) {
            var chunks = new List<Message>();
            while (reader.TryPeek(out var buffer)) {
                using (var binaryDecoder = new BinaryDecoder(buffer, context)) {
                    ReadNetworkMessageHeaderFlags(binaryDecoder);

                    // decode network message header according to the header flags
                    if (!TryReadNetworkMessageHeader(binaryDecoder, chunks.Count == 0)) {
                        return false;
                    }

                    var buffers = ReadPayload(binaryDecoder, chunks).ToArray();

                    ReadSecurityFooter(binaryDecoder);
                    ReadSignature(binaryDecoder);

                    // Processing completed
                    reader.Dequeue();
                    if (buffers.Length != 0 || chunks.Count == 0) {
                        if (buffers.Length > 0) {
                            DecodePayloadChunks(context, buffers, resolver);
                            // Process all messages in the buffer
                        }
                        break;
                    }
                    // Still not processed all chunks, continue reading
                    continue;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<byte[]> Encode(IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver resolver) {
            var messages = new List<byte[]>();
            bool isChunkMessage = false;
            var remainingChunks = EncodePayloadChunks(context, resolver).AsSpan();

            // Write one message even if it does not contain anything (heartbeat)
            do {
                // Re-evaluate flags every go around
                _uadpFlags = null;
                _groupFlags = null;
                _extendedFlags1 = null;
                _extendedFlags2 = null;

                var networkMessageNumber = NetworkMessageNumber++;

                using (var stream = Memory.GetStream()) {
                    using (var encoder = new BinaryEncoder(stream, context, leaveOpen: true)) {
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
                            WriteNetworkMessageHeaderFlags(encoder, isChunkMessage);

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

                        // TODO: instead of copy using ToArray we shall include the
                        // stream with the message and dispose it later when it is
                        // consumed. To get here the bug in BinaryEncoder that it
                        // disposes the underlying stream even if leaveOpen: true
                        // is set must be fixed.
                        messages.Add(stream.ToArray());
                    }
                }
            }
            while (remainingChunks.Length > 0);

            Debug.Assert(!isChunkMessage);
            return messages;
        }

        /// <summary>
        /// Try read network message
        /// </summary>
        /// <param name="binaryDecoder"></param>
        /// <param name="isFirstChunk"></param>
        /// <returns></returns>
        protected virtual bool TryReadNetworkMessageHeader(BinaryDecoder binaryDecoder,
            bool isFirstChunk) {
            if ((ExtendedFlags2 & ExtendedFlags2EncodingMask.DiscoveryTypeBits) != 0) {
                return false;
            }
            if (!TryReadGroupMessageHeader(binaryDecoder) ||
                !TryReadPayloadHeader(binaryDecoder, isFirstChunk) ||
                !TryReadExtendedNetworkMessageHeader(binaryDecoder) ||
                !TryReadSecurityHeader(binaryDecoder)) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Decode payload buffers
        /// </summary>
        /// <param name="context"></param>
        /// <param name="buffers"></param>
        /// <param name="resolver"></param>
        protected virtual void DecodePayloadChunks(IServiceMessageContext context,
            IReadOnlyList<byte[]> buffers, IDataSetMetaDataResolver resolver) {
            var payloadLength = buffers.Sum(b => b.Length);
            using (var stream = new RecyclableMemoryStream(Memory, Guid.NewGuid(),
                PublisherId + _sequenceNumber, payloadLength)) {
                foreach (var buffer in buffers) {
                    stream.Write(buffer);
                }
                stream.Position = 0;
                using (var decoder = new BinaryDecoder(stream, context)) {
                    foreach (UadpDataSetMessage message in Messages) {
                        if (!message.TryDecode(decoder, resolver)) {
                            return;
                        }
                    }
                    //
                    // Read remaining messages from buffer if possible.
                    // This is the case if the payload header was missing
                    // and we must use the data set message decoder to
                    // sort out the offset and lengths of the encoding.
                    //
                    while (decoder.Position < payloadLength) {
                        var extra = new UadpDataSetMessage();
                        if (!extra.TryDecode(decoder, resolver)) {
                            break;
                        }
                        Messages.Add(extra);
                    }
                }
            }
        }

        /// <summary>
        /// Encode payload buffers
        /// </summary>
        /// <param name="context"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        protected virtual Message[] EncodePayloadChunks(IServiceMessageContext context,
            IDataSetMetaDataResolver resolver) {
            var chunks = new Message[Messages.Count];
            for (var i = 0; i < Messages.Count; i++) {
                var message = (UadpDataSetMessage)Messages[i];
                using (var stream = Memory.GetStream()) {
                    using (var encoder = new BinaryEncoder(stream, context, leaveOpen: true)) {
                        message.Encode(encoder, resolver);

                        // TODO: instead of copy using ToArray we shall include the
                        // stream with the message and dispose it later when it is
                        // consumed. To get here the bug in BinaryEncoder that it
                        // disposes the underlying stream even if leaveOpen: true
                        // is set must be fixed.
                        chunks[i] = new Message(stream.ToArray(), message.DataSetWriterId);
                    }
                }
            }
            return chunks;
        }

        /// <summary>
        /// Read Network Message Header
        /// </summary>
        /// <param name="decoder"></param>
        private void ReadNetworkMessageHeaderFlags(BinaryDecoder decoder) {
            UadpFlags = (UADPFlagsEncodingMask)decoder.ReadByte(null);

            // Decode the ExtendedFlags1
            if ((UadpFlags & UADPFlagsEncodingMask.ExtendedFlags1) != 0) {
                ExtendedFlags1 = (ExtendedFlags1EncodingMask)decoder.ReadByte(null);
            }

            // Decode the ExtendedFlags2
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.ExtendedFlags2) != 0) {
                ExtendedFlags2 = (ExtendedFlags2EncodingMask)decoder.ReadByte(null);
            }

            // Decode PublisherId
            if ((UadpFlags & UADPFlagsEncodingMask.PublisherId) != 0) {
                switch (ExtendedFlags1 & ExtendedFlags1EncodingMask.PublisherIdTypeBits) {
                    case ExtendedFlags1EncodingMask.PublisherIdTypeUInt16:
                        PublisherId = decoder.ReadUInt16(null).ToString();
                        break;
                    case ExtendedFlags1EncodingMask.PublisherIdTypeUInt32:
                        PublisherId = decoder.ReadUInt32(null).ToString();
                        break;
                    case ExtendedFlags1EncodingMask.PublisherIdTypeUInt64:
                        PublisherId = decoder.ReadUInt64(null).ToString();
                        break;
                    case ExtendedFlags1EncodingMask.PublisherIdTypeString:
                        PublisherId = decoder.ReadString(null);
                        break;
                    case ExtendedFlags1EncodingMask.PublisherIdTypeByte:
                        PublisherId = decoder.ReadByte(null).ToString();
                        break;
                }
            }

            // Decode DataSetClassId
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.DataSetClassId) != 0) {
                DataSetClassId = decoder.ReadGuid(null);
            }
        }

        /// <summary>
        /// Write Network Message Header
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="isChunkMessage"></param>
        protected void WriteNetworkMessageHeaderFlags(BinaryEncoder encoder, bool isChunkMessage) {

            if (isChunkMessage) {
                UadpFlags |= UADPFlagsEncodingMask.ExtendedFlags1;
                ExtendedFlags1 |= ExtendedFlags1EncodingMask.ExtendedFlags2;
                ExtendedFlags2 |= ExtendedFlags2EncodingMask.ChunkMessage;
            }

            encoder.WriteByte(null, (byte)UadpFlags);
            if ((UadpFlags & UADPFlagsEncodingMask.ExtendedFlags1) != 0) {
                encoder.WriteByte(null, (byte)ExtendedFlags1);
            }
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.ExtendedFlags2) != 0) {
                encoder.WriteByte(null, (byte)ExtendedFlags2);
            }
            if ((UadpFlags & UADPFlagsEncodingMask.PublisherId) != 0) {
                if (PublisherId == null) {
                    throw ServiceResultException.Create(StatusCodes.BadEncodingError,
        "NetworkMessageHeader cannot be encoded. PublisherId is null but it is expected to be encoded.");
                }
                else {
                    switch (ExtendedFlags1 & ExtendedFlags1EncodingMask.PublisherIdTypeBits) {
                        case ExtendedFlags1EncodingMask.PublisherIdTypeByte:
                            encoder.WriteByte(null, byte.Parse(PublisherId));
                            break;
                        case ExtendedFlags1EncodingMask.PublisherIdTypeUInt16:
                            encoder.WriteUInt16(null, ushort.Parse(PublisherId));
                            break;
                        case ExtendedFlags1EncodingMask.PublisherIdTypeUInt32:
                            encoder.WriteUInt32(null, uint.Parse(PublisherId));
                            break;
                        case ExtendedFlags1EncodingMask.PublisherIdTypeUInt64:
                            encoder.WriteUInt64(null, ulong.Parse(PublisherId));
                            break;
                        case ExtendedFlags1EncodingMask.PublisherIdTypeString:
                            encoder.WriteString(null, PublisherId);
                            break;
                        default:
                            // Reserved - no type provided
                            break;
                    }
                }
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.DataSetClassId) != 0) {
                encoder.WriteGuid(null, DataSetClassId);
            }
        }

        /// <summary>
        /// Read Group Message Header
        /// </summary>
        /// <param name="decoder"></param>
        private bool TryReadGroupMessageHeader(BinaryDecoder decoder) {
            // Decode GroupHeader (that holds GroupFlags)
            if ((UadpFlags & UADPFlagsEncodingMask.GroupHeader) != 0) {
                GroupFlags = (GroupFlagsEncodingMask)decoder.ReadByte(null);
            }
            // Decode WriterGroupId
            if ((GroupFlags & GroupFlagsEncodingMask.WriterGroupId) != 0) {
                WriterGroupId = decoder.ReadUInt16(null);
            }
            // Decode GroupVersion
            if ((GroupFlags & GroupFlagsEncodingMask.GroupVersion) != 0) {
                GroupVersion = decoder.ReadUInt32(null);
            }
            // Decode NetworkMessageNumber
            if ((GroupFlags & GroupFlagsEncodingMask.NetworkMessageNumber) != 0) {
                NetworkMessageNumber = decoder.ReadUInt16(null);
            }
            // Decode SequenceNumber
            if ((GroupFlags & GroupFlagsEncodingMask.SequenceNumber) != 0) {
                _sequenceNumber = decoder.ReadUInt16(null);
            }
            return true;
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
                encoder.WriteByte(null, (byte)GroupFlags);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.WriterGroupId) != 0) {
                encoder.WriteUInt16(null, WriterGroupId);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.GroupVersion) != 0) {
                encoder.WriteUInt32(null, GroupVersion);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.NetworkMessageNumber) != 0) {
                encoder.WriteUInt16(null, networkMessageNumber);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.SequenceNumber) != 0) {
                encoder.WriteUInt16(null, SequenceNumber());
            }
        }

        /// <summary>
        /// Read Payload Header
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="isFirstChunk"></param>
        private bool TryReadPayloadHeader(BinaryDecoder decoder, bool isFirstChunk) {
            // Decode PayloadHeader
            if ((UadpFlags & UADPFlagsEncodingMask.PayloadHeader) != 0) {
                if ((ExtendedFlags2 & ExtendedFlags2EncodingMask.ChunkMessage) != 0) {
                    var dataSetWriterId = decoder.ReadUInt16(null);
                    // https://reference.opcfoundation.org/Core/Part14/v104/docs/7.2.2.2.4

                    if (isFirstChunk) {
                        Messages.Add(new UadpDataSetMessage {
                            DataSetWriterId = dataSetWriterId
                        });
                    }
                }
                else {
                    var count = decoder.ReadByte(null);
                    for (var i = 0; i < count; i++) {
                        Messages.Add(new UadpDataSetMessage {
                            DataSetWriterId = decoder.ReadUInt16(null)
                        });
                    }
                }
            }
            return true;
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
                // Write data set message payload header
                if (isChunkMessage) {
                    Debug.Assert(messages.Length >= 1);
                    // https://reference.opcfoundation.org/Core/Part14/v104/docs/7.2.2.2.4

                    // Write chunked NetworkMessage Payload Header (Table 77)
                    encoder.WriteUInt16(null, messages[0].DataSetWriterId);
                }
                else {
                    Debug.Assert(messages.Length <= byte.MaxValue);
                    encoder.WriteByte(null, (byte)messages.Length);
                    // Collect DataSetSetMessages headers
                    foreach (var message in messages) {
                        encoder.WriteUInt16(null, message.DataSetWriterId);
                    }
                }
            }
        }

        /// <summary>
        /// Read extended network message header
        /// </summary>
        private bool TryReadExtendedNetworkMessageHeader(BinaryDecoder decoder) {
            // Decode Timestamp
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.Timestamp) != 0) {
                Timestamp = decoder.ReadDateTime(null);
            }
            // Decode PicoSeconds
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.PicoSeconds) != 0) {
                PicoSeconds = decoder.ReadUInt16(null);
            }
            return true;
        }

        /// <summary>
        /// Write extended network message header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteExtendedNetworkMessageHeader(BinaryEncoder encoder) {
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.Timestamp) != 0) {
                encoder.WriteDateTime(null, Timestamp);
            }
            if ((NetworkMessageContentMask & (uint)UadpNetworkMessageContentMask.PicoSeconds) != 0) {
                encoder.WriteUInt16(null, PicoSeconds);
            }
        }

        /// <summary>
        /// Read security header
        /// </summary>
        /// <param name="decoder"></param>
        private bool TryReadSecurityHeader(BinaryDecoder decoder) {
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.Security) != 0) {
                SecurityFlags = (SecurityFlagsEncodingMask)decoder.ReadByte(null);

                SecurityTokenId = decoder.ReadUInt32(null);
                NonceLength = decoder.ReadByte(null);
                MessageNonce = decoder.ReadByteArray(null).ToArray();

                if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                    SecurityFooterSize = decoder.ReadUInt16(null);
                }
            }
            return true;
        }

        /// <summary>
        /// Write security header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteSecurityHeader(BinaryEncoder encoder) {
            if ((ExtendedFlags1 & ExtendedFlags1EncodingMask.Security) != 0) {
                encoder.WriteByte(null, (byte)SecurityFlags);

                encoder.WriteUInt32(null, SecurityTokenId);
                encoder.WriteByte(null, NonceLength);
                MessageNonce = new byte[NonceLength];
                encoder.WriteByteArray(null, MessageNonce);

                if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                    encoder.WriteUInt16(null, SecurityFooterSize);
                }
            }
        }

        /// <summary>
        /// Decode payload size and prepare for decoding payload
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="chunks"></param>
        private IReadOnlyList<byte[]> ReadPayload(BinaryDecoder decoder, List<Message> chunks) {
            var messages = new List<byte[]>();
            if ((ExtendedFlags2 & ExtendedFlags2EncodingMask.ChunkMessage) != 0) {
                // Write Chunked NetworkMessage Payload Fields (Table 78)
                // https://reference.opcfoundation.org/Core/Part14/v104/docs/7.2.2.2.4
                var messageSequenceNumber = decoder.ReadUInt16(null);
                var chunkOffset = decoder.ReadUInt32(null);
                var totalSize = decoder.ReadUInt32(null);
                var buffer = decoder.ReadByteString(null);

                var chunk = new Message(buffer, Messages.Count > 0 ? Messages[0].DataSetWriterId : (ushort)0);
                chunk.ChunkOffset = chunkOffset;
                chunk.TotalSize = totalSize;
                chunk.MessageSequenceNumber = messageSequenceNumber;
                chunks.Add(chunk);

                var messageBuffer = Message.GetMessageBufferFromChunks(chunks);
                if (messageBuffer == null) {
                    return messages;
                }
                messages.Add(messageBuffer);
            }
            else {
                if ((UadpFlags & UADPFlagsEncodingMask.PayloadHeader) != 0) {
                    // Read PayloadHeader Sizes
                    for (var i = 0; i < Messages.Count; i++) {
                        var messageSize = decoder.ReadUInt16(null);
                        messages.Add(new byte[messageSize]);
                    }
                    foreach (var buffer in messages) {
                        var read = decoder.BaseStream.Read(buffer);
                        Debug.Assert(read == buffer.Length);
                    }
                }
                else {
                    Messages.Add(new UadpDataSetMessage());
                    messages.Add(decoder.BaseStream.ReadAsBuffer().Array);
                }
            }
            return messages;
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

            const int kChunkHeaderSize =
                  2  // MessageSequenceNumber
                + 4  // ChunkOffset
                + 4  // TotalOffset
                + 4  // ByteString Length
                ;

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
                encoder.WriteUInt16(null, chunk.MessageSequenceNumber);
                encoder.WriteUInt32(null, chunk.ChunkOffset);
                encoder.WriteUInt32(null, (uint)chunk.ChunkData.Length);

                var chunkLength = Math.Min(chunk.Remaining, available);
                encoder.WriteInt32(null, chunkLength); // Write byte string
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
                        Debug.Assert(buffer.ChunkData.Length <= ushort.MaxValue);
                        encoder.WriteUInt16(null, (ushort)buffer.ChunkData.Length);
                    }
                }
                foreach (var buffer in writeSpan) {
                    encoder.WriteRawBytes(buffer.ChunkData, 0, buffer.ChunkData.Length);
                }
                remainingChunks = remainingChunks.Slice(writeCount);
                writeSpan = default;
            }
            // Write security header after and finish message
            return true;
        }

        /// <summary>
        /// Read security footer
        /// </summary>
        /// <param name="decoder"></param>
        protected void ReadSecurityFooter(BinaryDecoder decoder) {
            if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                SecurityFooter = decoder.ReadByteArray(null).ToArray();
            }
        }

        /// <summary>
        /// Write security footer
        /// </summary>
        /// <param name="encoder"></param>
        protected void WriteSecurityFooter(BinaryEncoder encoder) {
            if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                encoder.WriteByteArray(null, SecurityFooter);
            }
        }

        /// <summary>
        /// Read signature
        /// </summary>
        /// <param name="decoder"></param>
        protected void ReadSignature(BinaryDecoder decoder) {
            if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                Signature = decoder.ReadByteArray(null).ToArray();
            }
        }

        /// <summary>
        /// Write signature
        /// </summary>
        /// <param name="encoder"></param>
        protected void WriteSignature(BinaryEncoder encoder) {
            if ((SecurityFlags & SecurityFlagsEncodingMask.SecurityFooter) != 0) {
                if (Signature != null && Signature.Length > 0) {
                    encoder.WriteByteArray(null, Signature);
                }
            }
        }

        /// <summary>
        /// Track messages
        /// </summary>
        protected sealed class Message {

            /// <summary>
            /// Data set writer of the dataset message
            /// </summary>
            public ushort DataSetWriterId { get; }

            /// <summary>
            /// Current message sequence number
            /// </summary>
            public ushort MessageSequenceNumber { get; set; }

            /// <summary>
            /// Chunk offset
            /// </summary>
            public uint ChunkOffset { get; set; }

            /// <summary>
            /// Full message buffer
            /// </summary>
            public uint TotalSize { get; set; }

            /// <summary>
            /// Chunk length
            /// </summary>
            public int Remaining => ChunkData.Length - (int)ChunkOffset;

            /// <summary>
            /// Full message buffer
            /// </summary>
            public byte[] ChunkData { get; }

            /// <summary>
            /// Get message buffer from chunk list
            /// </summary>
            /// <param name="chunks"></param>
            /// <returns></returns>
            public static byte[] GetMessageBufferFromChunks(List<Message> chunks) {
                if (chunks.Count == 0) {
                    return null;
                }
                var totalSize = chunks[0].TotalSize;
                if (!chunks.All(a => a.TotalSize == totalSize)) {
                    chunks.Clear();
                    return Array.Empty<byte>();
                }
                var total = chunks.Sum(c => c.ChunkData.Length);
                if (total >= totalSize) {
                    var message = new byte[total];
                    int? firstIndex = null;
                    foreach (var c in chunks.OrderBy(a => a.MessageSequenceNumber)) {
                        if (firstIndex == null) {
                            firstIndex = c.MessageSequenceNumber;
                        }
                        else {
                            firstIndex++;
                            if (c.MessageSequenceNumber != firstIndex) {
                                chunks.Clear();
                                return Array.Empty<byte>();
                            }
                        }
                        Array.Copy(c.ChunkData, 0, message, c.ChunkOffset,
                            c.ChunkData.Length);
                    }
                    chunks.Clear();
                    return message;
                }
                return null;
            }

            /// <summary>
            /// Create chunk
            /// </summary>
            /// <param name="buffer"></param>
            /// <param name="dataSetWriterId"></param>
            public Message(byte[] buffer, ushort dataSetWriterId) {
                ChunkData = buffer;
                TotalSize = (uint)buffer.Length;
                ChunkOffset = 0;
                DataSetWriterId = dataSetWriterId;
                MessageSequenceNumber = 0;
            }
        }

        private UADPFlagsEncodingMask? _uadpFlags;
        private GroupFlagsEncodingMask? _groupFlags;
        private ExtendedFlags2EncodingMask? _extendedFlags2;
        private ExtendedFlags1EncodingMask? _extendedFlags1;
        /// <summary> To update sequence number </summary>
        protected ushort _sequenceNumber;
    }
}
