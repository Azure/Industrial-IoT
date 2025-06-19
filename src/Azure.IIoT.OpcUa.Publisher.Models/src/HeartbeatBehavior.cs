// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Controls how heartbeat messages are handled for monitored items.
    /// Heartbeats help maintain awareness of node state and connection health
    /// even when values don't change. Can be configured globally via the
    /// --hbb command line option. Works with heartbeat interval settings.
    /// </summary>
    [DataContract]
    [Flags]
    public enum HeartbeatBehavior
    {
        /// <summary>
        /// Default behavior that sends last known value when heartbeat triggers.
        /// Reports exact state regardless of value quality or status.
        /// Most straightforward option for basic monitoring.
        /// Value may be bad or uncertain quality.
        /// </summary>
        [EnumMember(Value = "WatchdogLKV")]
        WatchdogLKV = 0x0,

        /// <summary>
        /// Sends only good quality values when heartbeat triggers.
        /// Ensures reported values meet quality requirements.
        /// May not reflect actual current state if last good value is old.
        /// Use when data quality is more important than immediacy.
        /// </summary>
        [EnumMember(Value = "WatchdogLKG")]
        WatchdogLKG = 0x1,

        /// <summary>
        /// Periodically publishes last known value at heartbeat interval.
        /// Provides regular state updates regardless of value changes.
        /// Useful for monitoring systems that expect periodic updates.
        /// Higher bandwidth usage due to regular messages.
        /// </summary>
        [EnumMember(Value = "PeriodicLKV")]
        PeriodicLKV = 0x2,

        /// <summary>
        /// Periodically publishes last good quality value at heartbeat interval.
        /// Combines periodic updates with quality filtering.
        /// Ensures regular, quality-checked status updates.
        /// Best choice for reliable system state monitoring.
        /// </summary>
        [EnumMember(Value = "PeriodicLKG")]
        PeriodicLKG = WatchdogLKG | PeriodicLKV,

        /// <summary>
        /// Like WatchdogLKV but updates timestamps on each heartbeat.
        /// Helps distinguish between multiple heartbeat messages.
        /// Useful when downstream systems track value freshness.
        /// Can be combined with other behaviors using flags.
        /// </summary>
        [EnumMember(Value = "WatchdogLKVWithUpdatedTimestamps")]
        WatchdogLKVWithUpdatedTimestamps = 0x4,

        // Others can be combining Cont, LKG with 0x4

        /// <summary>
        /// Records heartbeat events in diagnostics without sending messages.
        /// Allows monitoring heartbeat behavior without generating traffic.
        /// Useful for testing and troubleshooting configurations.
        /// Can be combined with other behaviors using flags.
        /// </summary>
        [EnumMember(Value = "WatchdogLKVDiagnosticsOnly")]
        WatchdogLKVDiagnosticsOnly = 0x8,

        // Others can be combining Cont, LKG with 0x8

        /// <summary>
        /// Reserved, do not use
        /// </summary>
#pragma warning disable CA1700 // Do not name enum values 'Reserved'
        Reserved = 0x10,
#pragma warning restore CA1700 // Do not name enum values 'Reserved'

        /// <summary>
        /// Only sends periodic heartbeat messages with last known value.
        /// Filters out regular value change notifications.
        /// Reduces message volume in fast-changing nodes.
        /// Use when only periodic snapshots are needed.
        /// </summary>
        [EnumMember(Value = "PeriodicLKVDropValue")]
        PeriodicLKVDropValue = PeriodicLKV | Reserved,

        /// <summary>
        /// Only sends periodic heartbeat messages with last good value.
        /// Combines periodic-only updates with quality filtering.
        /// Most restrictive option for message generation.
        /// Best for reducing traffic while ensuring quality.
        /// </summary>
        [EnumMember(Value = "PeriodicLKGDropValue")]
        PeriodicLKGDropValue = PeriodicLKG | Reserved,
    }
}
