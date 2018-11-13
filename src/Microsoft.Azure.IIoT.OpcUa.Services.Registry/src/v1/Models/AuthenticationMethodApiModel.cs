// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Authentication Method model
    /// </summary>
    public class AuthenticationMethodApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public AuthenticationMethodApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public AuthenticationMethodApiModel(AuthenticationMethodModel model) {
            Id = model.Id;
            SecurityPolicy = model.SecurityPolicy;
            Configuration = model.Configuration;
            CredentialType = model.CredentialType == OpcUa.Registry.Models.CredentialType.None ?
                null : model.CredentialType;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public AuthenticationMethodModel ToServiceModel() {
            return new AuthenticationMethodModel {
                Id = Id,
                SecurityPolicy = SecurityPolicy,
                Configuration = Configuration,
                CredentialType = CredentialType ?? OpcUa.Registry.Models.CredentialType.None
            };
        }

        /// <summary>
        /// Method identifier
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Type of credential
        /// </summary>
        [JsonProperty(PropertyName = "credentialType",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(OpcUa.Registry.Models.CredentialType.None)]
        public CredentialType? CredentialType { get; set; }

        /// <summary>
        /// Security policy to use when passing credential.
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Method specific configuration
        /// </summary>
        [JsonProperty(PropertyName = "configuration",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public JToken Configuration { get; set; }
    }
}
