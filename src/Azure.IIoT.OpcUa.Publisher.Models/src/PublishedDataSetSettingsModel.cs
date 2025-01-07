// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Published dataset settings - corresponds to SubscriptionModel
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetSettingsModel
    {
        /// <summary>
        /// Publishing interval
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 0,
            EmitDefaultValue = false)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Life time
        /// </summary>
        [DataMember(Name = "lifeTimeCount", Order = 1,
            EmitDefaultValue = false)]
        public uint? LifeTimeCount { get; set; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        [DataMember(Name = "maxKeepAliveCount", Order = 2,
            EmitDefaultValue = false)]
        public uint? MaxKeepAliveCount { get; set; }

        /// <summary>
        /// Max notifications per publish
        /// </summary>
        [DataMember(Name = "maxNotificationsPerPublish", Order = 3,
            EmitDefaultValue = false)]
        public uint? MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        [DataMember(Name = "priority", Order = 4,
            EmitDefaultValue = false)]
        public byte? Priority { get; set; }

        /// <summary>
        /// Triggers automatic monitored items display name discovery
        /// </summary>
        [DataMember(Name = "resolveDisplayName", Order = 5,
            EmitDefaultValue = false)]
        public bool? ResolveDisplayName { get; set; }

        /// <summary>
        /// Use deferred acknoledgements
        /// </summary>
        [DataMember(Name = "useDeferredAcknoledgements", Order = 6,
            EmitDefaultValue = false)]
        public bool? UseDeferredAcknoledgements { get; set; }

        /// <summary>
        /// Will set the subscription to have publishing
        /// enabled and every monitored item created to be
        /// in desired monitoring mode.
        /// </summary>
        [DataMember(Name = "enableImmediatePublishing", Order = 8,
            EmitDefaultValue = false)]
        public bool? EnableImmediatePublishing { get; set; }

        /// <summary>
        /// Enable sequential publishing feature in the stack.
        /// </summary>
        [DataMember(Name = "enableSequentialPublishing", Order = 9,
            EmitDefaultValue = false)]
        public bool? EnableSequentialPublishing { get; set; }

        /// <summary>
        /// Republish after transferring the subscription during
        /// reconnect handling.
        /// </summary>
        [DataMember(Name = "republishAfterTransfer", Order = 10,
            EmitDefaultValue = false)]
        public bool? RepublishAfterTransfer { get; set; }

        /// <summary>
        /// Subscription watchdog behavior
        /// </summary>
        [DataMember(Name = "watchdogBehavior", Order = 11,
            EmitDefaultValue = false)]
        public SubscriptionWatchdogBehavior? WatchdogBehavior { get; set; }

        /// <summary>
        /// Monitored item watchdog timeout
        /// </summary>
        [DataMember(Name = "monitoredItemWatchdogTimeout", Order = 13,
            EmitDefaultValue = false)]
        public TimeSpan? MonitoredItemWatchdogTimeout { get; set; }

        /// <summary>
        /// Monitored item watchdog timeout
        /// </summary>
        [DataMember(Name = "monitoredItemWatchdogCondition", Order = 14,
            EmitDefaultValue = false)]
        public MonitoredItemWatchdogCondition? MonitoredItemWatchdogCondition { get; set; }

        /// <summary>
        /// Default sampling interval
        /// </summary>
        [DataMember(Name = "defaultSamplingInterval", Order = 15,
            EmitDefaultValue = false)]
        public TimeSpan? DefaultSamplingInterval { get; set; }

        /// <summary>
        /// Default heartbeat interval
        /// </summary>
        [DataMember(Name = "DefaultHeartbeatInterval", Order = 16,
            EmitDefaultValue = false)]
        public TimeSpan? DefaultHeartbeatInterval { get; set; }

        /// <summary>
        /// The default behavior of heartbeat
        /// </summary>
        [DataMember(Name = "DefaultHeartbeatBehavior", Order = 17,
            EmitDefaultValue = false)]
        public HeartbeatBehavior? DefaultHeartbeatBehavior { get; set; }
    }
}
