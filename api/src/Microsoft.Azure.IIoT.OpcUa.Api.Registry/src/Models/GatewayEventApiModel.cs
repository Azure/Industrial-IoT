// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Gateway event
    /// </summary>
    public class GatewayEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        [JsonProperty(PropertyName = "eventType")]
        public GatewayEventType EventType { get; set; }

        /// <summary>
        /// Gateway id
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Gateway
        /// </summary>
        [JsonProperty(PropertyName = "gateway",
            NullValueHandling = NullValueHandling.Ignore)]
        public GatewayApiModel Gateway { get; set; }

        /// <summary>
        /// The information is provided as a patch
        /// </summary>
        [JsonProperty(PropertyName = "isPatch",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPatch { get; set; }
    }
}