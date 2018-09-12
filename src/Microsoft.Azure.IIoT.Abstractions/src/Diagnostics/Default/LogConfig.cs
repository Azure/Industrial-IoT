// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Logging configuration
    /// </summary>
    public class LogConfig : ConfigBase, ILogConfig {

        /// <summary>
        /// Create log configuration
        /// </summary>
        public LogConfig() :
            this(null) {
        }

        /// <summary>
        /// Create log configuration
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="configuration"></param>
        public LogConfig(string processId,
            IConfigurationRoot configuration) :
            base(configuration) {
            _processId = processId;
        }

        /// <summary>
        /// Create log configuration
        /// </summary>
        /// <param name="configuration"></param>
        public LogConfig(IConfigurationRoot configuration) :
            this(null, configuration) {
        }

        /// <inheritdoc/>
        public LogLevel LogLevel => GetLogLevel(
            "Logging:LogLevel:Default", LogLevel.Debug);

        /// <inheritdoc/>
        public string ProcessId => GetString(
            "Logging:ProcessId", _processId);


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

        private readonly string _processId;
    }
}
