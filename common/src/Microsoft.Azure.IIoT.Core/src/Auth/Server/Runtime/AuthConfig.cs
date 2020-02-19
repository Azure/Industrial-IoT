// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Service principal configuration - includes auth, host and client configuration
    /// </summary>
    public class AuthConfig : ClientConfig, IAuthConfig {

        /// <summary>
        /// Auth configuration
        /// </summary>
        private const string kAuth_RequiredKey = "Auth:Required";
        private const string kAuth_TrustedIssuerKey = "Auth:TrustedIssuer";
        private const string kAuth_AllowedClockSkewKey = "Auth:AllowedClockSkewSeconds";
        private const string kAuth_AudienceKey = "Auth:Audience";

        /// <summary>Whether required</summary>
        public bool AuthRequired => GetBoolOrDefault(kAuth_RequiredKey,
            GetBoolOrDefault(PcsVariable.PCS_AUTH_REQUIRED, !string.IsNullOrEmpty(AppId)));
        /// <summary>Trusted issuer</summary>
        public string TrustedIssuer => GetStringOrDefault(kAuth_TrustedIssuerKey,
            GetStringOrDefault(PcsVariable.PCS_AUTH_ISSUER, "https://sts.windows.net/"));
        /// <summary>Allowed clock skew</summary>
        public TimeSpan AllowedClockSkew =>
            TimeSpan.FromSeconds(GetIntOrDefault(kAuth_AllowedClockSkewKey, 120));
        /// <summary>Valid audience</summary>
        public string Audience => GetStringOrDefault(kAuth_AudienceKey,
            GetStringOrDefault(PcsVariable.PCS_AUTH_AUDIENCE,
                Domain + "/" + GetStringOrDefault("PCS_AUTH_SERVICE_APPNAME")))?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AuthConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
