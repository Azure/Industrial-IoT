// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Collections.Generic;
    using Opc.Ua;

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
        /// Subscription Id
        /// </summary>
        public string SubscriptionId { get; set; }

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
        /// Data value for variable change notification
        /// </summary>
        public DataValue Value { get; set; }

        /// <summary>
        /// Extra fields
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
            MessageContentMask = decoder.ReadUInt32("ContentMask");
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.NodeId) != 0) {
                NodeId = decoder.ReadExpandedNodeId(nameof(MonitoredItemMessageContentMask.NodeId));
            }
            Value = new DataValue();
            
            // todo check why Value is not encoded as DataValue type
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ServerTimestamp) != 0) {
                Value.ServerTimestamp = decoder.ReadDateTime(nameof(MonitoredItemMessageContentMask.ServerTimestamp));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ServerPicoSeconds) != 0) {
                Value.ServerPicoseconds = decoder.ReadUInt16(nameof(MonitoredItemMessageContentMask.ServerPicoSeconds));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SourceTimestamp) != 0) {
                Value.SourceTimestamp = decoder.ReadDateTime(nameof(MonitoredItemMessageContentMask.SourceTimestamp));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SourcePicoSeconds) != 0) {
                Value.SourcePicoseconds = decoder.ReadUInt16(nameof(MonitoredItemMessageContentMask.SourcePicoSeconds));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.StatusCode) != 0) {
                Value.StatusCode = decoder.ReadStatusCode(nameof(MonitoredItemMessageContentMask.StatusCode));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Status) != 0) {
                var status = decoder.ReadString(nameof(MonitoredItemMessageContentMask.Status));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.EndpointUrl) != 0) {
                EndpointUrl = decoder.ReadString(nameof(MonitoredItemMessageContentMask.EndpointUrl));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SubscriptionId) != 0) {
                SubscriptionId = decoder.ReadString(nameof(MonitoredItemMessageContentMask.SubscriptionId));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ApplicationUri) != 0) {
                ApplicationUri = decoder.ReadString(nameof(MonitoredItemMessageContentMask.ApplicationUri));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.DisplayName) != 0) {
                DisplayName = decoder.ReadString(nameof(MonitoredItemMessageContentMask.DisplayName));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Timestamp) != 0) {
                var timestamp = decoder.ReadDateTime(nameof(MonitoredItemMessageContentMask.Timestamp));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.PicoSeconds) != 0) {
                var picoseconds = decoder.ReadUInt16(nameof(MonitoredItemMessageContentMask.PicoSeconds));
            }

            Value.WrappedValue = decoder.ReadVariant("Value");

            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ExtraFields) != 0) {
                var dictionary = (KeyValuePairCollection)decoder.ReadEncodeableArray("ExtensionFields", typeof(Ua.KeyValuePair));
                ExtensionFields = new Dictionary<string, string>(dictionary.Count);
                foreach (var item in dictionary) {
                    ExtensionFields[item.Key.Name] = item.Value.ToString();
                }
            }
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            encoder.WriteUInt32("ContentMask", MessageContentMask);
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.NodeId) != 0) {
                encoder.WriteExpandedNodeId(nameof(MonitoredItemMessageContentMask.NodeId), NodeId);
            }
            // todo check why Value is not encoded as DataValue type
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ServerTimestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemMessageContentMask.ServerTimestamp),
                    Value?.ServerTimestamp ?? DateTime.MinValue);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ServerPicoSeconds) != 0) {
                encoder.WriteUInt16(nameof(MonitoredItemMessageContentMask.ServerPicoSeconds),
                    Value?.ServerPicoseconds ?? 0);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SourceTimestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemMessageContentMask.SourceTimestamp),
                    Value?.SourceTimestamp ?? DateTime.MinValue);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SourcePicoSeconds) != 0) {
                encoder.WriteUInt16(nameof(MonitoredItemMessageContentMask.SourcePicoSeconds),
                    Value?.SourcePicoseconds ?? 0);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.StatusCode) != 0) {
                encoder.WriteStatusCode(nameof(MonitoredItemMessageContentMask.StatusCode),
                    Value?.StatusCode ?? StatusCodes.BadNoData);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Status) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.Status),
                    Value == null ? "" : StatusCode.LookupSymbolicId(Value.StatusCode.Code));
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.EndpointUrl) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.EndpointUrl), EndpointUrl);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.SubscriptionId) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.SubscriptionId), SubscriptionId);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ApplicationUri) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.ApplicationUri), ApplicationUri);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.DisplayName) != 0) {
                encoder.WriteString(nameof(MonitoredItemMessageContentMask.DisplayName), DisplayName);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.Timestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemMessageContentMask.Timestamp), DateTime.UtcNow);
            }
            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.PicoSeconds) != 0) {
                encoder.WriteUInt16(nameof(MonitoredItemMessageContentMask.PicoSeconds), 0);
            }

            if (Value?.WrappedValue != null) {
                encoder.WriteVariant("Value", Value.WrappedValue);
            }
            else {
                encoder.WriteVariant("Value", Variant.Null);
            }

            if ((MessageContentMask & (uint)MonitoredItemMessageContentMask.ExtraFields) != 0) {
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
        public bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }
            if (!(encodeable is MonitoredItemMessage wrapper)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.MessageContentMask, MessageContentMask) ||
                !Utils.IsEqual(wrapper.NodeId, NodeId) ||
                !Utils.IsEqual(wrapper.SubscriptionId, SubscriptionId) ||
                !Utils.IsEqual(wrapper.EndpointUrl, EndpointUrl) ||
                !Utils.IsEqual(wrapper.ApplicationUri, ApplicationUri) ||
                !Utils.IsEqual(wrapper.DisplayName, DisplayName) ||
                !Utils.IsEqual(wrapper.Value, Value) ||
                !Utils.IsEqual(wrapper.BinaryEncodingId, BinaryEncodingId) ||
                !Utils.IsEqual(wrapper.TypeId, TypeId) ||
                !Utils.IsEqual(wrapper.XmlEncodingId, XmlEncodingId)) {
                return false;
            }
            return true;
        }
    }
}