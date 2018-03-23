// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Runtime {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Runtime;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Auth;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Web service configuration - wraps a configuration root
    /// </summary>
    public class Config : IOpcUaServicesConfig, IClientAuthConfig {

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
        private const string USE_OPC_EDGE_PROXY_KEX = "UseOpcEdgeProxy";
        private const string IOTHUB_CONNECTION_STRING_KEY = "IoTHubConnectionString";
        private const string IOTHUB_MANAGER_URL_KEY = "IoTHubManagerUrl";
        /// <summary>IoT hub connection string</summary>
        public string IoTHubConnString => GetString(
            IOTHUB_CONNECTION_STRING_KEY, GetString("_HUB_CS", null));
        /// <summary>Whether to bypass proxy</summary>
        public bool BypassProxy =>
            !GetBool(USE_OPC_EDGE_PROXY_KEX, false);
        /// <summary>IoT hub manager endpoint url</summary>
        public string IoTHubManagerV1ApiUrl => GetString(IOTHUB_MANAGER_URL_KEY,
            GetString("PCS_IOTHUBMANAGER_WEBSERVICE_URL", null));

        /// <summary>
        /// Auth configuration
        /// </summary>
        ///
        private const string AUTH_KEY = "Auth:";
        private const string CORS_WHITELIST_KEY = AUTH_KEY + "cors_whitelist";
        private const string AUTH_TYPE_KEY = AUTH_KEY + "auth_type";
        private const string AUTH_REQUIRED_KEY = AUTH_KEY + "auth_required";
        /// <summary>Cors whitelist</summary>
        public string CorsWhitelist =>
            GetString(CORS_WHITELIST_KEY, string.Empty);
        /// <summary>Whether enabled</summary>
        public bool CorsEnabled =>
            !string.IsNullOrEmpty(CorsWhitelist.Trim());
        /// <summary>Auth needed?</summary>
        public bool AuthRequired =>
            GetBool(AUTH_REQUIRED_KEY, false);
        /// <summary>Type of auth token</summary>
        public string AuthType =>
            GetString(AUTH_TYPE_KEY, "JWT");

        private const string JWT_KEY = AUTH_KEY + "JWT:";
        private const string JWT_ALGOS_KEY = JWT_KEY + "allowed_algorithms";
        private const string JWT_ISSUER_KEY = JWT_KEY + "issuer";
        private const string JWT_AUDIENCE_KEY = JWT_KEY + "audience";
        private const string JWT_CLOCK_SKEW_KEY = JWT_KEY + "clock_skew_seconds";
        /// <summary>Allowed JWT algos</summary>
        public IEnumerable<string> JwtAllowedAlgos =>
            GetString(JWT_ALGOS_KEY, "RS256,RS384,RS512").Split(',');
        /// <summary>JWT issuer</summary>
        public string JwtIssuer =>
            GetString(JWT_ISSUER_KEY);
        /// <summary>JWT audience</summary>
        public string JwtAudience =>
            GetString(JWT_AUDIENCE_KEY);
        /// <summary>JWT clock skew</summary>
        public TimeSpan JwtClockSkew =>
            TimeSpan.FromSeconds(GetInt(JWT_CLOCK_SKEW_KEY, 120));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) {
            Configuration = configuration;
            Logger = new Logger(Uptime.ProcessId,
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
            return value;
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
