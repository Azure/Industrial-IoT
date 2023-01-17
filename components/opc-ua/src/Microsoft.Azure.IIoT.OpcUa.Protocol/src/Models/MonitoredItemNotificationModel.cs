// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Opc.Ua;

    /// <summary>
    /// Monitored item notification
    /// </summary>
    public class MonitoredItemNotificationModel {

        /// <summary>
        /// Identifier of the monitored item that originated the message
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Identifier to relate notifications to a value
        /// </summary>
        public uint MessageId => SequenceNumber ?? (uint)GetHashCode();

        /// <summary>
        /// Data set field identifier as configured
        /// </summary>
        public string DataSetFieldName { get; set; }

        /// <summary>
        /// Node Id in string format as configured
        /// </summary>
        public string NodeId { get; internal set; }

        /// <summary>
        /// Attribute monitored
        /// </summary>
        public uint AttributeId { get; internal set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; internal set; }

        /// <summary>
        /// Sequence number
        /// </summary>
        public uint? SequenceNumber { get; set; }

        /// <summary>
        /// Value of variable change notification
        /// </summary>
        public DataValue Value { get; set; }

        /// <summary>
        /// Is heartbeat?
        /// </summary>
        public bool IsHeartbeat { get; set; }
    }
}