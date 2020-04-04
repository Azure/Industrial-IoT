// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic.v2.Supervisor {
    using Microsoft.Azure.IIoT.Modules.Diagnostic.Services;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Diagnostic settings controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    public class DiagnosticSettingsController : ISettingsController {

        /// <summary>
        /// Send frequency in seconds
        /// </summary>
        public int Interval {
            get => (int)_publisher.Interval.TotalSeconds;
            set => _publisher.Interval = TimeSpan.FromSeconds(value);
        }

        /// <summary>
        /// Set and get the log level
        /// </summary>
        public string LogLevel {
            set {
                if (value == null) {
                    // Set default
                    LogControl.Level.MinimumLevel = LogEventLevel.Information;
                    _logger.Information("Setting log level to default level.");
                }
                else {
                    // The enum values are the same as the ones defined for serilog
                    if (!Enum.TryParse<LogEventLevel>(value, true,
                        out var level)) {
                        throw new ArgumentException(
                            $"Bad log level value {value} passed.");
                    }
                    _logger.Information("Setting log level to {level}", level);
                    LogControl.Level.MinimumLevel = level;
                }
            }
            // The enum values are the same as in serilog
            get => LogControl.Level.MinimumLevel.ToString();
        }

        /// <summary>
        /// Test settings
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public VariantValue this[string key] {
            set {
                if (value == null || VariantValueEx.IsNull(value)) {
                    _tempState.AddOrUpdate(key, null);
                    return;
                }
                _tempState.AddOrUpdate(key, value);
            }
            get {
                _tempState.TryGetValue(key, out var result);
                return result;
            }
        }

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="publisher"></param>
        /// <param name="logger"></param>
        public DiagnosticSettingsController(ITelemetrySender publisher, ILogger logger) {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tempState = new Dictionary<string, VariantValue>();
        }

        /// <summary>
        /// Apply changes
        /// </summary>
        /// <returns></returns>
        public Task ApplyAsync() {
            foreach (var item in _tempState.ToList()) {
                if (VariantValueEx.IsNull(item.Value)) {
                    _logger.Information("Removed {Key}", item.Key);
                }
                else {
                    _logger.Information("Added {Key} {Value}", item.Key, item.Value);
                }
                _tempState.Remove(item.Key);
            }
            return Task.CompletedTask;
        }

        private readonly Dictionary<string, VariantValue> _tempState;
        private readonly ITelemetrySender _publisher;
        private readonly ILogger _logger;
    }
}
