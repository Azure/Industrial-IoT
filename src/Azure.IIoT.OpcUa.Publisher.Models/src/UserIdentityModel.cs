// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// User identity model
    /// </summary>
    [DataContract]
    public sealed record class UserIdentityModel
    {
        /// <summary>
        /// <para>
        /// For <see cref="CredentialType.UserName"/> authentication
        /// this is the name of the user.
        /// </para>
        /// <para>
        /// For <see cref="CredentialType.X509Certificate"/> authentication
        /// this is the subject name of the certificate that has been
        /// configured.
        /// Either <see cref="User"/> or <see cref="Thumbprint"/> must be
        /// used to select the certificate in the user certificate store.
        /// </para>
        /// <para>
        /// Not used for the other authentication types.
        /// </para>
        /// </summary>
        [DataMember(Name = "user", Order = 1,
            EmitDefaultValue = false)]
        public string? User { get; set; }

        /// <summary>
        /// <para>
        /// For <see cref="CredentialType.UserName"/> authentication
        /// this is the password of the user.
        /// </para>
        /// <para>
        /// For <see cref="CredentialType.X509Certificate"/> authentication
        /// this is the passcode to export the configured certificate's
        /// private key.
        /// </para>
        /// <para>
        /// Not used for the other authentication types.
        /// </para>
        /// </summary>
        [DataMember(Name = "password", Order = 2,
            EmitDefaultValue = false)]
        public string? Password { get; set; }

        /// <summary>
        /// <para>
        /// For <see cref="CredentialType.X509Certificate"/> authentication
        /// this is the thumbprint of the configured certificate to use.
        /// Either <see cref="User"/> or <see cref="Thumbprint"/> must be
        /// used to select the certificate in the user certificate store.
        /// </para>
        /// <para>
        /// Not used for the other authentication types.
        /// </para>
        /// </summary>
        [DataMember(Name = "thumbprint", Order = 3,
            EmitDefaultValue = false)]
        public string? Thumbprint { get; set; }
    }
}
