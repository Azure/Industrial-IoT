// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Managed client runtime diagnostics included in session diagnostic dumps.
    /// </summary>
    [DataContract]
    public sealed record class ManagedSessionDiagnosticsModel
    {
        /// <summary>
        /// Current connectivity state.
        /// </summary>
        [DataMember(Name = "state", Order = 0)]
        public EndpointConnectivityState State { get; init; }

        /// <summary>
        /// Successful connection count.
        /// </summary>
        [DataMember(Name = "connectCount", Order = 1,
            EmitDefaultValue = false)]
        public int ConnectCount { get; init; }

        /// <summary>
        /// Reconnect attempt count.
        /// </summary>
        [DataMember(Name = "reconnectCount", Order = 2,
            EmitDefaultValue = false)]
        public int ReconnectCount { get; init; }

        /// <summary>
        /// Whether a reconnect is in progress.
        /// </summary>
        [DataMember(Name = "reconnectTriggered", Order = 3,
            EmitDefaultValue = false)]
        public bool ReconnectTriggered { get; init; }

        /// <summary>
        /// Active publish worker count.
        /// </summary>
        [DataMember(Name = "publishWorkerCount", Order = 4,
            EmitDefaultValue = false)]
        public int PublishWorkerCount { get; init; }

        /// <summary>
        /// Good publish request count.
        /// </summary>
        [DataMember(Name = "goodPublishRequestCount", Order = 5,
            EmitDefaultValue = false)]
        public int GoodPublishRequestCount { get; init; }

        /// <summary>
        /// Bad publish request count.
        /// </summary>
        [DataMember(Name = "badPublishRequestCount", Order = 6,
            EmitDefaultValue = false)]
        public int BadPublishRequestCount { get; init; }

        /// <summary>
        /// Outstanding request count.
        /// </summary>
        [DataMember(Name = "outstandingRequestCount", Order = 7,
            EmitDefaultValue = false)]
        public int OutstandingRequestCount { get; init; }

        /// <summary>
        /// Configured minimum publish request count.
        /// </summary>
        [DataMember(Name = "minimumPublishRequestCount", Order = 8,
            EmitDefaultValue = false)]
        public int MinimumPublishRequestCount { get; init; }

        /// <summary>
        /// Successful keep alives since the last failure.
        /// </summary>
        [DataMember(Name = "keepAliveCounter", Order = 9,
            EmitDefaultValue = false)]
        public int KeepAliveCounter { get; init; }

        /// <summary>
        /// Total keep alive count.
        /// </summary>
        [DataMember(Name = "keepAliveTotal", Order = 10,
            EmitDefaultValue = false)]
        public int KeepAliveTotal { get; init; }

        /// <summary>
        /// Whether a complex type system is loaded.
        /// </summary>
        [DataMember(Name = "complexTypeSystemLoaded", Order = 11,
            EmitDefaultValue = false)]
        public bool ComplexTypeSystemLoaded { get; init; }

        /// <summary>
        /// Whether the complex type system loaded completely.
        /// </summary>
        [DataMember(Name = "complexTypeSystemFullyLoaded", Order = 12,
            EmitDefaultValue = false)]
        public bool ComplexTypeSystemFullyLoaded { get; init; }

        /// <summary>
        /// Latest managed background errors.
        /// </summary>
        [DataMember(Name = "backgroundErrors", Order = 13,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? BackgroundErrors { get; init; }

        /// <summary>
        /// Managed logical subscription diagnostics.
        /// </summary>
        [DataMember(Name = "subscriptions", Order = 14,
            EmitDefaultValue = false)]
        public IReadOnlyList<ManagedSubscriptionDiagnosticsModel>? Subscriptions { get; init; }
    }

    /// <summary>
    /// Managed logical subscription diagnostics included in diagnostic dumps.
    /// </summary>
    [DataContract]
    public sealed record class ManagedSubscriptionDiagnosticsModel
    {
        /// <summary>
        /// Server subscription identifiers for all partitions.
        /// </summary>
        [DataMember(Name = "subscriptionIds", Order = 0,
            EmitDefaultValue = false)]
        public IReadOnlyList<uint>? SubscriptionIds { get; init; }

        /// <summary>
        /// Publisher registration count.
        /// </summary>
        [DataMember(Name = "registrationCount", Order = 1,
            EmitDefaultValue = false)]
        public int RegistrationCount { get; init; }

        /// <summary>
        /// Server-side partition count.
        /// </summary>
        [DataMember(Name = "partitionCount", Order = 2,
            EmitDefaultValue = false)]
        public int PartitionCount { get; init; }

        /// <summary>
        /// Total monitored item count.
        /// </summary>
        [DataMember(Name = "monitoredItems", Order = 3,
            EmitDefaultValue = false)]
        public int MonitoredItems { get; init; }

        /// <summary>
        /// Applied monitored item count.
        /// </summary>
        [DataMember(Name = "appliedMonitoredItems", Order = 4,
            EmitDefaultValue = false)]
        public int AppliedMonitoredItems { get; init; }

        /// <summary>
        /// Pending monitored item count.
        /// </summary>
        [DataMember(Name = "pendingMonitoredItems", Order = 5,
            EmitDefaultValue = false)]
        public int PendingMonitoredItems { get; init; }

        /// <summary>
        /// Retrying monitored item count.
        /// </summary>
        [DataMember(Name = "retryingMonitoredItems", Order = 6,
            EmitDefaultValue = false)]
        public int RetryingMonitoredItems { get; init; }

        /// <summary>
        /// Terminal monitored item count.
        /// </summary>
        [DataMember(Name = "terminalMonitoredItems", Order = 7,
            EmitDefaultValue = false)]
        public int TerminalMonitoredItems { get; init; }

        /// <summary>
        /// Cyclic-read monitored item count.
        /// </summary>
        [DataMember(Name = "cyclicMonitoredItems", Order = 8,
            EmitDefaultValue = false)]
        public int CyclicMonitoredItems { get; init; }

        /// <summary>
        /// Cyclic-read worker count.
        /// </summary>
        [DataMember(Name = "cyclicWorkerCount", Order = 9,
            EmitDefaultValue = false)]
        public int CyclicWorkerCount { get; init; }

        /// <summary>
        /// Tracked retry count.
        /// </summary>
        [DataMember(Name = "retryCount", Order = 10,
            EmitDefaultValue = false)]
        public int RetryCount { get; init; }

        /// <summary>
        /// Heartbeat-enabled monitored item count.
        /// </summary>
        [DataMember(Name = "heartbeatsEnabled", Order = 11,
            EmitDefaultValue = false)]
        public int HeartbeatsEnabled { get; init; }

        /// <summary>
        /// Condition-enabled monitored item count.
        /// </summary>
        [DataMember(Name = "conditionsEnabled", Order = 12,
            EmitDefaultValue = false)]
        public int ConditionsEnabled { get; init; }

        /// <summary>
        /// Late monitored item count.
        /// </summary>
        [DataMember(Name = "lateMonitoredItems", Order = 13,
            EmitDefaultValue = false)]
        public int LateMonitoredItems { get; init; }

        /// <summary>
        /// Whether publishing is enabled.
        /// </summary>
        [DataMember(Name = "publishingEnabled", Order = 14,
            EmitDefaultValue = false)]
        public bool PublishingEnabled { get; init; }

        /// <summary>
        /// Whether the watchdog is enabled.
        /// </summary>
        [DataMember(Name = "watchdogEnabled", Order = 15,
            EmitDefaultValue = false)]
        public bool WatchdogEnabled { get; init; }

        /// <summary>
        /// Whether a watchdog reset is in progress.
        /// </summary>
        [DataMember(Name = "watchdogResetInProgress", Order = 16,
            EmitDefaultValue = false)]
        public bool WatchdogResetInProgress { get; init; }

        /// <summary>
        /// Latest managed subscription background errors.
        /// </summary>
        [DataMember(Name = "backgroundErrors", Order = 17,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? BackgroundErrors { get; init; }
    }
}
