// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Message mask extensions
    /// </summary>
    internal static class MonitoredItemMessageFlagsEx {

        /// <summary>
        /// Get message content mask
        /// </summary>
        /// <returns></returns>
        public static uint ToMonitoredItemMessageMask(this DataSetContentMask? message,
            DataSetFieldContentMask? field) {
            MonitoredItemMessageContentMask result = 0;
            if (field == null) {
                result |=
                    MonitoredItemMessageContentMask.SourceTimestamp |
                    MonitoredItemMessageContentMask.ServerTimestamp |
                    MonitoredItemMessageContentMask.StatusCode |
                    MonitoredItemMessageContentMask.NodeId |
                    MonitoredItemMessageContentMask.EndpointUrl |
                    MonitoredItemMessageContentMask.ApplicationUri |
                    MonitoredItemMessageContentMask.DisplayName |
                    MonitoredItemMessageContentMask.ExtensionFields |
                    MonitoredItemMessageContentMask.SequenceNumber;
            }
            else {
                if (0 != (field & DataSetFieldContentMask.SourceTimestamp)) {
                    result |= MonitoredItemMessageContentMask.SourceTimestamp;
                }
                if (0 != (field & DataSetFieldContentMask.SourcePicoSeconds)) {
                    result |= MonitoredItemMessageContentMask.SourcePicoSeconds;
                }
                if (0 != (field & DataSetFieldContentMask.ServerTimestamp)) {
                    result |= MonitoredItemMessageContentMask.ServerTimestamp;
                }
                if (0 != (field & DataSetFieldContentMask.ServerPicoSeconds)) {
                    result |= MonitoredItemMessageContentMask.ServerPicoSeconds;
                }
                if (0 != (field & DataSetFieldContentMask.StatusCode)) {
                    result |= MonitoredItemMessageContentMask.StatusCode;
                }
                if (0 != (field & DataSetFieldContentMask.NodeId)) {
                    result |= MonitoredItemMessageContentMask.NodeId;
                }
                if (0 != (field & DataSetFieldContentMask.EndpointUrl)) {
                    result |= MonitoredItemMessageContentMask.EndpointUrl;
                }
                if (0 != (field & DataSetFieldContentMask.ApplicationUri)) {
                    result |= MonitoredItemMessageContentMask.ApplicationUri;
                }
                if (0 != (field & DataSetFieldContentMask.DisplayName)) {
                    result |= MonitoredItemMessageContentMask.DisplayName;
                }
                if (0 != (field & DataSetFieldContentMask.ExtensionFields)) {
                    result |= MonitoredItemMessageContentMask.ExtensionFields;
                }
            }
            if (message == null) {
                result |=
                    MonitoredItemMessageContentMask.Timestamp |
                    MonitoredItemMessageContentMask.Status;
            }
            else {
                if (0 != (message & DataSetContentMask.Timestamp)) {
                    result |= MonitoredItemMessageContentMask.Timestamp;
                }
                if (0 != (message & DataSetContentMask.PicoSeconds)) {
                    result |= MonitoredItemMessageContentMask.PicoSeconds;
                }
                if (0 != (message & DataSetContentMask.Status)) {
                    result |= MonitoredItemMessageContentMask.Status;
                }
                if (0 != (message & DataSetContentMask.SequenceNumber)) {
                    result |= MonitoredItemMessageContentMask.SequenceNumber;
                }
                if (0 != (message & DataSetContentMask.MessageType)) {
                    result |= MonitoredItemMessageContentMask.MessageType;
                }
                if (0 != (message & DataSetContentMask.ReversibleFieldEncoding)) {
                    result |= MonitoredItemMessageContentMask.ReversibleFieldEncoding;
                }
            }
            return (uint)result;
        }
    }
}