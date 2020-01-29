// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Publisher event
    /// </summary>
    public class PublisherEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        [JsonProperty(PropertyName = "eventType")]
        public PublisherEventType EventType { get; set; }

        /// <summary>
        /// Publisher id
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Publisher
        /// </summary>
        [JsonProperty(PropertyName = "publisher",
            NullValueHandling = NullValueHandling.Ignore)]
        public PublisherApiModel Publisher { get; set; }

        /// <summary>
        /// The information is provided as a patch
        /// </summary>
        [JsonProperty(PropertyName = "isPatch",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPatch { get; set; }
    }
}