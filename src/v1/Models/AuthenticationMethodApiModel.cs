// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json.Linq;

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
            Id = model?.Id;
            SecurityPolicy = model?.SecurityPolicy;
            Configuration = model?.Configuration;
            CredentialType = model?.CredentialType;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public AuthenticationMethodModel ToServiceModel() {
            return new AuthenticationMethodModel {
                Id = Id,
                SecurityPolicy = SecurityPolicy,
                Configuration = Configuration,
                CredentialType = CredentialType
            };
        }

        /// <summary>
        /// Method id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Type of credential
        /// </summary>
        public CredentialType? CredentialType { get; set; }

        /// <summary>
        /// Security policy to use when passing credential.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Method specific configuration
        /// </summary>
        public JToken Configuration { get; set; }
    }
}
