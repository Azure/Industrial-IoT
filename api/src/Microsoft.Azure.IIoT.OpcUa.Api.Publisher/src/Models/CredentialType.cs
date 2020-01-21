// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of credentials to use for authentication
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CredentialType {

        /// <summary>
        /// No credentials for anonymous access
        /// </summary>
        None,

        /// <summary>
        /// User name and password as credential
        /// </summary>
        UserName,

        /// <summary>
        /// Credential is a x509 certificate
        /// </summary>
        X509Certificate,

        /// <summary>
        /// Jwt token as credential
        /// </summary>
        JwtToken
    }
}
