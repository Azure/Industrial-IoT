// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Twin registration update request
    /// </summary>
    public class TwinRegistrationUpdateApiModel {

        /// <summary>
        /// Identifier of the twin to patch
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Whether to copy existing registration
        /// rather than replacing
        /// </summary>
        [JsonProperty(PropertyName = "duplicate",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Duplicate { get; set; }

        /// <summary>
        /// Activate (=true) or disable twin (=false), if
        /// null, unchanged.
        /// </summary>
        [JsonProperty(PropertyName = "activate",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Activate { get; set; }

        /// <summary>
        /// Authentication to use on the twin.
        /// </summary>
        [JsonProperty(PropertyName = "authentication",
            NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticationApiModel Authentication { get; set; }
    }
}
