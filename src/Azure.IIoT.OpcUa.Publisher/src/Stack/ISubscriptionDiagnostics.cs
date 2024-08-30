// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    /// <summary>
    /// Safely access subscription diagnostics
    /// </summary>
    public interface ISubscriptionDiagnostics
    {
        /// <summary>
        /// Get good monitored items
        /// </summary>
        int GoodMonitoredItems { get; }

        /// <summary>
        /// Get bad monitored items
        /// </summary>
        int BadMonitoredItems { get; }

        /// <summary>
        /// Late monitored items
        /// </summary>
        int LateMonitoredItems { get; }

        /// <summary>
        /// Heartbeats enabled
        /// </summary>
        int HeartbeatsEnabled { get; }

        /// <summary>
        /// Conditions enabled
        /// </summary>
        int ConditionsEnabled { get; }
    }
}
