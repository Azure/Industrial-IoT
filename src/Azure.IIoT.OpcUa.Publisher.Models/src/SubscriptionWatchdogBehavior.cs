// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines how the publisher responds when monitored items stop reporting data.
    /// The watchdog triggers when items are late according to OpcNodeWatchdogTimespan
    /// and OpcNodeWatchdogCondition settings. Can be configured globally via the
    /// --dwb command line option.
    /// </summary>
    [DataContract]
    [Flags]
    public enum SubscriptionWatchdogBehavior
    {
        /// <summary>
        /// Log watchdog events to diagnostics output only.
        /// Least intrusive behavior that maintains normal operation.
        /// Useful for monitoring and troubleshooting connection issues.
        /// Recommended for non-critical monitoring scenarios.
        /// </summary>
        [EnumMember(Value = "Diagnostic")]
        Diagnostic,

        /// <summary>
        /// Attempts to reestablish the subscription when watchdog triggers.
        /// Automatically recovers from temporary connection issues.
        /// May cause brief interruption during reset operation.
        /// Good balance between reliability and availability.
        /// </summary>
        [EnumMember(Value = "Reset")]
        Reset,

        /// <summary>
        /// Immediately terminates the publisher application when triggered.
        /// Most aggressive recovery option that forces complete restart.
        /// Useful when clean restart is required for recovery.
        /// WARNING: Will disrupt all active subscriptions.
        /// </summary>
        [EnumMember(Value = "FailFast")]
        FailFast,

        /// <summary>
        /// Gracefully exits the process with exit code -10 when triggered.
        /// Allows container orchestrators or service managers to handle restart.
        /// Provides clean shutdown with specific error indication.
        /// Suitable for managed deployment environments.
        /// </summary>
        [EnumMember(Value = "ExitProcess")]
        ExitProcess
    }
}
