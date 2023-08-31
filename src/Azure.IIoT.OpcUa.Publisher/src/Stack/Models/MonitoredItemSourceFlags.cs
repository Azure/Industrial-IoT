// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using System;

    /// <summary>
    /// Source flags
    /// </summary>
    [Flags]
    public enum MonitoredItemSourceFlags
    {
        /// <summary>
        /// Heartbeat is the source of the notification
        /// </summary>
        Heartbeat = 0x1,

        /// <summary>
        /// Cyclic read is the source of the notification
        /// </summary>
        CyclicRead = 0x2,

        /// <summary>
        /// Condition is the source of the notification.
        /// </summary>
        Condition = 0x4,

        /// <summary>
        /// An error is the source of the notification
        /// </summary>
        Error = 0x8
    }
}
