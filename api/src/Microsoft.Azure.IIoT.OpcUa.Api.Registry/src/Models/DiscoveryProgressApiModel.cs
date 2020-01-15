// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Discovery progress
    /// </summary>
    public class DiscoveryProgressApiModel {

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [JsonProperty(PropertyName = "requestId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string RequestId { get; set; }

        /// <summary>
        /// Event type
        /// </summary>
        [JsonProperty(PropertyName = "eventType")]
        public DiscoveryProgressType EventType { get; set; }

        /// <summary>
        /// Discoverer that registered the application
        /// </summary>
        [JsonProperty(PropertyName = "discovererId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DiscovererId { get; set; }

        /// <summary>
        /// Additional request information as per event
        /// </summary>
        [JsonProperty(PropertyName = "requestDetails",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> RequestDetails { get; set; }

        /// <summary>
        /// Timestamp of the message
        /// </summary>
        [JsonProperty(PropertyName = "timeStamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Number of workers running
        /// </summary>
        [JsonProperty(PropertyName = "workers",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? Workers { get; set; }

        /// <summary>
        /// Progress
        /// </summary>
        [JsonProperty(PropertyName = "progress",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? Progress { get; set; }

        /// <summary>
        /// Total
        /// </summary>
        [JsonProperty(PropertyName = "total",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? Total { get; set; }

        /// <summary>
        /// Number of items discovered
        /// </summary>
        [JsonProperty(PropertyName = "discovered",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? Discovered { get; set; }

        /// <summary>
        /// Discovery result
        /// </summary>
        [JsonProperty(PropertyName = "result",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Result { get; set; }

        /// <summary>
        /// Discovery result details
        /// </summary>
        [JsonProperty(PropertyName = "resultDetails",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> ResultDetails { get; set; }
    }
}
