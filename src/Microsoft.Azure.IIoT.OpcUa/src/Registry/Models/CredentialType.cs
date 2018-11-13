// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of credential to use for serverauth
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CredentialType {

        /// <summary>
        /// Anonymous
        /// </summary>
        None,

        /// <summary>
        /// Token is password
        /// </summary>
        UserNamePassword,

        /// <summary>
        /// Token is a x509 cert
        /// </summary>
        X509Certificate,

        /// <summary>
        /// Token is a jwt token
        /// </summary>
        JwtToken
    }
}
