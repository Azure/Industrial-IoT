// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Endpoint model for webservice api
    /// </summary>
    public class EndpointApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointApiModel() {}

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointApiModel(EndpointModel model) {
            Url = model.Url;
            User = model.User;
            Token = model.Token;
            TokenType = model.TokenType;
            SecurityMode = model.SecurityMode;
            SecurityPolicy = model.SecurityPolicy;
            Validation = model.Validation;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public EndpointModel ToServiceModel() {
            return new EndpointModel {
                Url = Url,
                User = User,
                Token = Token,
                TokenType = TokenType,
                SecurityMode = SecurityMode,
                SecurityPolicy = SecurityPolicy,
                Validation = Validation,
            };
        }

        /// <summary>
        /// Endpoint
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        [Required]
        public string Url { get; set; }

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
        [DefaultValue(Microsoft.Azure.IIoT.OpcUa.Models.TokenType.None)]
        public TokenType? TokenType { get; set; }

        /// <summary>
        /// Security Mode to use for communication - default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(Microsoft.Azure.IIoT.OpcUa.Models.SecurityMode.Best)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security policy uri to use for communication - default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Certificate to validate against or null to trust any.
        /// </summary>
        [JsonProperty(PropertyName = "validation",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public byte[] Validation { get; set; }
    }
}
