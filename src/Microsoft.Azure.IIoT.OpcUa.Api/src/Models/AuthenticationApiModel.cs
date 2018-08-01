// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Type of token to use for serverauth
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TokenType {
        None,
        UserNamePassword,
        X509Certificate
    }

    /// <summary>
    /// Authentication model for webservice api
    /// </summary>
    public class AuthenticationApiModel {

        /// <summary>
        /// User name to use
        /// </summary>
        [JsonProperty(PropertyName = "user",
            NullValueHandling = NullValueHandling.Ignore)]
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        [JsonProperty(PropertyName = "token",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Token { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        [JsonProperty(PropertyName = "tokenType",
            NullValueHandling = NullValueHandling.Ignore)]
        public TokenType? TokenType { get; set; }
    }
}
