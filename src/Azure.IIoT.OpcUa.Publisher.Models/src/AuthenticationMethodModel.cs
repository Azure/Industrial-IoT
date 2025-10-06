// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Authentication Method model
    /// </summary>
    [DataContract]
    public sealed record class AuthenticationMethodModel
    {
        /// <summary>
        /// Method id
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public required string Id { get; set; }

        /// <summary>
        /// Type of credential
        /// </summary>
        [DataMember(Name = "credentialType", Order = 1)]
        public CredentialType CredentialType { get; set; }

        /// <summary>
        /// Security policy to use when passing credential.
        /// </summary>
        [DataMember(Name = "securityPolicy", Order = 2,
            EmitDefaultValue = false)]
        public string? SecurityPolicy { get; set; }

        /// <summary>
        /// Method specific configuration
        /// </summary>
        [DataMember(Name = "configuration", Order = 3,
            EmitDefaultValue = false)]
        [SkipValidation]
        public VariantValue? Configuration { get; set; }
    }
}
