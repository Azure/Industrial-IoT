// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Endpoint Event model
    /// </summary>
    public class EndpointEventApiModel {

        /// <summary>
        /// Type of event
        /// </summary>
        [JsonProperty(PropertyName = "eventType")]
        public EndpointEventType EventType { get; set; }

        /// <summary>
        /// Endpoint id
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Endpoint info
        /// </summary>
        [JsonProperty(PropertyName = "endpoint",
            NullValueHandling = NullValueHandling.Ignore)]
        public EndpointInfoApiModel Endpoint { get; set; }

        /// <summary>
        /// The information is provided as a patch
        /// </summary>
        [JsonProperty(PropertyName = "isPatch",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPatch { get; set; }
    }
}