// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth.Runtime {
    using Microsoft.Azure.IIoT.Services.Auth;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Auth client configuration
    /// </summary>
    public class AuthConfig : ClientConfig, IAuthConfig {

        /// <summary>
        /// Auth configuration
        /// </summary>
        private const string kAuth_RequiredKey = "Auth:Required";
        private const string kAuth_TrustedIssuerKey = "Auth:TrustedIssuer";
        private const string kAuth_AllowedClockSkewKey = "Auth:AllowedClockSkewSeconds";
        /// <summary>Whether required</summary>
        public bool AuthRequired => GetBoolOrDefault(kAuth_RequiredKey,
            GetBoolOrDefault("PCS_AUTH_REQUIRED", !string.IsNullOrEmpty(AppSecret)));
        /// <summary>Allowed issuer</summary>
        public string TrustedIssuer => GetStringOrDefault(kAuth_TrustedIssuerKey,
            GetStringOrDefault("PCS_AUTH_ISSUER", string.IsNullOrEmpty(TenantId) ?
                null : $"https://login.windows.net/{TenantId}/"));
        /// <summary>Allowed clock skew</summary>
        public TimeSpan AllowedClockSkew =>
            TimeSpan.FromSeconds(GetIntOrDefault(kAuth_AllowedClockSkewKey, 120));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="serviceId"></param>
        public AuthConfig(IConfigurationRoot configuration, string serviceId = "") :
            base(configuration, serviceId) {
        }
    }
}
