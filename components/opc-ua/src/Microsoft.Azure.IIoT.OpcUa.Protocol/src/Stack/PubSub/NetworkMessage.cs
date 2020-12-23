// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encodeable Network message
    /// </summary>
    public class NetworkMessage : IEncodeable {

        /// <summary>
        /// Message content
        /// </summary>
        public uint MessageContentMask { get; set; }

        /// <summary>
        /// Message id
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Publisher identifier
        /// </summary>
        public string PublisherId { get; set; }

        /// <summary>
        /// Dataset class
        /// </summary>
        public string DataSetClassId { get; set; }

        /// <summary>
        /// Dataset writerGroup
        /// </summary>
        public string DataSetWriterGroup { get; set; }

        /// <summary>
        /// Message type
        /// </summary>
        public string MessageType { get; set; } = "ua-data";

        /// <summary>
        /// DataSet Messages
        /// </summary>
        public List<DataSetMessage> Messages { get; set; } = new List<DataSetMessage>();

        /// <inheritdoc/>
        public ExpandedNodeId TypeId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public void Decode(IDecoder decoder) {
            switch (decoder.EncodingType) {
                case EncodingType.Binary:
                    DecodeBinary(decoder);
                    break;
                case EncodingType.Json:
                    DecodeJson(decoder);
                    break;
                case EncodingType.Xml:
                    throw new NotSupportedException("XML encoding is not supported.");
                default:
                    throw new NotImplementedException(
                        $"Unknown encoding: {decoder.EncodingType}");
            }
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            switch (encoder.EncodingType) {
                case EncodingType.Binary:
                    EncodeBinary(encoder);
                    break;
                case EncodingType.Json:
                    EncodeJson(encoder);
                    break;
                case EncodingType.Xml:
                    throw new NotSupportedException("XML encoding is not supported.");
                default:
                    throw new NotImplementedException(
                        $"Unknown encoding: {encoder.EncodingType}");
            }
        }

        /// <inheritdoc/>
        public override bool Equals(Object value) {
            return IsEqual(value as IEncodeable);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }
            if (!(encodeable is NetworkMessage wrapper)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.MessageContentMask, MessageContentMask) ||
                !Utils.IsEqual(wrapper.MessageId, MessageId) ||
                !Utils.IsEqual(wrapper.DataSetClassId, DataSetClassId) ||
                !Utils.IsEqual(wrapper.DataSetWriterGroup, DataSetWriterGroup) ||
                !Utils.IsEqual(wrapper.BinaryEncodingId, BinaryEncodingId) ||
                !Utils.IsEqual(wrapper.MessageType, MessageType) ||
                !Utils.IsEqual(wrapper.PublisherId, PublisherId) ||
                !Utils.IsEqual(wrapper.TypeId, TypeId) ||
                !Utils.IsEqual(wrapper.XmlEncodingId, XmlEncodingId) ||
                !Utils.IsEqual(wrapper.Messages, Messages)) {
                return false;
            }
            if (Messages.Count != wrapper.Messages.Count) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Decode from binary
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodeBinary(IDecoder decoder) {
            MessageContentMask = decoder.ReadUInt32(nameof(MessageContentMask));
            if ((MessageContentMask & (uint)UadpNetworkMessageContentMask.NetworkMessageNumber) != 0) {
                MessageId = decoder.ReadString(nameof(MessageId));
            }
            MessageType = decoder.ReadString(nameof(MessageType));
            if (MessageType != "ua-data") {
                // todo throw incorrect message format
            }
            if ((MessageContentMask & (uint)UadpNetworkMessageContentMask.PublisherId) != 0) {
                PublisherId = decoder.ReadString(nameof(PublisherId));
            }
            if ((MessageContentMask & (uint)UadpNetworkMessageContentMask.DataSetClassId) != 0) {
                DataSetClassId = decoder.ReadString(nameof(DataSetClassId));
            }
            var messagesArray = decoder.ReadEncodeableArray("Messages", typeof(DataSetMessage));
            Messages = new List<DataSetMessage>();
            foreach (var value in messagesArray) {
                Messages.Add(value as DataSetMessage);
            }
        }

        /// <inheritdoc/>
        private void DecodeJson(IDecoder decoder) {
            MessageContentMask = 0;
            MessageId = decoder.ReadString(nameof(MessageId));
            if (MessageId != null) {
                MessageContentMask |= (uint)JsonNetworkMessageContentMask.NetworkMessageHeader;
            }
            MessageType = decoder.ReadString(nameof(MessageType));
            if (MessageType != "ua-data"){
                // todo throw incorrect message format
            }
            PublisherId = decoder.ReadString(nameof(PublisherId));
            if (PublisherId != null) {
                MessageContentMask |= (uint)JsonNetworkMessageContentMask.PublisherId;
            }
            DataSetClassId = decoder.ReadString(nameof(DataSetClassId));
            if(DataSetClassId != null){
                MessageContentMask |= (uint)JsonNetworkMessageContentMask.DataSetClassId;
            }
            DataSetWriterGroup = decoder.ReadString(nameof(DataSetWriterGroup));

            var messagesArray = decoder.ReadEncodeableArray("Messages", typeof(DataSetMessage));
            Messages = new List<DataSetMessage>();
            foreach (var value in messagesArray) {
                Messages.Add(value as DataSetMessage);
            }
            if (Messages.Count == 1) {
                MessageContentMask |= (uint)JsonNetworkMessageContentMask.SingleDataSetMessage;
            }
        }

        /// <summary>
        /// Encode as binary
        /// </summary>
        /// <param name="encoder"></param>
        private void EncodeBinary(IEncoder encoder) {
            encoder.WriteUInt32(nameof(MessageContentMask), MessageContentMask);
            if ((MessageContentMask & (uint)UadpNetworkMessageContentMask.NetworkMessageNumber) != 0) {
                encoder.WriteString(nameof(MessageId), MessageId);
            }
            encoder.WriteString(nameof(MessageType), MessageType);
            if ((MessageContentMask & (uint)UadpNetworkMessageContentMask.PublisherId) != 0) {
                encoder.WriteString(nameof(PublisherId), PublisherId);
            }
            if ((MessageContentMask & (uint)UadpNetworkMessageContentMask.DataSetClassId) != 0) {
                encoder.WriteString(nameof(DataSetClassId), DataSetClassId);
            }
            if (Messages != null && Messages.Count > 0) {
                encoder.WriteEncodeableArray(nameof(Messages), Messages.ToArray(), typeof(DataSetMessage[]));
            }
        }

        /// <summary>
        /// Encode as json
        /// </summary>
        /// <param name="encoder"></param>
        private void EncodeJson(IEncoder encoder) {
            if ((MessageContentMask & (uint)JsonNetworkMessageContentMask.NetworkMessageHeader) != 0) {
                encoder.WriteString(nameof(MessageId), MessageId);
                encoder.WriteString(nameof(MessageType), MessageType);
                if ((MessageContentMask & (uint)JsonNetworkMessageContentMask.PublisherId) != 0) {
                    encoder.WriteString(nameof(PublisherId), PublisherId);
                }
                if ((MessageContentMask & (uint)JsonNetworkMessageContentMask.DataSetClassId) != 0 &&
                    !string.IsNullOrEmpty(DataSetClassId)) {
                    encoder.WriteString(nameof(DataSetClassId), DataSetClassId);
                }
                if (!string.IsNullOrEmpty(DataSetWriterGroup)) {
                    encoder.WriteString(nameof(DataSetWriterGroup), DataSetWriterGroup);
                }
                if (Messages != null && Messages.Count > 0) {
                    if ((MessageContentMask & (uint)JsonNetworkMessageContentMask.SingleDataSetMessage) != 0) {
                        encoder.WriteEncodeable(nameof(Messages), Messages[0], typeof(DataSetMessage));
                    }
                    else {
                        encoder.WriteEncodeableArray(nameof(Messages), Messages.ToArray(), typeof(DataSetMessage[]));
                    }
                }
            }
        }
    }
}