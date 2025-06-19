// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines the conditions that trigger the subscription watchdog behavior.
    /// Works in conjunction with OpcNodeWatchdogTimespan to determine when nodes
    /// are considered "late" and DataSetWriterWatchdogBehavior to define the response.
    /// Can be configured globally via the --mwc command line option.
    /// </summary>
    [DataContract]
    [Flags]
    public enum MonitoredItemWatchdogCondition
    {
        /// <summary>
        /// Triggers watchdog behavior only when all monitored items exceed their timeout.
        /// Most lenient condition that waits for complete communication loss.
        /// Reduces false positives when some nodes update less frequently.
        /// Suitable when all nodes must be non-responsive to indicate an issue.
        /// </summary>
        [EnumMember(Value = "WhenAllAreLate")]
        WhenAllAreLate,

        /// <summary>
        /// Triggers watchdog behavior when any monitored item exceeds its timeout.
        /// Default behavior that provides quickest response to communication issues.
        /// May trigger more frequently in systems with variable update rates.
        /// Best for critical monitoring where any delay requires attention.
        /// </summary>
        [EnumMember(Value = "WhenAnyIsLate")]
        WhenAnyIsLate
    }
}
