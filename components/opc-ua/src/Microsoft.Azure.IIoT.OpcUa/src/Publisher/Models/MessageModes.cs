// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Message modes
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MessageModes {

        /// <summary>
        /// Monitored item messages
        /// </summary>
        MonitoredItem,

        /// <summary>
        /// Pub/Sub subscription message mode
        /// </summary>
        Subscription
    }
}