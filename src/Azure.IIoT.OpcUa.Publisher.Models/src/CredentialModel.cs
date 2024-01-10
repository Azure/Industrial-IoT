// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Credential model. For backwards compatibility
    /// the actual credentials to pass to the server is set
    /// through the value property.
    /// </summary>
    [DataContract]
    public sealed record class CredentialModel
    {
        /// <summary>
        /// Type of credential
        /// </summary>
        [DataMember(Name = "type", Order = 0,
            EmitDefaultValue = false)]
        public CredentialType? Type { get; set; }

        /// <summary>
        /// Credential to pass to server. Can be omitted in case of
        /// <see cref="CredentialType.None"/>.
        /// </summary>
        [DataMember(Name = "value", Order = 1,
            EmitDefaultValue = false)]
        public UserIdentityModel? Value { get; set; }
    }
}
