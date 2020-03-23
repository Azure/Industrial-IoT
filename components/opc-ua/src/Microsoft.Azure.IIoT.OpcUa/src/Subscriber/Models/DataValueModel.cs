// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {
    using Newtonsoft.Json;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Publisher monitored item sample model
    /// </summary>
    [JsonObject(Id = "dataValue",
        ItemNullValueHandling = NullValueHandling.Ignore)]
    public class DataValueModel{

        /// <summary>
        /// Value
        /// </summary>
        [JsonProperty(PropertyName = "value",
            NullValueHandling = NullValueHandling.Ignore)]
        public dynamic Value {get; set; }

        /// <summary>
        /// Status of the value (Quality)
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }

        /// <summary>
        /// Source Timesamp
        /// </summary>
        [JsonProperty(PropertyName = "sourceTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Server Timestamp
        /// </summary>
        [JsonProperty(PropertyName = "serverTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ServerTimestamp { get; set; }
    }
}