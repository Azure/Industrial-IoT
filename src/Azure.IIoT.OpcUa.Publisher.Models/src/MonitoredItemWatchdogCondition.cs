// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Monitored item watchdog condition
    /// </summary>
    [DataContract]
    [Flags]
    public enum MonitoredItemWatchdogCondition
    {
        /// <summary>
        /// Perform subscription action when all
        /// monitored items are late
        /// </summary>
        [EnumMember(Value = "WhenAllAreLate")]
        WhenAllAreLate,

        /// <summary>
        /// Perform subscription action when any
        /// monitored item is late
        /// </summary>
        [EnumMember(Value = "WhenAnyIsLate")]
        WhenAnyIsLate
    }
}
