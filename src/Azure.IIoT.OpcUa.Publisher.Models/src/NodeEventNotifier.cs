// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Flags that can be set for the EventNotifier attribute.
    /// </summary>
    [Flags]
    [DataContract]
    public enum NodeEventNotifier
    {
        /// <summary>
        /// The Object or View produces event
        /// notifications.
        /// </summary>
        [EnumMember(Value = "SubscribeToEvents")]
        SubscribeToEvents = 0x1,

        /// <summary>
        /// The Object has an event history which may
        /// be read.
        /// </summary>
        [EnumMember(Value = "HistoryRead")]
        HistoryRead = 0x4,

        /// <summary>
        /// The Object has an event history which may
        /// be updated.
        /// </summary>
        [EnumMember(Value = "HistoryWrite")]
        HistoryWrite = 0x8,
    }
}
