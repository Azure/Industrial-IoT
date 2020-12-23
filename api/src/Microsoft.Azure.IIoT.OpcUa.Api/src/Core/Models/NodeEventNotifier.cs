// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Flags that can be set for the EventNotifier attribute.
    /// </summary>
    [Flags]
    [DataContract]
    public enum NodeEventNotifier {

        /// <summary>
        /// The Object or View produces event notifications.
        /// </summary>
        [EnumMember]
        SubscribeToEvents = 0x1,

        /// <summary>
        /// The Object has an event history which may
        /// be read.
        /// </summary>
        [EnumMember]
        HistoryRead = 0x4,

        /// <summary>
        /// The Object has an event history which may
        /// be updated.
        /// </summary>
        [EnumMember]
        HistoryWrite = 0x8,
    }
}
