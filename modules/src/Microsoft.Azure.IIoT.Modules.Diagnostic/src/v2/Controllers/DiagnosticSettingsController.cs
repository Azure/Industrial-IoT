// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic.v2.Supervisor {
    using Microsoft.Azure.IIoT.Modules.Diagnostic.Services;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

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
        /// Other test settings
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public JToken this[string key] {
            set {
                if (value == null || value.Type == JTokenType.Null) {
                    _tempState.AddOrUpdate(key, null);
                    return;
                }
                _tempState.AddOrUpdate(key, value.ToString());
            }
            get {
                if (!_tempState.TryGetValue(key, out var result)) {
                    result = null;
                }
                return result != null ? JToken.FromObject(result) : JValue.CreateNull();
            }
        }

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="publisher"></param>
        /// <param name="logger"></param>
        public DiagnosticSettingsController(IPublisher publisher, ILogger logger) {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tempState = new Dictionary<string, string>();
        }

        /// <summary>
        /// Apply changes
        /// </summary>
        /// <returns></returns>
        public Task ApplyAsync() {
            foreach (var item in _tempState.ToList()) {
                if (string.IsNullOrEmpty(item.Value)) {
                    _logger.Information("Removed {Key}", item.Key);
                }
                else {
                    _logger.Information("Added {Key} {Value}", item.Key, item.Value);
                }
                _tempState.Remove(item.Key);
            }
            return Task.CompletedTask;
        }

        private readonly Dictionary<string, string> _tempState;
        private readonly IPublisher _publisher;
        private readonly ILogger _logger;
    }
}
