// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

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
        /// Enable (=true) or disable twin
        /// </summary>
        [JsonProperty(PropertyName = "isTrusted",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsTrusted { get; set; }

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
        public object Token { get; set; }

        /// <summary>
        /// Type of token - if null, unchanged
        /// </summary>
        [JsonProperty(PropertyName = "tokenType",
            NullValueHandling = NullValueHandling.Ignore)]
        public TokenType? TokenType { get; set; }

        /// <summary>
        /// Server name - if null, unchanged
        /// </summary>
        [JsonProperty(PropertyName = "applicationName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationName { get; set; }
    }
}
