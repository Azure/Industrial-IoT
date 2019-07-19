// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Flags that can be set for the EventNotifier attribute.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
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
