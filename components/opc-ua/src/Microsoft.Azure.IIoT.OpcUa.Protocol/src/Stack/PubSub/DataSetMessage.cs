// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
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
        /// Dataset writer id
        /// </summary>
        public string DataSetWriterId { get; set; }

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
                !Utils.IsEqual(wrapper.MetaDataVersion, MetaDataVersion) ||
                !Utils.IsEqual(wrapper.SequenceNumber, SequenceNumber) ||
                !Utils.IsEqual(wrapper.Status, Status) ||
                !Utils.IsEqual(wrapper.Timestamp, Timestamp) ||
                !Utils.IsEqual(wrapper.Payload, Payload)) {
                return false;
            }

            return true;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        /// <summary>
        /// Encode as binary
        /// </summary>
        /// <param name="encoder"></param>
        private void EncodeBinary(IEncoder encoder) {
#pragma warning restore IDE0060 // Remove unused parameter

            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Encode as json
        /// </summary>
        /// <param name="encoder"></param>
        private void EncodeJson(IEncoder encoder) {
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.DataSetWriterId) != 0) {
                encoder.WriteString(nameof(JsonDataSetMessageContentMask.DataSetWriterId), DataSetWriterId);
            }
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.SequenceNumber) != 0) {
                encoder.WriteUInt32(nameof(JsonDataSetMessageContentMask.SequenceNumber), SequenceNumber);
            }
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.MetaDataVersion) != 0) {
                encoder.WriteEncodeable(nameof(JsonDataSetMessageContentMask.MetaDataVersion), MetaDataVersion, typeof(ConfigurationVersionDataType));
            }
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.Timestamp) != 0) {
                encoder.WriteDateTime(nameof(JsonDataSetMessageContentMask.Timestamp), Timestamp);
            }
            if ((MessageContentMask & (uint)JsonDataSetMessageContentMask.Status) != 0) {
                encoder.WriteStatusCode(nameof(JsonDataSetMessageContentMask.Status), Status);
            }
            if (Payload != null) {
                encoder.WriteEncodeable(nameof(Payload), Payload, null);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        /// <summary>
        /// Encode as binary
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodeBinary(IDecoder decoder) {
#pragma warning restore IDE0060 // Remove unused parameter

            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Decode as json
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodeJson(IDecoder decoder) {
            DataSetWriterId = decoder.ReadString(nameof(JsonDataSetMessageContentMask.DataSetWriterId));
            if (DataSetWriterId != null) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.DataSetWriterId;
            }

            SequenceNumber = decoder.ReadUInt32(nameof(JsonDataSetMessageContentMask.SequenceNumber));
            if (SequenceNumber != 0) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.SequenceNumber;
            }

            MetaDataVersion = decoder.ReadEncodeable(nameof(JsonDataSetMessageContentMask.MetaDataVersion), typeof(ConfigurationVersionDataType)) as ConfigurationVersionDataType;
            if (MetaDataVersion != null) {
                MessageContentMask|= (uint)JsonDataSetMessageContentMask.MetaDataVersion;
            }

            Timestamp = decoder.ReadDateTime(nameof(JsonDataSetMessageContentMask.Timestamp));
            if (Timestamp != null) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.Timestamp;
            }

            Status = decoder.ReadStatusCode(nameof(JsonDataSetMessageContentMask.Status));
            if (Status != null) {
                MessageContentMask |= (uint)JsonDataSetMessageContentMask.Status;
            }

            Payload = new DataSet();
            Payload.Decode(decoder);
        }
    }
}