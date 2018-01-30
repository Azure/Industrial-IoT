// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.Runtime {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Web service configuration - wraps a configuration root
    /// </summary>
    public class EdgeConfig : IOpcUaServicesConfig, IEdgeConfig {

        /// <summary>
        /// A configured logger
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Configuration
        /// </summary>
        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// Edge module configuration
        /// </summary>
        private const string BYPASS_CERT_VERIFICATION = "BypassCertVerification";
        private const string EDGEHUB_CONNSTRING_KEY = "EdgeHubConnectionString";
        private const string IOTHUB_CONNSTRING_KEY = "IotHubConnectionString";
        /// <summary>Edge hub connection string</summary>
        public string EdgeHubConnectionString =>
            GetString(EDGEHUB_CONNSTRING_KEY, 
                GetString(IOTHUB_CONNSTRING_KEY, 
                    GetString("_HUB_CS", null)));
        /// <summary>Whether to bypass cert validation</summary>
        public bool BypassCertVerification =>
            GetBool(BYPASS_CERT_VERIFICATION, GetString("_HUB_CS", null) != null);

        /// <summary>
        /// Dummy Service configuration
        /// </summary>
        public string IoTHubConnString => null;
        public string IoTHubManagerV1ApiUrl => null;
        public bool BypassProxy => false;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public EdgeConfig(IConfigurationRoot configuration) {
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
        /// Read variable and replace environment variable if needed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string GetString(string key, string defaultValue = "") =>
            Configuration.GetValue(key, defaultValue);

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
