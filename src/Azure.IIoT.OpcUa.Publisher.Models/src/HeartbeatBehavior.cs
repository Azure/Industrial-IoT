// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Heartbeat behavior
    /// </summary>
    [DataContract]
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
        [EnumMember(Value = "ContinuousLKV")]
        ContinuousLKV = 0x2,

        /// <summary>
        /// Continuously sends last good value
        /// </summary>
        [EnumMember(Value = "ContinuousLKG")]
        ContinuousLKG = 0x3,

        /// <summary>
        /// Update value timestamps to be different
        /// </summary>
        [EnumMember(Value = "WatchdogLKVWithUpdatedTimestamps")]
        WatchdogLKVWithUpdatedTimestamps = 0x4

        // Others can be combining Cont, LKG with 0x4
    }
}
