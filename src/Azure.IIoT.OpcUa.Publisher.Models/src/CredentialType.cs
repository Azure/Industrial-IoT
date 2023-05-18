// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Type of credentials to use for authentication
    /// </summary>
    [DataContract]
    public enum CredentialType
    {
        /// <summary>
        /// No credentials for anonymous access
        /// </summary>
        [EnumMember(Value = "None")]
        None,

        /// <summary>
        /// User name and password as credential
        /// </summary>
        [EnumMember(Value = "UserName")]
        UserName,

        /// <summary>
        /// Credential is a x509 certificate
        /// </summary>
        [EnumMember(Value = "X509Certificate")]
        X509Certificate,

        /// <summary>
        /// Jwt token as credential
        /// </summary>
        [EnumMember(Value = "JwtToken")]
        JwtToken
    }
}
