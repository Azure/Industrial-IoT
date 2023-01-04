﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Discovery announcements and probes base class
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class UadpDiscoveryMessage : UadpNetworkMessage {

        /// <summary>
        /// Whether this is a discovery probe
        /// </summary>
        internal bool IsProbe { get; set; }

        /// <summary>
        /// Data set writer name in case of ua-metadata message
        /// </summary>
        public ushort DataSetWriterId { get; set; }

        /// <summary>
        /// Data set metadata in case this is a metadata message
        /// </summary>
        public DataSetMetaDataType MetaData { get; set; }

        /// <summary>
        /// If discovery request
        /// </summary>
        internal ushort[] DataSetWriterIds { get; set; }

        /// <summary>
        /// Discovery type
        /// </summary>
        internal byte DiscoveryType { get; set; }

        /// <summary>
        /// The possible types of UADP network discovery announcement types
        /// </summary>
        [Flags]
        internal enum UADPDiscoveryAnnouncementType {
            Reserved = 0,
            PublisherEndpoints = 1,
            DataSetMetaData = 2,
            DataSetWriterConfiguration = 3,
            PubSubConnectionsConfiguration = 4,
            ApplicationInformation = 5
        }

        /// <summary>
        /// The possible types of UADP discovery probe types
        /// </summary>
        [Flags]
        internal enum UADPDiscoveryProbeType {
            Reserved = 0,
            PublisherInformationProbe = 1,
            FindApplicationsProbe = 2
        }

        /// <summary>
        /// The possible types of UADP publisher info probe message
        /// </summary>
        [Flags]
        internal enum PublisherInformationProbeMessageType {
            Reserved = 0,
            PublisherServerEndpoints = 1,
            DataSetMetaData = 2,
            DataSetWriterConfiguration = 3,
            WriterGroupConfiguration = 4,
            PubSubConnectionsConfiguration = 5
        }

        /// <inheritdoc/>
        public override IReadOnlyList<byte[]> Encode(IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver resolver) {
            var messages = new List<byte[]>();
            bool isChunkMessage = false;
            var remainingChunks = EncodePayloadChunks(context, resolver).AsSpan();

            // Re-evaluate flags every go around
            UadpFlags =
                UADPFlagsEncodingMask.PublisherId
              | UADPFlagsEncodingMask.ExtendedFlags1;
            ExtendedFlags1 =
                ExtendedFlags1EncodingMask.Security
              | ExtendedFlags1EncodingMask.PublisherIdTypeString
              | ExtendedFlags1EncodingMask.ExtendedFlags2;
            ExtendedFlags2 = IsProbe ?
                ExtendedFlags2EncodingMask.DiscoveryProbe :
                ExtendedFlags2EncodingMask.DiscoveryAnnouncement;

            while (remainingChunks.Length == 1) {
                using (var stream = Memory.GetStream()) {
                    using (var encoder = new BinaryEncoder(stream, context, leaveOpen: true)) {
                        var writeSpan = remainingChunks;
                        while (true) {
                            WriteNetworkMessageHeaderFlags(encoder, isChunkMessage);
                            WriteNetworkMessageHeader(encoder);

                            if (!TryWritePayload(encoder, maxChunkSize, ref writeSpan,
                                ref remainingChunks, ref isChunkMessage)) {
                                encoder.Position = 0; // Restart writing
                                continue;
                            }

                            WriteSecurityFooter(encoder);
                            WriteSignature(encoder);
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
            Debug.Assert(remainingChunks.Length == 0);
            return messages;
        }

        /// <inheritdoc/>
        protected override bool TryReadNetworkMessageHeader(BinaryDecoder decoder, bool isFirstChunk) {
            if ((ExtendedFlags2 & ExtendedFlags2EncodingMask.DiscoveryProbe) != 0) {
                // Read probetype
                DiscoveryType = decoder.ReadByte(null);
                DataSetWriterIds = decoder.ReadUInt16Array(null).ToArray();
                IsProbe = true;
                return true;
            }
            if ((ExtendedFlags2 & ExtendedFlags2EncodingMask.DiscoveryAnnouncement) != 0) {
                // Read announcement type
                DiscoveryType = decoder.ReadByte(null);
                _sequenceNumber = decoder.ReadUInt16(null);
                IsProbe = false;
                return true;
            }
            // Not a discovery message
            return false;
        }

        /// <summary>
        /// Write discovery header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteNetworkMessageHeader(BinaryEncoder encoder) {
            if (IsProbe) {
                // Write probe type
                encoder.WriteByte(null, DiscoveryType);
                encoder.WriteUInt16Array(null, DataSetWriterIds ?? Array.Empty<ushort>());
                ExtendedFlags2 &= ~ExtendedFlags2EncodingMask.DiscoveryAnnouncement;
                ExtendedFlags2 |= ExtendedFlags2EncodingMask.DiscoveryProbe;
            }
            else {
                // Write announcement type
                encoder.WriteByte(null, DiscoveryType);
                encoder.WriteUInt16(null, SequenceNumber());
                ExtendedFlags2 &= ~ExtendedFlags2EncodingMask.DiscoveryProbe;
                ExtendedFlags2 |= ExtendedFlags2EncodingMask.DiscoveryAnnouncement;
            }
        }

        /// <inheritdoc/>
        protected override Message[] EncodePayloadChunks(IServiceMessageContext context,
            IDataSetMetaDataResolver resolver) {
            using (var stream = Memory.GetStream()) {
                using (var encoder = new BinaryEncoder(stream, context, leaveOpen: true)) {
                    if (!IsProbe) {
                        switch ((UADPDiscoveryAnnouncementType)DiscoveryType) {
                            case UADPDiscoveryAnnouncementType.DataSetMetaData:
                                encoder.WriteUInt16(null, DataSetWriterId);
                                encoder.WriteEncodeable(null, MetaData,
                                    typeof(DataSetMetaDataType));
                                // temporary write StatusCode.Good
                                encoder.WriteStatusCode(null, StatusCodes.Good);
                                break;
                            case UADPDiscoveryAnnouncementType.DataSetWriterConfiguration:
                            case UADPDiscoveryAnnouncementType.PublisherEndpoints:
                            case UADPDiscoveryAnnouncementType.PubSubConnectionsConfiguration:
                            case UADPDiscoveryAnnouncementType.ApplicationInformation:
                                // not implemented
                                break;
                        }
                    }
                    else {
                        switch ((UADPDiscoveryProbeType)DiscoveryType) {
                            case UADPDiscoveryProbeType.PublisherInformationProbe:
                            case UADPDiscoveryProbeType.FindApplicationsProbe:
                                // not implemented
                                break;
                        }
                    }

                    // TODO: instead of copy using ToArray we shall include the
                    // stream with the message and dispose it later when it is
                    // consumed. To get here the bug in BinaryEncoder that it
                    // disposes the underlying stream even if leaveOpen: true
                    // is set must be fixed.
                    return new[] { new Message(stream.ToArray(), DataSetWriterId) };
                }
            }
        }

        /// <inheritdoc/>
        protected override void DecodePayloadChunks(IServiceMessageContext context,
            IReadOnlyList<byte[]> buffers, IDataSetMetaDataResolver resolver) {
            if (buffers.Count == 0) {
                return;
            }
            Debug.Assert(buffers.Count == 1);
            using (var stream = Memory.GetStream(buffers[0])) {
                using (var decoder = new BinaryDecoder(stream, context)) {
                    if ((ExtendedFlags2 & ExtendedFlags2EncodingMask.DiscoveryAnnouncement) != 0) {
                        switch ((UADPDiscoveryAnnouncementType)DiscoveryType) {
                            case UADPDiscoveryAnnouncementType.DataSetMetaData:
                                DataSetWriterId = decoder.ReadUInt16(null);
                                MetaData = (DataSetMetaDataType)decoder.ReadEncodeable(null,
                                    typeof(DataSetMetaDataType));
                                // temporary read
                                var status = decoder.ReadStatusCode(null);
                                break;
                            case UADPDiscoveryAnnouncementType.DataSetWriterConfiguration:
                            case UADPDiscoveryAnnouncementType.PublisherEndpoints:
                            case UADPDiscoveryAnnouncementType.PubSubConnectionsConfiguration:
                            case UADPDiscoveryAnnouncementType.ApplicationInformation:
                                // not implemented
                                break;
                        }
                    }
                    else if ((ExtendedFlags2 & ExtendedFlags2EncodingMask.DiscoveryProbe) != 0) {
                        switch ((UADPDiscoveryProbeType)DiscoveryType) {
                            case UADPDiscoveryProbeType.PublisherInformationProbe:
                            case UADPDiscoveryProbeType.FindApplicationsProbe:
                                // not implemented
                                break;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object value) {
            if (ReferenceEquals(this, value)) {
                return true;
            }
            if (!(value is UadpDiscoveryMessage wrapper)) {
                return false;
            }
            if (!base.Equals(value)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.DataSetWriterId, DataSetWriterId) ||
                !Utils.IsEqual(wrapper.MetaData, MetaData)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(DataSetWriterId);
            hash.Add(MetaData);
            return hash.ToHashCode();
        }
    }
}