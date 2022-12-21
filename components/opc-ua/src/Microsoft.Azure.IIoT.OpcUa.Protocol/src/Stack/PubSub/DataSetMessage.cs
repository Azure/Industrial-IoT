// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Opc.Ua.Encoders;
    using System;

    /// <summary>
    /// Data set message
    /// </summary>
    public class DataSetMessage : IEncodeable {

        /// <summary>
        /// Content mask
        /// </summary>
        public uint MessageContentMask { get; set; }

        /// <summary>
        /// Dataset message type
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Dataset writer id
        /// </summary>
        public ushort DataSetWriterId { get; set; }

        /// <summary>
        /// Dataset writer name
        /// </summary>
        public string DataSetWriterName { get; set; }

        /// <summary>
        /// Sequence number
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Metadata version
        /// </summary>
        public ConfigurationVersionDataType MetaDataVersion { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Picoseconds
        /// </summary>
        public uint Picoseconds { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public StatusCode Status { get; set; }

        /// <summary>
        /// Payload
        /// </summary>
        public DataSet Payload { get; set; }

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
                    throw new NotImplementedException("XML encoding is not implemented.");
                default:
                    throw new NotImplementedException($"Unknown encoding: {decoder.EncodingType}");
            }
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            ApplyEncodeMask();
            switch (encoder.EncodingType) {
                case EncodingType.Binary:
                    EncodeBinary(encoder);
                    break;
                case EncodingType.Json:
                    EncodeJson(encoder);
                    break;
                case EncodingType.Xml:
                    throw new NotImplementedException("XML encoding is not implemented.");
                default:
                    throw new NotImplementedException($"Unknown encoding: {encoder.EncodingType}");
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object value) {
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
            if (!(encodeable is DataSetMessage wrapper)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.MessageContentMask, MessageContentMask) ||
                !Utils.IsEqual(wrapper.DataSetWriterId, DataSetWriterId) ||
                !Utils.IsEqual(wrapper.DataSetWriterName, DataSetWriterName) ||
                !Utils.IsEqual(wrapper.MessageType, MessageType) ||
                !Utils.IsEqual(wrapper.MetaDataVersion, MetaDataVersion) ||
                !Utils.IsEqual(wrapper.SequenceNumber, SequenceNumber) ||
                !Utils.IsEqual(wrapper.Status, Status) ||
                !Utils.IsEqual(wrapper.Timestamp, Timestamp) ||
                !Utils.IsEqual(wrapper.Payload, Payload) ||
                !Utils.IsEqual(wrapper.BinaryEncodingId, BinaryEncodingId) ||
                !Utils.IsEqual(wrapper.TypeId, TypeId) ||
                !Utils.IsEqual(wrapper.XmlEncodingId, XmlEncodingId)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        private void ApplyEncodeMask() {
            if (Payload == null) {
                return;
            }
            foreach (var value in Payload.Values) {
                if (value == null) {
                    continue;
                }
                if ((Payload.FieldContentMask & (uint)DataSetFieldContentMask.RawData) != 0 ||
                    (Payload.FieldContentMask & (uint)DataSetFieldContentMask.StatusCode) == 0) {
                    value.StatusCode = StatusCodes.Good;
                }
                if ((Payload.FieldContentMask & (uint)DataSetFieldContentMask.RawData) != 0 ||
                    (Payload.FieldContentMask & (uint)DataSetFieldContentMask.SourceTimestamp) == 0) {
                    value.SourceTimestamp = DateTime.MinValue;
                }
                if ((Payload.FieldContentMask & (uint)DataSetFieldContentMask.RawData) != 0 ||
                    (Payload.FieldContentMask & (uint)DataSetFieldContentMask.ServerTimestamp) == 0) {
                    value.ServerTimestamp = DateTime.MinValue;
                }
                if ((Payload.FieldContentMask & (uint)DataSetFieldContentMask.RawData) != 0 ||
                    (Payload.FieldContentMask & (uint)DataSetFieldContentMask.SourcePicoSeconds) == 0) {
                    value.SourcePicoseconds = 0;
                }
                if ((Payload.FieldContentMask & (uint)DataSetFieldContentMask.RawData) != 0 ||
                    (Payload.FieldContentMask & (uint)DataSetFieldContentMask.ServerPicoSeconds) == 0) {
                    value.ServerPicoseconds = 0;
                }
            }
        }

        /// <inheritdoc/>
        private void EncodeBinary(IEncoder encoder) {
            encoder.WriteUInt32(nameof(MessageContentMask), MessageContentMask);
            encoder.WriteUInt16(nameof(DataSetWriterId), DataSetWriterId);
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.SequenceNumber) != 0) {
                encoder.WriteUInt32(nameof(UadpDataSetMessageContentMask.SequenceNumber), SequenceNumber);
            }
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.MajorVersion) != 0) {
                encoder.WriteUInt32(nameof(UadpDataSetMessageContentMask.MajorVersion), MetaDataVersion.MajorVersion);
            }
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.MinorVersion) != 0) {
                encoder.WriteUInt32(nameof(UadpDataSetMessageContentMask.MinorVersion), MetaDataVersion.MinorVersion);
            }
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.Timestamp) != 0) {
                encoder.WriteDateTime(nameof(UadpDataSetMessageContentMask.Timestamp), Timestamp);
            }
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.PicoSeconds) != 0) {
                encoder.WriteUInt32(nameof(UadpDataSetMessageContentMask.PicoSeconds), Picoseconds);
            }
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.Status) != 0) {
                encoder.WriteStatusCode(nameof(Status), Status);
            }
            if (Payload != null) {
                var payload = new KeyDataValuePairCollection();
                foreach (var tuple in Payload) {
                    payload.Add(new KeyDataValuePair() {
                        Key = tuple.Key,
                        Value = tuple.Value
                    });
                }
                encoder.WriteEncodeableArray(nameof(Payload), payload.ToArray(), typeof(KeyDataValuePair));
            }

        }

        /// <inheritdoc/>
        private void EncodeJson(IEncoder encoder) {
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.DataSetWriterId) != 0) {
                encoder.WriteUInt16(nameof(DataSetWriterId), DataSetWriterId);
            }
            if ((MessageContentMask & 64u /*(uint)JsonDataSetMessageContentMask.DataSetWriterName TODO: Remove with 1.05 */) != 0) {
                encoder.WriteString(nameof(DataSetWriterName), DataSetWriterName);
            }
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.SequenceNumber) != 0) {
                encoder.WriteUInt32(nameof(SequenceNumber), SequenceNumber);
            }
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.MetaDataVersion) != 0) {
                encoder.WriteEncodeable(nameof(MetaDataVersion), MetaDataVersion, typeof(ConfigurationVersionDataType));
            }
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.Timestamp) != 0) {
                encoder.WriteDateTime(nameof(Timestamp), Timestamp);
            }
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.Status) != 0) {
                encoder.WriteStatusCode(nameof(Status), Status);
            }
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.MessageType) != 0) {
                switch (MessageType) {
                    case MessageType.KeyFrame:
                        encoder.WriteString(nameof(MessageType), "ua-keyframe");
                        break;
                    case MessageType.Event:
                        encoder.WriteString(nameof(MessageType), "ua-event");
                        break;
                    case MessageType.KeepAlive:
                        encoder.WriteString(nameof(MessageType), "ua-keepalive");
                        break;
                    case MessageType.Condition:
                        encoder.WriteString(nameof(MessageType), "ua-condition");
                        break;
                    case MessageType.DeltaFrame:
                        encoder.WriteString(nameof(MessageType), "ua-deltaframe");
                        break;
                }
            }
            if (Payload != null) {
                var jsonEncoder = encoder as JsonEncoderEx;
                jsonEncoder.WriteDataValueDictionary(nameof(Payload), Payload);
            }
        }

        /// <inheritdoc/>
        private void DecodeBinary(IDecoder decoder) {
            MessageContentMask = decoder.ReadUInt32(nameof(MessageContentMask));
            DataSetWriterId = decoder.ReadUInt16(nameof(DataSetWriterId));

            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.SequenceNumber) != 0) {
                SequenceNumber = decoder.ReadUInt32(nameof(UadpDataSetMessageContentMask.SequenceNumber));
            }
            MetaDataVersion = new ConfigurationVersionDataType() {
                MajorVersion = 0,
                MinorVersion = 0
            };
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.MajorVersion) != 0) {
                MetaDataVersion.MajorVersion = decoder.ReadUInt32(nameof(UadpDataSetMessageContentMask.MajorVersion));
            }
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.MinorVersion) != 0) {
                MetaDataVersion.MinorVersion = decoder.ReadUInt32(nameof(UadpDataSetMessageContentMask.MinorVersion));
            }
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.Timestamp) != 0) {
                Timestamp = decoder.ReadDateTime(nameof(UadpDataSetMessageContentMask.Timestamp));
            }
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.PicoSeconds) != 0) {
                Picoseconds = decoder.ReadUInt32(nameof(UadpDataSetMessageContentMask.PicoSeconds));
            }
            if ((MessageContentMask & (uint)UadpDataSetMessageContentMask.Status) != 0) {
                Status = decoder.ReadStatusCode(nameof(Status));
            }
            var payload = (KeyDataValuePairCollection)decoder.ReadEncodeableArray(nameof(Payload), typeof(KeyDataValuePair));
            Payload = new DataSet();
            foreach (var tuple in payload) {
                Payload[tuple.Key] = new DataValue(tuple.Value);
            }
        }

        /// <inheritdoc/>
        private void DecodeJson(IDecoder decoder) {
            DataSetWriterName = decoder.ReadString(nameof(DataSetWriterName));
            if (DataSetWriterName == null) {
                // Try legacy format
                DataSetWriterName = decoder.ReadString(nameof(DataSetWriterId));
            }
            if (DataSetWriterName != null) {
                MessageContentMask |= 64u; // TODO: Add with 1.05 (uint)JsonDataSetMessageContentMask.DataSetWriterName;
            }
            DataSetWriterId = decoder.ReadUInt16(nameof(DataSetWriterId));
            if (DataSetWriterId != 0) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.DataSetWriterId;
            }
            SequenceNumber = decoder.ReadUInt32(nameof(SequenceNumber));
            if (SequenceNumber != 0) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.SequenceNumber;
            }
            MetaDataVersion = decoder.ReadEncodeable(nameof(MetaDataVersion), typeof(ConfigurationVersionDataType))
                as ConfigurationVersionDataType;
            if (MetaDataVersion != null) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.MetaDataVersion;
            }
            Timestamp = decoder.ReadDateTime(nameof(Timestamp));
            if (Timestamp != DateTime.MinValue) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.Timestamp;
            }
            Status = decoder.ReadStatusCode(nameof(Status));
            if (Status != StatusCodes.Good) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.Status;
            }
            var messageType = decoder.ReadString(nameof(MessageType));
            if (messageType != null) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.MessageType;

                if (messageType.Equals("ua-deltaframe")) {
                    MessageType = MessageType.DeltaFrame;
                }
                else if (messageType.Equals("ua-event")) {
                    MessageType = MessageType.Event;
                }
                else if (messageType.Equals("ua-keepalive")) {
                    MessageType = MessageType.KeepAlive;
                }
                else if (messageType.Equals("ua-condition")) {
                    MessageType = MessageType.Condition;
                }
                else if (messageType.Equals("ua-keyframe")) {
                    MessageType = MessageType.KeyFrame;
                }
                else {
                    MessageType = MessageType.None;
                }
            }

            var jsonDecoder = decoder as JsonDecoderEx;
            var payload = jsonDecoder.ReadDataValueDictionary(nameof(Payload));
            Payload = new DataSet(payload, 0);
        }
    }
}