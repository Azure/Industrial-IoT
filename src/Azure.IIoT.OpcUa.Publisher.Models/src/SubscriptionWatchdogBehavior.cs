// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Subscription watchdog behavior
    /// </summary>
    [DataContract]
    [Flags]
    public enum SubscriptionWatchdogBehavior
    {
        /// <summary>
        /// Just log to diagnostics
        /// </summary>
        [EnumMember(Value = "Diagnostic")]
        Diagnostic,

        /// <summary>
        /// Try to reset the subscription
        /// </summary>
        [EnumMember(Value = "Reset")]
        Reset,

        /// <summary>
        /// Watchdog crashes entire application
        /// </summary>
        [EnumMember(Value = "FailFast")]
        FailFast,

        /// <summary>
        /// Watchdog exits the process with
        /// exit code -10
        /// </summary>
        [EnumMember(Value = "ExitProcess")]
        ExitProcess
    }
}
