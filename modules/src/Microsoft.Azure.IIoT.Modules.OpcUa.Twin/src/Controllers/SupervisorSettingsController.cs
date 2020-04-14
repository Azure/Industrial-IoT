// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Controllers {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

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
        /// Called to start or remove twins
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public VariantValue this[string endpointId] {
            set {
                if (value.IsNull()) {
                    _endpoints.AddOrUpdate(endpointId, null);
                    return;
                }
                if (!value.IsString ||
                    !value.ToString().IsBase64()) {
                    return;
                }
                _endpoints.AddOrUpdate(endpointId, value);
            }
            get {
                if (!_endpoints.TryGetValue(endpointId, out var result)) {
                    result = null;
                }
                return result;
            }
        }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="supervisor"></param>
        /// <param name="logger"></param>
        public SupervisorSettingsController(ISupervisorServices supervisor, ILogger logger) {
            _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _endpoints = new Dictionary<string, VariantValue>();
        }


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
                if (string.IsNullOrEmpty((string)item.Value)) {
                    try {
                        await _supervisor.DetachEndpointAsync(item.Key);
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Error detaching twin {Key}", item.Key);
                    }
                }
                else {
                    try {
                        await _supervisor.AttachEndpointAsync(item.Key, (string)item.Value);
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Error attaching twin {Key}", item.Key);
                    }
                }
                _endpoints.Remove(item.Key);
            }
        }

        private readonly Dictionary<string, VariantValue> _endpoints;
        private readonly ISupervisorServices _supervisor;
        private readonly ILogger _logger;
    }
}
