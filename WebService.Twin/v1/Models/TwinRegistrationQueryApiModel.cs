// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Twin query
    /// </summary>
    public class TwinRegistrationQueryApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinRegistrationQueryApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinRegistrationQueryApiModel(TwinRegistrationQueryModel model) {
            Url = model.Url;
            User = model.User;
            TokenType = model.TokenType;
            IsTrusted = model.IsTrusted;
            SecurityPolicy = model.SecurityPolicy;
            SecurityMode = model.SecurityMode;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public TwinRegistrationQueryModel ToServiceModel() {
            return new TwinRegistrationQueryModel {
                Url = Url,
                User = User,
                TokenType = TokenType,
                IsTrusted = IsTrusted,
                SecurityPolicy = SecurityPolicy,
                SecurityMode = SecurityMode,
            };
        }

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        [JsonProperty(PropertyName = "url",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Url { get; set; }

        /// <summary>
        /// User name to use
        /// </summary>
        [JsonProperty(PropertyName = "user",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string User { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        [JsonProperty(PropertyName = "tokenType",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public TokenType? TokenType { get; set; }

        /// <summary>
        /// Implict trust of the other side
        /// </summary>
        [JsonProperty(PropertyName = "isTrusted",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? IsTrusted { get; set; }

        /// <summary>
        /// Security Mode 
        /// </summary>
        [JsonProperty(PropertyName = "securityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security policy uri 
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SecurityPolicy { get; set; }
    }
}

