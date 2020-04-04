// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Authentication Method model
    /// </summary>
    [DataContract]
    public class AuthenticationMethodApiModel {

        /// <summary>
        /// Method id
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Type of credential
        /// </summary>
        [DataMember(Name = "credentialType", Order = 1,
            EmitDefaultValue = false)]
        public CredentialType? CredentialType { get; set; }

        /// <summary>
        /// Security policy to use when passing credential.
        /// </summary>
        [DataMember(Name = "securityPolicy", Order = 2,
            EmitDefaultValue = false)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Method specific configuration
        /// </summary>
        [DataMember(Name = "configuration", Order = 3,
            EmitDefaultValue = false)]
        public VariantValue Configuration { get; set; }
    }
}
