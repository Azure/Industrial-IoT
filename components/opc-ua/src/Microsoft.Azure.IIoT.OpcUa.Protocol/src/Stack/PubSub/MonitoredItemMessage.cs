// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using Opc.Ua.Encoders;
    using System.Collections.Generic;

    /// <summary>
    /// Encodeable monitored item message
    /// </summary>
    public class MonitoredItemMessage : IEncodeable {

        /// <summary>
        /// Content mask
        /// </summary>
        public uint MessageContentMask { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        public ExpandedNodeId NodeId { get; set; }

        /// <summary>
        /// Endpoint url
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Timestamp assigned by publisher 
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Data value for variable change notification
        /// </summary>
        public DataValue Value { get; set; }

        /// <summary>
        /// Sequence number
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Extension fields
        /// </summary>
        public Dictionary<string, string> ExtensionFields { get; set; }

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
            ApplyEncodeMask();
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
            if (!(encodeable is MonitoredItemMessage wrapper)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.MessageContentMask, MessageContentMask) ||
                !Utils.IsEqual(wrapper.NodeId, NodeId) ||
                !Utils.IsEqual(wrapper.EndpointUrl, EndpointUrl) ||
                !Utils.IsEqual(wrapper.ApplicationUri, ApplicationUri) ||
                !Utils.IsEqual(wrapper.DisplayName, DisplayName) ||
                !Utils.IsEqual(wrapper.Timestamp, Timestamp) ||
                !Utils.IsEqual(wrapper.Value, Value) ||
                !Utils.IsEqual(wrapper.ExtensionFields, ExtensionFields) ||
                !Utils.IsEqual(wrapper.BinaryEncodingId, BinaryEncodingId) ||
                !Utils.IsEqual(wrapper.TypeId, TypeId) ||
                !Utils.IsEqual(wrapper.XmlEncodingId, XmlEncodingId)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        private void ApplyEncodeMask() {

            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.NodeId) == 0) {
                NodeId = null;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.EndpointUrl) == 0) {
                EndpointUrl = null;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ApplicationUri) == 0) {
                ApplicationUri = null;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.DisplayName) == 0) {
                DisplayName = null;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Timestamp) == 0) {
                Timestamp = DateTime.MinValue;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ExtensionFields) == 0) {
                ExtensionFields = null;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SequenceNumber) == 0) {
                SequenceNumber = 0;
            }
            if (Value == null) {
                // no point to go further
                return;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Status) == 0 &&
                (MessageContentMask & (uint)MonitoredItemMessageContentMask.StatusCode) == 0) {
                Value.StatusCode = StatusCodes.Good;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SourceTimestamp) == 0) {
                Value.SourceTimestamp = DateTime.MinValue;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ServerTimestamp) == 0) {
                Value.ServerTimestamp = DateTime.MinValue;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SourcePicoSeconds) == 0) {
                Value.SourcePicoseconds = 0;
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ServerPicoSeconds) == 0) {
                Value.ServerPicoseconds = 0;
            }
        }

        /// <inheritdoc/>
        private void DecodeBinary(IDecoder decoder) {

            MessageContentMask = decoder.ReadUInt32("ContentMask");
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.NodeId) != 0) {
                NodeId = decoder.ReadExpandedNodeId(nameof(MonitoredItemMessageContentMask.NodeId));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.EndpointUrl) != 0) {
                EndpointUrl = decoder.ReadString(nameof(MonitoredItemMessageContentMask.EndpointUrl));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ApplicationUri) != 0) {
                ApplicationUri = decoder.ReadString(nameof(MonitoredItemMessageContentMask.ApplicationUri));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.DisplayName) != 0) {
                DisplayName = decoder.ReadString(nameof(MonitoredItemMessageContentMask.DisplayName));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Timestamp) != 0) {
                Timestamp = decoder.ReadDateTime(nameof(MonitoredItemMessageContentMask.Timestamp));
            }
            Value = decoder.ReadDataValue("Value");
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SequenceNumber) != 0) {
                SequenceNumber = decoder.ReadUInt32(nameof(MonitoredItemMessageContentMask.SequenceNumber));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ExtensionFields) != 0) {
                var dictionary = (KeyValuePairCollection)decoder.ReadEncodeableArray("ExtensionFields", typeof(Ua.KeyValuePair));
                ExtensionFields = new Dictionary<string, string>(dictionary.Count);
                foreach (var item in dictionary) {
                    ExtensionFields[item.Key.Name] = item.Value.ToString();
                }
            }
        }

        private void DecodeJson(IDecoder decoder) {
            NodeId = decoder.ReadExpandedNodeId(nameof(MonitoredItemMessageContentMask.NodeId));
            if (NodeId != null) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.NodeId;
            }
            EndpointUrl = decoder.ReadString(nameof(MonitoredItemMessageContentMask.EndpointUrl));
            if (EndpointUrl != null) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.EndpointUrl;
            }
            ApplicationUri = decoder.ReadString(nameof(MonitoredItemMessageContentMask.ApplicationUri));
            if (ApplicationUri != null) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.ApplicationUri;
            }
            DisplayName = decoder.ReadString(nameof(MonitoredItemMessageContentMask.DisplayName));
            if (DisplayName != null) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.DisplayName;
            }
            Timestamp = decoder.ReadDateTime(nameof(MonitoredItemMessageContentMask.Timestamp));
            if (Timestamp != DateTime.MinValue) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.Timestamp;
            }
            Value = decoder.ReadDataValue("Value");
            if (Value.ServerTimestamp != null) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.ServerTimestamp;
            }
            if (Value.ServerPicoseconds != 0) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.ServerPicoSeconds;
            }
            if (Value.SourceTimestamp != null) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.SourceTimestamp;
            }
            if (Value.SourcePicoseconds != 0) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.SourcePicoSeconds;
            }
            if (Value.StatusCode != 0) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.StatusCode;
            }
            SequenceNumber = decoder.ReadUInt32(nameof(MonitoredItemMessageContentMask.SequenceNumber));
            if (SequenceNumber != 0) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.SequenceNumber;
            }
            var jsonDecoder = decoder as JsonDecoderEx;
            ExtensionFields = (Dictionary<string,string>)jsonDecoder.ReadStringDictionary(nameof(ExtensionFields));
            if (ExtensionFields != null) {
                MessageContentMask |= (uint)MonitoredItemMessageContentMask.ExtensionFields;
            }
        }

        /// <inheritdoc/>
        private void EncodeBinary(IEncoder encoder) {

            encoder.WriteUInt32("ContentMask", MessageContentMask);
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.NodeId) != 0) {
                encoder.WriteExpandedNodeId(nameof(MonitoredItemMessageContentMask.NodeId), NodeId);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.EndpointUrl) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.EndpointUrl), EndpointUrl);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ApplicationUri) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.ApplicationUri), ApplicationUri);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.DisplayName) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.DisplayName), DisplayName);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Timestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemMessageContentMask.Timestamp), Timestamp);
            }
            encoder.WriteDataValue("Value", Value);
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SequenceNumber) != 0) {
                encoder.WriteUInt32(nameof(MonitoredItemMessageContentMask.SequenceNumber), SequenceNumber);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ExtensionFields) != 0) {
                if (ExtensionFields != null) {
                    var dictionary = new KeyValuePairCollection();
                    foreach (var item in ExtensionFields) {
                        dictionary.Add(new Ua.KeyValuePair(){
                            Key = item.Key,
                            Value = item.Value});
                    }
                    encoder.WriteEncodeableArray("ExtensionFields", dictionary.ToArray(), typeof(Ua.KeyValuePair));
                }
            }
        }

        /// <inheritdoc/>
        private void EncodeJson(IEncoder encoder) {

            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.NodeId) != 0) {
                encoder.WriteExpandedNodeId(nameof(MonitoredItemMessageContentMask.NodeId), NodeId);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.EndpointUrl) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.EndpointUrl), EndpointUrl);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ApplicationUri) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.ApplicationUri), ApplicationUri);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.DisplayName) != 0 &&
                !string.IsNullOrEmpty(DisplayName)) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.DisplayName), DisplayName);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Timestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemMessageContentMask.Timestamp), Timestamp);
            }
            //  add the status as a string for backwards compatibility
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Status) != 0 && Value != null) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.Status),
                    StatusCode.LookupSymbolicId(Value.StatusCode.Code));
            }
            encoder.WriteDataValue("Value", Value);
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SequenceNumber) != 0) {
                encoder.WriteUInt32(nameof(MonitoredItemMessageContentMask.SequenceNumber), SequenceNumber);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ExtensionFields) != 0) {
                if (ExtensionFields != null) {
                    var jsonEncoder = encoder as JsonEncoderEx;
                    jsonEncoder.WriteStringDictionary(nameof(ExtensionFields), ExtensionFields);
                }
            }
        }
    }
}