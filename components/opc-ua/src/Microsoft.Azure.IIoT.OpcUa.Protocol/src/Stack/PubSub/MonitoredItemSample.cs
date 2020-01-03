// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encodeable monitored item message
    /// </summary>
    public class MonitoredItemSample : IEncodeable {

        /// <summary>
        /// Content mask
        /// </summary>
        public uint MessageContentMask { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        public NodeId NodeId { get; set; }

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
        /// Data value
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
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.NodeId) != 0) {
                encoder.WriteNodeId(nameof(MonitoredItemSampleContentMask.NodeId), NodeId);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.ServerTimestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemSampleContentMask.ServerTimestamp), Value.ServerTimestamp);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.ServerPicoSeconds) != 0) {
                encoder.WriteUInt16(nameof(MonitoredItemSampleContentMask.ServerPicoSeconds), Value.ServerPicoseconds);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.SourceTimestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemSampleContentMask.SourceTimestamp), Value.SourceTimestamp);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.SourcePicoSeconds) != 0) {
                encoder.WriteUInt16(nameof(MonitoredItemSampleContentMask.SourcePicoSeconds), Value.SourcePicoseconds);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.StatusCode) != 0) {
                encoder.WriteStatusCode(nameof(MonitoredItemSampleContentMask.StatusCode), Value.StatusCode);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.Status) != 0) {
                encoder.WriteString(nameof(MonitoredItemSampleContentMask.Status), StatusCode.LookupSymbolicId(Value.StatusCode.Code));
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.EndpointUrl) != 0) {
                encoder.WriteString(nameof(MonitoredItemSampleContentMask.EndpointUrl), EndpointUrl);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.SubscriptionId) != 0) {
                encoder.WriteString(nameof(MonitoredItemSampleContentMask.SubscriptionId), SubscriptionId);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.ApplicationUri) != 0) {
                encoder.WriteString(nameof(MonitoredItemSampleContentMask.ApplicationUri), ApplicationUri);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.DisplayName) != 0) {
                encoder.WriteString(nameof(MonitoredItemSampleContentMask.DisplayName), DisplayName);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.Timestamp) != 0) {
                encoder.WriteDateTime(nameof(MonitoredItemSampleContentMask.Timestamp), DateTime.UtcNow);
            }
            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.PicoSeconds) != 0) {
                encoder.WriteUInt16(nameof(MonitoredItemSampleContentMask.PicoSeconds), 0);
            }

            encoder.WriteVariant("Value", Value.WrappedValue);

            if ((MessageContentMask & (uint)MonitoredItemSampleContentMask.ExtraFields) != 0) {
                if (ExtensionFields != null) {
                    foreach (var field in ExtensionFields) {
                        encoder.WriteString(field.Key, field.Value);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }

            return false;
        }
    }
}