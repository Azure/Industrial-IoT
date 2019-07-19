// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Read processed historic data
    /// </summary>
    public class ReadProcessedValuesDetailsApiModel {

        /// <summary>
        /// Start time to read from.
        /// </summary>
        [JsonProperty(PropertyName = "startTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to read until
        /// </summary>
        [JsonProperty(PropertyName = "endTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Interval to process
        /// </summary>
        [JsonProperty(PropertyName = "processingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public double? ProcessingInterval { get; set; }

        /// <summary>
        /// The aggregate type node ids
        /// </summary>
        [JsonProperty(PropertyName = "aggregateTypeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AggregateTypeId { get; set; }

        /// <summary>
        /// A configuration for the aggregate
        /// </summary>
        [JsonProperty(PropertyName = "aggregateConfiguration",
            NullValueHandling = NullValueHandling.Ignore)]
        public AggregateConfigApiModel AggregateConfiguration { get; set; }
    }
}
