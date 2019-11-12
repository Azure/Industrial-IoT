// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// A monitored and published item
    /// </summary>
    public class PublishedItemApiModel {

        /// <summary>
        /// Node to monitor
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Publishing interval to use
        /// </summary>
        [JsonProperty(PropertyName = "publishingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? PublishingInterval { get; set; }

        /// <summary>
        /// Sampling interval to use
        /// </summary>
        [JsonProperty(PropertyName = "samplingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? SamplingInterval { get; set; }
    }
}
