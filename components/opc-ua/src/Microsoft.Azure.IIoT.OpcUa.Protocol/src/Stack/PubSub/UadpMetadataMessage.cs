// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Encodeable Metadata message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class UadpMetadataMessage : UadpNetworkMessage {

        /// <summary>
        /// Data set writer name in case of ua-metadata message
        /// </summary>
        public ushort DataSetWriterId { get; set; }

        /// <summary>
        /// Data set metadata in case this is a metadata message
        /// </summary>
        public DataSetMetaDataType MetaData { get; set; }

        /// <summary>
        /// Discovery type
        /// </summary>
        internal UADPNetworkMessageDiscoveryType DiscoveryType { get; set; }
            = UADPNetworkMessageDiscoveryType.DataSetMetaData;

        /// <summary>
        /// The possible types of UADP network discovery response types
        /// </summary>
        [Flags]
        internal enum UADPNetworkMessageDiscoveryType {
            PublisherEndpoint = 2,
            DataSetMetaData = 4,
            DataSetWriterConfiguration = 8
        }

        /// <inheritdoc/>
        public override IReadOnlyList<byte[]> Encode(IServiceMessageContext context, int maxChunkSize) {
            var messages = new List<byte[]>();
            bool isChunkMessage = false;

            var remainingChunks = EncodeDataSetMetaData(context).AsSpan();

            // Re-evaluate flags every go around
            UadpFlags =
                UADPFlagsEncodingMask.PublisherId
              | UADPFlagsEncodingMask.ExtendedFlags1;
            ExtendedFlags1 =
                ExtendedFlags1EncodingMask.Security
              | ExtendedFlags1EncodingMask.PublisherIdTypeString
              | ExtendedFlags1EncodingMask.ExtendedFlags2;
            ExtendedFlags2 =
                ExtendedFlags2EncodingMask.NetworkMessageWithDiscoveryResponse;

            while (remainingChunks.Length == 1) {
                using (var stream = new MemoryStream()) {
                    using (var encoder = new BinaryEncoder(stream, context)) {
                        var writeSpan = remainingChunks;
                        while (true) {
                            WriteNetworkMessageHeader(encoder, isChunkMessage);
                            if (!TryWritePayload(encoder, maxChunkSize, ref writeSpan,
                                ref remainingChunks, ref isChunkMessage)) {
                                encoder.Position = 0; // Restart writing
                                continue;
                            }
                            break;
                        }
                        stream.SetLength(encoder.Position);
                    }
                    messages.Add(stream.ToArray());
                }
            }
            Debug.Assert(remainingChunks.Length == 0);
            return messages;

            Message[] EncodeDataSetMetaData(IServiceMessageContext context) {
                using (var stream = new MemoryStream()) {
                    using (var encoder = new BinaryEncoder(stream, context)) {
                        switch (DiscoveryType) {
                            case UADPNetworkMessageDiscoveryType.DataSetMetaData:
                                encoder.WriteUInt16("DataSetWriterId", DataSetWriterId);
                                encoder.WriteEncodeable("MetaData", MetaData,
                                    typeof(DataSetMetaDataType));
                                // temporary write StatusCode.Good
                                encoder.WriteStatusCode("StatusCode", StatusCodes.Good);
                                break;
                            case UADPNetworkMessageDiscoveryType.DataSetWriterConfiguration:
                            case UADPNetworkMessageDiscoveryType.PublisherEndpoint:
                                // not implemented
                                break;
                        }

                    }
                    return new[] { new Message(stream.ToArray(), DataSetWriterId) };
                }
            }
        }

        /// <inheritdoc/>
        public override bool TryDecode(IServiceMessageContext context, IEnumerable<byte[]> reader) {
            return base.TryDecode(context, reader);
        }

        /// <inheritdoc/>
        public override bool Equals(object value) {
            if (ReferenceEquals(this, value)) {
                return true;
            }
            if (!(value is UadpMetadataMessage wrapper)) {
                return false;
            }
            if (!base.Equals(value)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.DataSetWriterGroup, DataSetWriterGroup) ||
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
            hash.Add(DataSetWriterGroup);
            hash.Add(DataSetWriterId);
            hash.Add(MetaData);
            return hash.ToHashCode();
        }
    }
}