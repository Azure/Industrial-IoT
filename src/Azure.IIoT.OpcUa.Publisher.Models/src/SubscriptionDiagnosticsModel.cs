// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Subscription diagnostics
    /// </summary>
    [DataContract]
    public record class SubscriptionDiagnosticsModel
    {
        /// <summary>
        /// Subscription id
        /// </summary>
        [DataMember(Name = "subscriptionId", Order = 0,
            EmitDefaultValue = false)]
        public uint SubscriptionId { get; init; }

        /// <summary>
        /// Subscription priority
        /// </summary>
        [DataMember(Name = "priority", Order = 1,
            EmitDefaultValue = false)]
        public byte Priority { get; init; }

        /// <summary>
        /// Publishing interval
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 2,
            EmitDefaultValue = false)]
        public double PublishingInterval { get; init; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        [DataMember(Name = "maxKeepAliveCount", Order = 3,
            EmitDefaultValue = false)]
        public uint MaxKeepAliveCount { get; init; }

        /// <summary>
        /// Max lifetime count
        /// </summary>
        [DataMember(Name = "maxLifetimeCount", Order = 4,
            EmitDefaultValue = false)]
        public uint MaxLifetimeCount { get; init; }

        /// <summary>
        /// Current keep alive count
        /// </summary>
        [DataMember(Name = "currentKeepAliveCount", Order = 5,
            EmitDefaultValue = false)]
        public uint CurrentKeepAliveCount { get; init; }

        /// <summary>
        /// Current lifetime count
        /// </summary>
        [DataMember(Name = "currentLifetimeCount", Order = 6,
            EmitDefaultValue = false)]
        public uint CurrentLifetimeCount { get; init; }

        /// <summary>
        /// Max notifications per publish
        /// </summary>
        [DataMember(Name = "maxNotificationsPerPublish", Order = 7,
            EmitDefaultValue = false)]
        public uint MaxNotificationsPerPublish { get; init; }

        /// <summary>
        /// Publishing enabled
        /// </summary>
        [DataMember(Name = "publishingEnabled", Order = 8,
            EmitDefaultValue = false)]
        public bool PublishingEnabled { get; init; }

        /// <summary>
        /// Modify count
        /// </summary>
        [DataMember(Name = "modifyCount", Order = 9,
            EmitDefaultValue = false)]
        public uint ModifyCount { get; init; }

        /// <summary>
        /// Subscription enable count
        /// </summary>
        [DataMember(Name = "enableCount", Order = 10,
            EmitDefaultValue = false)]
        public uint EnableCount { get; init; }

        /// <summary>
        /// Disable count
        /// </summary>
        [DataMember(Name = "disableCount", Order = 11,
            EmitDefaultValue = false)]
        public uint DisableCount { get; init; }

        /// <summary>
        /// Monitored item count
        /// </summary>
        [DataMember(Name = "monitoredItemCount", Order = 12,
            EmitDefaultValue = false)]
        public uint MonitoredItemCount { get; init; }

        /// <summary>
        /// Disabled monitored item count
        /// </summary>
        [DataMember(Name = "disabledMonitoredItemCount", Order = 13,
            EmitDefaultValue = false)]
        public uint DisabledMonitoredItemCount { get; init; }

        /// <summary>
        /// Publish request count
        /// </summary>
        [DataMember(Name = "publishRequestCount", Order = 14,
            EmitDefaultValue = false)]
        public uint PublishRequestCount { get; init; }

        /// <summary>
        /// Late publish request count
        /// </summary>
        [DataMember(Name = "latePublishRequestCount", Order = 15,
            EmitDefaultValue = false)]
        public uint LatePublishRequestCount { get; init; }

        /// <summary>
        /// Data change notifications count
        /// </summary>
        [DataMember(Name = "dataChangeNotificationsCount", Order = 16,
            EmitDefaultValue = false)]
        public uint DataChangeNotificationsCount { get; init; }

        /// <summary>
        /// Event notifications count
        /// </summary>
        [DataMember(Name = "eventNotificationsCount", Order = 17,
            EmitDefaultValue = false)]
        public uint EventNotificationsCount { get; init; }

        /// <summary>
        /// Total Notifications count
        /// </summary>
        [DataMember(Name = "notificationsCount", Order = 18,
            EmitDefaultValue = false)]
        public uint NotificationsCount { get; init; }

        /// <summary>
        /// Unacknowledged message count
        /// </summary>
        [DataMember(Name = "unacknowledgedMessageCount", Order = 19,
            EmitDefaultValue = false)]
        public uint UnacknowledgedMessageCount { get; init; }

        /// <summary>
        /// Discarded message count
        /// </summary>
        [DataMember(Name = "discardedMessageCount", Order = 20,
            EmitDefaultValue = false)]
        public uint DiscardedMessageCount { get; init; }

        /// <summary>
        /// Next sequence number
        /// </summary>
        [DataMember(Name = "nextSequenceNumber", Order = 21,
            EmitDefaultValue = false)]
        public uint NextSequenceNumber { get; init; }

        /// <summary>
        /// Monitoring queue overflow count
        /// </summary>
        [DataMember(Name = "monitoringQueueOverflowCount", Order = 22,
            EmitDefaultValue = false)]
        public uint MonitoringQueueOverflowCount { get; init; }

        /// <summary>
        /// Event queue overflow count
        /// </summary>
        [DataMember(Name = "eventQueueOverFlowCount", Order = 23,
            EmitDefaultValue = false)]
        public uint EventQueueOverFlowCount { get; init; }

        /// <summary>
        /// Transfer request count
        /// </summary>
        [DataMember(Name = "transferRequestCount", Order = 24,
            EmitDefaultValue = false)]
        public uint TransferRequestCount { get; init; }

        /// <summary>
        /// Transferred to alt client count
        /// </summary>
        [DataMember(Name = "transferredToAltClientCount", Order = 25,
            EmitDefaultValue = false)]
        public uint TransferredToAltClientCount { get; init; }

        /// <summary>
        /// Transferred to same client count
        /// </summary>
        [DataMember(Name = "transferredToSameClientCount", Order = 26,
            EmitDefaultValue = false)]
        public uint TransferredToSameClientCount { get; init; }

        /// <summary>
        /// Publish request count
        /// </summary>
        [DataMember(Name = "republishRequestCount", Order = 27,
            EmitDefaultValue = false)]
        public uint RepublishRequestCount { get; init; }

        /// <summary>
        /// Republish message request count
        /// </summary>
        [DataMember(Name = "republishMessageRequestCount", Order = 28,
            EmitDefaultValue = false)]
        public uint RepublishMessageRequestCount { get; init; }

        /// <summary>
        /// Republish message count
        /// </summary>
        [DataMember(Name = "republishMessageCount", Order = 29,
            EmitDefaultValue = false)]
        public uint RepublishMessageCount { get; init; }
    }
}
