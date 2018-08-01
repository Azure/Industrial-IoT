// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Request node browsing continuation
    /// </summary>
    public class BrowseNextRequestApiModel {

        /// <summary>
        /// Continuation token to use
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken")]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Whether to abort browse and release
        /// </summary>
        [JsonProperty(PropertyName = "abort",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Abort { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticationApiModel Elevation { get; set; }
    }
}
