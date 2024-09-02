// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Heartbeat behavior
    /// </summary>
    [DataContract]
    [Flags]
    public enum HeartbeatBehavior
    {
        /// <summary>
        /// Watchdog with Last known value
        /// </summary>
        [EnumMember(Value = "WatchdogLKV")]
        WatchdogLKV = 0x0,

        /// <summary>
        /// Watchdog with last good value
        /// </summary>
        [EnumMember(Value = "WatchdogLKG")]
        WatchdogLKG = 0x1,

        /// <summary>
        /// Continuously sends last known value
        /// </summary>
        [EnumMember(Value = "PeriodicLKV")]
        PeriodicLKV = 0x2,

        /// <summary>
        /// Continuously sends last good value
        /// </summary>
        [EnumMember(Value = "PeriodicLKG")]
        PeriodicLKG = WatchdogLKG | PeriodicLKV,

        /// <summary>
        /// Update value timestamps to be different
        /// </summary>
        [EnumMember(Value = "WatchdogLKVWithUpdatedTimestamps")]
        WatchdogLKVWithUpdatedTimestamps = 0x4,

        // Others can be combining Cont, LKG with 0x4

        /// <summary>
        /// Does not send heartbeat but counts it in
        /// diagnostics
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
        /// Continuously sends last known value but not
        /// the received values.
        /// </summary>
        [EnumMember(Value = "PeriodicLKVDropValue")]
        PeriodicLKVDropValue = PeriodicLKV | Reserved,

        /// <summary>
        /// Continuously sends last good value but not
        /// the received values.
        /// </summary>
        [EnumMember(Value = "PeriodicLKGDropValue")]
        PeriodicLKGDropValue = PeriodicLKG | Reserved,
    }
}
