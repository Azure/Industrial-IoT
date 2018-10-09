// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel;

    /// <summary>
    /// Authentication model
    /// </summary>
    public class AuthenticationApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public AuthenticationApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public AuthenticationApiModel(AuthenticationModel model) {
            User = model.User;
            Token = model.Token;
            TokenType = model.TokenType;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public AuthenticationModel ToServiceModel() {
            return new AuthenticationModel {
                User = User,
                Token = Token,
                TokenType = TokenType
            };
        }

        /// <summary>
        /// User name to use
        /// </summary>
        [JsonProperty(PropertyName = "user",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        [JsonProperty(PropertyName = "token",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public JToken Token { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        [JsonProperty(PropertyName = "tokenType",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(Microsoft.Azure.IIoT.OpcUa.Registry.Models.TokenType.None)]
        public TokenType? TokenType { get; set; }
    }
}
