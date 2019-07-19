// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Read historic values
    /// </summary>
    public class ReadValuesDetailsApiModel {

        /// <summary>
        /// The start time to read from
        /// </summary>
        [JsonProperty(PropertyName = "startTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// The end time to read to
        /// </summary>
        [JsonProperty(PropertyName = "endTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The number of values to read
        /// </summary>
        [JsonProperty(PropertyName = "numValues",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? NumValues { get; set; }

        /// <summary>
        /// Whether to return the bounds as well.
        /// </summary>
        [JsonProperty(PropertyName = "returnBounds",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? ReturnBounds { get; set; }
    }
}
