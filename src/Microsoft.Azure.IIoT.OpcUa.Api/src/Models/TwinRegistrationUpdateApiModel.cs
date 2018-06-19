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
        /// User name to use - if null, unchanged, empty
        /// string to delete
        /// </summary>
        [JsonProperty(PropertyName = "user",
            NullValueHandling = NullValueHandling.Ignore)]
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server - if null, unchanged, empty
        /// string to delete
        /// </summary>
        [JsonProperty(PropertyName = "token",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Token { get; set; }

        /// <summary>
        /// Type of token - if null, unchanged
        /// </summary>
        [JsonProperty(PropertyName = "tokenType",
            NullValueHandling = NullValueHandling.Ignore)]
        public TokenType? TokenType { get; set; }
    }
}
