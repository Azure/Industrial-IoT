// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Service auth configuration
    /// </summary>
    public class AadServiceAuthConfig : ConfigBase, IOAuthServerConfig {

        /// <summary>
        /// Auth configuration
        /// </summary>
        private const string kAuth_TrustedIssuerKey = "Aad:TrustedIssuer";
        private const string kAuth_AllowedClockSkewKey = "Aad:AllowedClockSkewSeconds";
        private const string kAuth_AudienceKey = "Aad:Audience";
        private const string kAuth_TenantIdKey = "Aad:TenantId";
        private const string kAuth_InstanceUrlKey = "Aad:InstanceUrl";

        /// <inheritdoc/>
        public bool IsValid => Audience != null;
        /// <summary>Provider</summary>
        public string Provider => AuthProvider.AzureAD;
        /// <summary>Aad instance url</summary>
        public string InstanceUrl => GetStringOrDefault(kAuth_InstanceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_INSTANCE,
            () => GetStringOrDefault("PCS_WEBUI_AUTH_AAD_INSTANCE",
                () => "https://login.microsoftonline.com"))).Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_TENANT,
            () => GetStringOrDefault("PCS_WEBUI_AUTH_AAD_TENANT",
                () => "common"))).Trim();
        /// <summary>Trusted issuer</summary>
        public string TrustedIssuer => GetStringOrDefault(kAuth_TrustedIssuerKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_ISSUER,
                () => "https://sts.windows.net/"))?.Trim();
        /// <summary>Allowed clock skew</summary>
        public TimeSpan AllowedClockSkew =>
            TimeSpan.FromSeconds(GetIntOrDefault(kAuth_AllowedClockSkewKey,
                () => 120));
        /// <summary>Valid audience</summary>
        public string Audience => GetStringOrDefault(kAuth_AudienceKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_AUDIENCE,
                () => null))?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AadServiceAuthConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
