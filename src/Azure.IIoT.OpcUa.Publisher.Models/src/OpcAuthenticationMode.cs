// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Specifies the authentication method used to connect to OPC UA servers.
    /// The chosen mode determines how the Publisher authenticates itself to servers.
    /// When using credentials or certificates, encrypted communication should be enabled
    /// via UseSecurity or EndpointSecurityMode to protect authentication secrets.
    /// </summary>
    [DataContract]
    public enum OpcAuthenticationMode
    {
        /// <summary>
        /// No authentication credentials provided (default).
        /// Server must allow anonymous access for connections to succeed.
        /// Least secure option - use only when no security requirements exist
        /// or in secure, isolated networks.
        /// </summary>
        [EnumMember(Value = "Anonymous")]
        Anonymous,

        /// <summary>
        /// Authenticate using username and password credentials.
        /// Requires OpcAuthenticationUsername and OpcAuthenticationPassword.
        /// WARNING: Always use with encrypted communication (SignAndEncrypt)
        /// to prevent credential exposure. Credentials can be stored encrypted
        /// at rest using the --fce command line option.
        /// </summary>
        [EnumMember(Value = "UsernamePassword")]
        UsernamePassword,

        /// <summary>
        /// Authenticate using X.509 certificates.
        /// Uses certificate from User certificate store in PKI configuration.
        /// OpcAuthenticationUsername specifies the certificate subject name.
        /// OpcAuthenticationPassword provides the private key password if needed.
        /// Most secure option when properly managed.
        /// </summary>
        [EnumMember(Value = "Certificate")]
        Certificate
    }
}
