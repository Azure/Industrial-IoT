// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Service auth configuration - includes auth and host configuration
    /// </summary>
    public class AuthServiceServiceAuthConfig : AuthServiceConfigBase, IOAuthServerConfig {

        /// <summary>
        /// Auth configuration
        /// </summary>
        private const string kAuth_TrustedIssuerKey = "Auth:TrustedIssuer";
        private const string kAuth_AllowedClockSkewKey = "Auth:AllowedClockSkewSeconds";
        private const string kAuth_AudienceKey = "Auth:Audience";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";

        /// <summary>Scheme</summary>
        public string Scheme => "authservice";
        /// <summary>Auth server instance url</summary>
        public string InstanceUrl => IsDisabled ? null :
            GetStringOrDefault(kAuth_InstanceUrlKey,
                () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_URL,
                    () => GetDefaultUrl("9090", "auth")));
        /// <summary>Trusted issuer</summary>
        public string TrustedIssuer => IsDisabled ? null :
            GetStringOrDefault(kAuth_TrustedIssuerKey,
                () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_ISSUER))?.Trim();
        /// <summary>Allowed clock skew</summary>
        public TimeSpan AllowedClockSkew =>
            TimeSpan.FromSeconds(GetIntOrDefault(kAuth_AllowedClockSkewKey,
                () => 120));
        /// <summary>Valid audience</summary>
        public string Audience => IsDisabled ? null :
            GetStringOrDefault(kAuth_AudienceKey,
                () => GetStringOrDefault(PcsVariable.PCS_SERVICE_NAME,
                    () => "iiot"))?.Trim();

        /// <summary>No tenant</summary>
        public string TenantId => null;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AuthServiceServiceAuthConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
