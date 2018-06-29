// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Runtime {
    using Microsoft.Azure.IIoT.Services.Swagger;
    using Microsoft.Azure.IIoT.Services.Auth;
    using Microsoft.Azure.IIoT.Services.Cors;
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Web service configuration - wraps a configuration root as well
    /// as reads simple configuration from the following environment
    /// variables:
    /// - IIOT_CORS_WHITELIST (optional, defaults to *)
    /// - IIOT_AUTH_TRUSTED_ISSUE (optional, defaults to 
    ///     "https://login.windows...")
    /// - IIOT_AUTH_APP_ID (set to registered application id)
    /// - IIOT_AUTH_APP_KEY (if not provided, auth will be off)
    /// - IIOT_AUTH_CLIENT_ID (set to registered client application
    ///     id)
    /// - IIOT_AUTH_CLIENT_KEY (if not provided, but auth is on, 
    ///     swagger will be off)
    /// - IIOT_AAD_TENANT_ID (if not set, "common" tenant will be used)
    /// </summary>
    public class ServiceConfig : ConfigBase, IAuthConfig,
        ICorsConfig, IClientConfig, ISwaggerConfig {

        /// <summary>
        /// Cors configuration
        /// </summary>
        private const string kCorsWhitelistKey = "CorsWhitelist";
        /// <summary>Cors whitelist</summary>
        public string CorsWhitelist => GetString(kCorsWhitelistKey,
            GetString("IIOT_CORS_WHITELIST", "*"));
        /// <summary>Whether enabled</summary>
        public bool CorsEnabled =>
            !string.IsNullOrEmpty(CorsWhitelist.Trim());

        /// <summary>
        /// Auth configuration
        /// </summary>
        private const string kAuthKey = "Auth:";
        private const string kAuth_RequiredKey = kAuthKey + "Required";
        private const string kAuth_TrustedIssuerKey = kAuthKey + "TrustedIssuer";
        private const string kAuth_AllowedClockSkewKey = kAuthKey + "AllowedClockSkewSeconds";
        /// <summary>Whether required</summary>
        public bool AuthRequired =>
            GetBool(kAuth_RequiredKey, !string.IsNullOrEmpty(ClientSecret));
        /// <summary>Allowed issuer</summary>
        public string TrustedIssuer => GetString(kAuth_TrustedIssuerKey,
            GetString("IIOT_AUTH_TRUSTED_ISSUER", string.IsNullOrEmpty(TenantId) ?
                null : $"https://login.windows.net/{TenantId}/"));
        /// <summary>Allowed clock skew</summary>
        public TimeSpan AllowedClockSkew =>
            TimeSpan.FromSeconds(GetInt(kAuth_AllowedClockSkewKey, 120));

        /// <summary>
        /// Application configuration
        /// </summary>
        private const string kAuth_AppIdKey = "AppId";
        private const string kAuth_AppSecretKey = "AppSecret";
        private const string kAuth_TenantIdKey = kAuthKey + "TenantId";
        private const string kAuth_AuthorityKey = kAuthKey + "Authority";
        /// <summary>Application id</summary>
        public string ClientId => GetString(kAuth_AppIdKey, GetString(
            ServiceId + "_APP_ID", GetString("IIOT_AUTH_APP_ID"))).Trim();
        /// <summary>App secret for behalf of flow</summary>
        public string ClientSecret => GetString(kAuth_AppSecretKey, GetString(
            ServiceId + "_APP_KEY", GetString("IIOT_AUTH_APP_KEY"))).Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => GetString(kAuth_TenantIdKey,
            GetString("IIOT_AAD_TENANT_ID"));
        /// <summary>Optional authority</summary>
        public string Authority => GetString(kAuth_AuthorityKey);

        /// <summary>
        /// Swagger configuration
        /// </summary>
        private const string kSwaggerKey = "Swagger:";
        private const string kSwagger_EnabledKey = kSwaggerKey + "Enabled";
        private const string kSwagger_AppIdKey = kSwaggerKey + "AppId";
        private const string kSwagger_AppSecretKey = kSwaggerKey + "AppSecret";
        /// <summary>Enabled</summary>
        public bool UIEnabled => GetBool(kSwagger_EnabledKey, 
            !AuthRequired || !string.IsNullOrEmpty(SwaggerClientSecret));
        /// <summary>Auth enabled</summary>
        public bool WithAuth =>
            AuthRequired;
        /// <summary>Application id</summary>
        public string SwaggerClientId => GetString(kSwagger_AppIdKey, GetString(
            ServiceId + "_CLIENT_ID", GetString("IIOT_AUTH_CLIENT_ID"))).Trim();
        /// <summary>Application key</summary>
        public string SwaggerClientSecret => GetString(kSwagger_AppSecretKey, GetString(
            ServiceId + "_CLIENT_KEY", GetString("IIOT_AUTH_CLIENT_KEY"))).Trim();

        public string ServiceId { get; }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ServiceConfig(string processId, string serviceId, 
            IConfigurationRoot configuration) :
            base(processId, configuration) {
            ServiceId = serviceId.ToUpperInvariant();
        }
    }
}
