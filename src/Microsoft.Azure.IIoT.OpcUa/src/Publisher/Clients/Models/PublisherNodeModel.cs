// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Published node model
    /// </summary>
    public class PublisherNodeModel {

        /// <summary>
        /// Node id
        /// </summary>
        [JsonProperty(PropertyName = "Id",
            NullValueHandling = NullValueHandling.Include)]
        public string Id { get; set; }

        /// <summary>
        /// Publishing interval
        /// </summary>
        [JsonProperty(PropertyName = "OpcPublishingInterval",
            NullValueHandling = NullValueHandling.Include)]
        public int? OpcPublishingInterval { get; set; }

        /// <summary>
        /// Sampling interval
        /// </summary>
        [JsonProperty(PropertyName = "OpcSamplingInterval",
            NullValueHandling = NullValueHandling.Include)]
        public int? OpcSamplingInterval { get; set; }
    }
}
