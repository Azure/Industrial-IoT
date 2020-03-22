// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using System.Runtime.Serialization;
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Authentication Method model
    /// </summary>
    [DataContract]
    public class AuthenticationMethodApiModel {

        /// <summary>
        /// Method id
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Type of credential
        /// </summary>
        [DataMember(Name = "credentialType",
            EmitDefaultValue = false)]
        public CredentialType? CredentialType { get; set; }

        /// <summary>
        /// Security policy to use when passing credential.
        /// </summary>
        [DataMember(Name = "securityPolicy",
            EmitDefaultValue = false)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Method specific configuration
        /// </summary>
        [DataMember(Name = "configuration",
            EmitDefaultValue = false)]
        public VariantValue Configuration { get; set; }
    }
}
