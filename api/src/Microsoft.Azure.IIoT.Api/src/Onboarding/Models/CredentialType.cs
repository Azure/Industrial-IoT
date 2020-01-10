// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of credential to use for serverauth
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CredentialType {

        /// <summary>
        /// No credentials
        /// </summary>
        None,

        /// <summary>
        /// User name with secret
        /// </summary>
        UserName,

        /// <summary>
        /// Certificate
        /// </summary>
        X509Certificate,

        /// <summary>
        /// Token is a jwt token
        /// </summary>
        JwtToken
    }
}
