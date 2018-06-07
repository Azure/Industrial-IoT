// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Services.Protocol;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Web.Swagger;
    using Microsoft.Azure.IIoT.Web.Auth;
    using Microsoft.Azure.IIoT.Web.Cors;
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Web service configuration - wraps a configuration root
    /// </summary>
    public class Config : IOpcUaConfig, IIoTHubConfig, IAuthConfig,
        ICorsConfig, IClientConfig, ISwaggerConfig {

        /// <summary>
        /// A configured logger
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Configuration
        /// </summary>
        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// Service configuration
        /// </summary>
        private const string kUseOpcEdgeProxyKey = "UseOpcEdgeProxy";
        private const string kIoTHubConnectionStringKey = "IoTHubConnectionString";
        /// <summary>IoT hub connection string</summary>
        public string IoTHubConnString => GetString(kIoTHubConnectionStringKey,
            GetString(ServiceInfo.ID + "_HUB_CS", GetString("_HUB_CS", null)));
        /// <summary>Whether to bypass proxy</summary>
        public bool BypassProxy =>
            !GetBool(kUseOpcEdgeProxyKey, false);
        /// <summary>IoT hub manager endpoint url</summary>

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
            GetBool(kAuth_RequiredKey, ClientSecret != null);
        /// <summary>Allowed issuer</summary>
        public string TrustedIssuer => GetString(kAuth_TrustedIssuerKey,
            GetString("IIOT_AAD_TRUSTED_ISSUER", string.IsNullOrEmpty(TenantId) ?
                null : $"https://login.windows.net/{TenantId}/"));
        /// <summary>Allowed clock skew</summary>
        public TimeSpan AllowedClockSkew =>
            TimeSpan.FromSeconds(GetInt(kAuth_AllowedClockSkewKey, 120));

        /// <summary>
        /// Application configuration
        /// </summary>
        private const string kAuth_AppIdKey = kAuthKey + "AppId";
        private const string kAuth_AppSecretKey = kAuthKey + "AppSecret";
        private const string kAuth_TenantIdKey = kAuthKey + "TenantId";
        private const string kAuth_AuthorityKey = kAuthKey + "Authority";
        /// <summary>Application id</summary>
        public string ClientId => GetString(kAuth_AppIdKey,
            GetString(ServiceInfo.ID + "_APP_ID"))?.Trim();
        /// <summary>App secret for behalf of flow</summary>
        public string ClientSecret => GetString(kAuth_AppSecretKey,
            GetString(ServiceInfo.ID + "_APP_KEY"))?.Trim();
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
        public bool UIEnabled =>
            GetBool(kSwagger_EnabledKey, true);
        /// <summary>Auth enabled</summary>
        public bool WithAuth =>
            AuthRequired;
        /// <summary>Application id</summary>
        public string SwaggerClientId => GetString(kSwagger_AppIdKey,
            GetString(ServiceInfo.ID + "_APP_ID_SWAGGER"))?.Trim();
        /// <summary>Application key</summary>
        public string SwaggerClientSecret => GetString(kSwagger_AppSecretKey,
            GetString(ServiceInfo.ID + "_APP_KEY_SWAGGER"))?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) {
            Configuration = configuration;
            Logger = new ConsoleLogger(Uptime.ProcessId,
                GetLogLevel("Logging:LogLevel:Default", LogLevel.Debug));
        }

        /// <summary>
        /// Get log level
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private LogLevel GetLogLevel(string key, LogLevel defaultValue) {
            var level = GetString(key);
            if (!string.IsNullOrEmpty(level)) {
                switch (level.ToLowerInvariant()) {
                    case "Warning":
                        return LogLevel.Warn;
                    case "Trace":
                    case "Debug":
                        return LogLevel.Debug;
                    case "Information":
                        return LogLevel.Info;
                    case "Error":
                    case "Critical":
                        return LogLevel.Error;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Read string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string GetString(string key, string defaultValue = "") {
            var value = Configuration.GetValue(key, defaultValue);
            if (string.IsNullOrEmpty(value)) {
                return defaultValue;
            }
            return value.Trim();
        }

        /// <summary>
        /// Read boolean
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private bool GetBool(string key, bool defaultValue = false) {
            var value = GetString(key, defaultValue.ToString()).ToLowerInvariant();
            var knownTrue = new HashSet<string> { "true", "t", "yes", "y", "1", "-1" };
            var knownFalse = new HashSet<string> { "false", "f", "no", "n", "0" };
            if (knownTrue.Contains(value)) {
                return true;
            }
            if (knownFalse.Contains(value)) {
                return false;
            }
            return defaultValue;
        }

        /// <summary>
        /// Read int
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private int GetInt(string key, int defaultValue = 0) {
            try {
                return Convert.ToInt32(GetString(key, defaultValue.ToString()));
            }
            catch (Exception e) {
                throw new InvalidConfigurationException(
                    $"Unable to load configuration value for '{key}'", e);
            }
        }
    }
}
