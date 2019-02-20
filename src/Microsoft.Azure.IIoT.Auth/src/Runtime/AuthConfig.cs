// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Auth client configuration
    /// </summary>
    public class AuthConfig : ClientConfig, IAuthConfig {

        /// <summary>
        /// Auth configuration
        /// </summary>
        private const string kAuth_HttpsRedirectPortKey = "Auth:HttpsRedirectPort";
        private const string kAuth_RequiredKey = "Auth:Required";
        private const string kAuth_TrustedIssuerKey = "Auth:TrustedIssuer";
        private const string kAuth_AllowedClockSkewKey = "Auth:AllowedClockSkewSeconds";

        /// <summary>Whether required</summary>
        public bool AuthRequired => GetBoolOrDefault(kAuth_RequiredKey,
            GetBoolOrDefault("PCS_AUTH_REQUIRED", !string.IsNullOrEmpty(Audience)));
        /// <summary>Https enforced</summary>
        public int HttpsRedirectPort => GetIntOrDefault(kAuth_HttpsRedirectPortKey,
            GetIntOrDefault("PCS_AUTH_HTTPSREDIRECTPORT", 0));
        /// <summary>Trusted issuer</summary>
        public string TrustedIssuer => GetStringOrDefault(kAuth_TrustedIssuerKey,
            GetStringOrDefault("PCS_AUTH_ISSUER", "https://sts.windows.net/"));
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
