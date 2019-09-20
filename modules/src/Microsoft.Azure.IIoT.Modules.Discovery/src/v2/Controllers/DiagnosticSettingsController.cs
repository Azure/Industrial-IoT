// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery.v2.Supervisor {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Serilog;
    using Serilog.Events;
    using System;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Diagnostic settings controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    public class DiagnosticSettingsController : ISettingsController {

        /// <summary>
        /// Called based on the reported connected property.
        /// </summary>
        public bool Connected { get; set; }

        /// <summary>
        /// Set and get the log level
        /// </summary>
        public JToken LogLevel {
            set {
                if (value == null || value.Type == JTokenType.Null) {
                    // Set default
                    LogEx.Level.MinimumLevel = LogEventLevel.Information;
                    _logger.Information("Setting log level to default level.");
                }
                else if (value.Type == JTokenType.String) {
                    // The enum values are the same as in serilog
                    if (!Enum.TryParse<LogEventLevel>((string)value, true,
                        out var level)) {
                        throw new ArgumentException(
                            $"Bad log level value {value} passed.");
                    }
                    _logger.Information("Setting log level to {level}", level);
                    LogEx.Level.MinimumLevel = level;
                }
                else {
                    throw new NotSupportedException(
                        $"Bad log level value type {value.Type}");
                }
            }
            // The enum values are the same as in serilog
            get => JToken.FromObject(LogEx.Level.MinimumLevel.ToString());
        }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="logger"></param>
        public DiagnosticSettingsController(
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private readonly ILogger _logger;
    }
}
