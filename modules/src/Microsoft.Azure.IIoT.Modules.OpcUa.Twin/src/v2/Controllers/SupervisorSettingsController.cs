// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Supervisor settings controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    public class SupervisorSettingsController : ISettingsController {

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
                    LogControl.Level.MinimumLevel = LogEventLevel.Information;
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
                    LogControl.Level.MinimumLevel = level;
                }
                else {
                    throw new NotSupportedException(
                        $"Bad log level value type {value.Type}");
                }
            }
            // The enum values are the same as in serilog
            get => JToken.FromObject(LogControl.Level.MinimumLevel.ToString());
        }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="supervisor"></param>
        /// <param name="logger"></param>
        public SupervisorSettingsController(ISupervisorServices supervisor,
            ILogger logger) {
            _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _endpoints = new Dictionary<string, string>();
        }

        /// <summary>
        /// Called to start or remove twins
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public JToken this[string endpointId] {
            set {
                if (value == null || value.Type == JTokenType.Null) {
                    _endpoints.AddOrUpdate(endpointId, null);
                    return;
                }
                if (value.Type != JTokenType.String ||
                    !value.ToString().IsBase64()) {
                    return;
                }
                _endpoints.AddOrUpdate(endpointId, value.ToString());
            }
            get {
                if (!_endpoints.TryGetValue(endpointId, out var result)) {
                    result = null;
                }
                return result != null ? JToken.FromObject(result) : JValue.CreateNull();
            }
        }

        /// <summary>
        /// Returns all currently existing endpoints
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetPropertyNames() {
            return _supervisor.GetStatusAsync().Result.Endpoints.Select(e => e.Id);
        }

        /// <summary>
        /// Apply changes
        /// </summary>
        /// <returns></returns>
        public async Task ApplyAsync() {
            foreach (var item in _endpoints.ToList()) {
                if (string.IsNullOrEmpty(item.Value)) {
                    try {
                        await _supervisor.DetachEndpointAsync(item.Key);
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Error detaching twin {Key}", item.Key);
                    }
                }
                else {
                    try {
                        await _supervisor.AttachEndpointAsync(item.Key, item.Value);
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Error attaching twin {Key}", item.Key);
                    }
                }
                _endpoints.Remove(item.Key);
            }
        }

        private readonly Dictionary<string, string> _endpoints;
        private readonly ISupervisorServices _supervisor;
        private readonly ILogger _logger;
    }
}
