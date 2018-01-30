// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.Runtime {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Runtime;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.Auth;
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
        private const string BYPASS_PROXY = "NoOpcEdgeProxy";
        private const string IOTHUB_CONNSTRING_KEY = "IoTHubConnectionString";
        private const string DEPENDENCIES_KEY = "Dependencies:";
        private const string IOTHUBMANAGER_SERVICE_KEY = DEPENDENCIES_KEY + "IoTHubManager:";
        /// <summary>IoT hub connection string</summary>
        public string IoTHubConnString =>
            GetConnectionString(IOTHUB_CONNSTRING_KEY);
        /// <summary>Whether to bypass proxy</summary>
        public bool BypassProxy =>
            GetBool(BYPASS_PROXY, false);
        /// <summary>IoT hub manager endpoint url</summary>
        public string IoTHubManagerV1ApiUrl => 
            GetString(IOTHUBMANAGER_SERVICE_KEY + "webservice_url");

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
        /// In order to connect to Azure IoT Hub, the service requires a connection
        /// string. The value can be found in the Azure Portal. For more information see
        /// https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-getstarted
        /// to find the connection string value.
        /// 
        /// The connection string can be stored in the 'appsettings.json' configuration
        /// file, or in the PCS_IOTHUB_CONNSTRING environment variable. 
        /// 
        /// When working with VisualStudio, the environment variable can be set in the
        /// WebService project settings, under the "Debug" tab.
        /// </summary>
        /// <returns></returns>
        private string GetConnectionString(string key) {
            var connstring = GetString(key);
            if (string.IsNullOrEmpty(connstring)) {
                return null;
            }
            if (connstring.ToLowerInvariant().Contains("your azure iot hub")) {
                Logger.Warn(
                    "The service configuration is incomplete.  If you do not intend " +
                    "a debug configuration, please provide your Azure IoT Hub connection " +
                    "string. For more information, see the environment variables " +
                    "used in project properties and the 'iothub_connstring' " +
                    "value in the 'appsettings.json' configuration file.", () => { });
                return null;
            }
            return connstring;
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
        /// Read variable and replace environment variable if needed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string GetString(string key, string defaultValue = "") {
            var value = Configuration.GetValue(key, defaultValue);
            ReplaceEnvironmentVariables(ref value, defaultValue);
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

        /// <summary>
        /// Replace all placeholders
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        private void ReplaceEnvironmentVariables(ref string value, string defaultValue) {
            if (string.IsNullOrEmpty(value)) {
                value = defaultValue;
                return;
            }
            // Search for optional replacements: ${?VAR_NAME}
            var keys = Regex.Matches(value, @"\${\?([a-zA-Z_][a-zA-Z0-9_]*)}").Cast<Match>()
                .Select(m => m.Groups[1].Value).Distinct().ToArray();
            // Replace
            foreach (var key in keys) {
                value = value.Replace("${?" + key + "}", GetString(key, string.Empty));
            }

            // Pattern for mandatory replacements: ${VAR_NAME}
            const string PATTERN = @"\${([a-zA-Z_][a-zA-Z0-9_]*)}";
            // Search
            keys = Regex.Matches(value, PATTERN).Cast<Match>()
                .Select(m => m.Groups[1].Value).Distinct().ToArray();
            // Replace
            foreach (var key in keys) {
                var replacement = GetString(key, null);
                if (replacement != null) {
                    value = value.Replace("${" + key + "}", replacement);
                }
            }
            // Non replaced placeholders cause an exception
            keys = Regex.Matches(value, PATTERN).Cast<Match>()
                .Select(m => m.Groups[1].Value).ToArray();
            if (keys.Length > 0) {
                var varsNotFound = keys.Aggregate(", ", (current, k) => current + k);
                Logger.Error("Environment variables not found", () => new { varsNotFound });
                throw new InvalidConfigurationException(
                    "Environment variables not found: " + varsNotFound);
            }
            value.Trim();
            if (string.IsNullOrEmpty(value)) {
                value = defaultValue;
            }
        }
    }
}
