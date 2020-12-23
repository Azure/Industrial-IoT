// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;

    /// <summary>
    /// Flags that can be set for the EventNotifier attribute.
    /// </summary>
    [Flags]
    public enum NodeEventNotifier {

        /// <summary>
        /// The Object or View produces event
        /// notifications.
        /// </summary>
        SubscribeToEvents = 0x1,

        /// <summary>
        /// The Object has an event history which may
        /// be read.
        /// </summary>
        HistoryRead = 0x4,

        /// <summary>
        /// The Object has an event history which may
        /// be updated.
        /// </summary>
        HistoryWrite = 0x8,
    }
}
