// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Type of credentials to use for authentication
    /// </summary>
    [DataContract]
    public enum CredentialType {

        /// <summary>
        /// No credentials for anonymous access
        /// </summary>
        [EnumMember]
        None,

        /// <summary>
        /// User name and password as credential
        /// </summary>
        [EnumMember]
        UserName,

        /// <summary>
        /// Credential is a x509 certificate
        /// </summary>
        [EnumMember]
        X509Certificate,

        /// <summary>
        /// Jwt token as credential
        /// </summary>
        [EnumMember]
        JwtToken
    }
}
