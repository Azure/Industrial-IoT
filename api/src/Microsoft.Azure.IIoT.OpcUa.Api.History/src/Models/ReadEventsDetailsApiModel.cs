// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Read event data
    /// </summary>
    public class ReadEventsDetailsApiModel {

        /// <summary>
        /// Start time to read from
        /// </summary>
        [JsonProperty(PropertyName = "startTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to read to
        /// </summary>
        [JsonProperty(PropertyName = "endTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Number of events to read
        /// </summary>
        [JsonProperty(PropertyName = "numEvents",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? NumEvents { get; set; }

        /// <summary>
        /// The filter to use to select the event fields
        /// </summary>
        [JsonProperty(PropertyName = "filter",
            NullValueHandling = NullValueHandling.Ignore)]
        public EventFilterApiModel Filter { get; set; }
    }
}
