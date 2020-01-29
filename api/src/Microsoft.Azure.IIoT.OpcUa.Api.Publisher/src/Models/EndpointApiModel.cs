// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint model
    /// </summary>
    public class EndpointApiModel {

        /// <summary>
        /// Endpoint url to use to connect with
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        /// <summary>
        /// Alternative endpoint urls that can be used for
        /// accessing and validating the server
        /// </summary>
        [JsonProperty(PropertyName = "alternativeUrls",
            NullValueHandling = NullValueHandling.Ignore)]
        public HashSet<string> AlternativeUrls { get; set; }

        /// <summary>
        /// Security Mode to use for communication.
        /// default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security policy uri to use for communication.
        /// default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Endpoint certificate thumbprint
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Certificate { get; set; }
    }
}
