// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.Module.Framework;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using System.Linq;

    /// <summary>
    /// Supervisor settings controller
    /// </summary>
    [Version(1)]
    public class OpcUaSupervisorSettings : ISettingsController {

        /// <summary>
        /// Called based on the reported connected property.
        /// </summary>
        public bool Connected { get; set; }

        /// <summary>
        /// Called to start or remove twins
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public JToken this[string endpointId] {
            set {
                if (value == null) {
                    _endpoints.Remove(endpointId);
                    return;
                }
                if (value.Type != JTokenType.String ||
                    !value.ToString().IsBase64()) {
                    return;
                }
                if (!_endpoints.ContainsKey(endpointId)) {
                    _endpoints.Add(endpointId, value.ToString());
                }
                else {
                    _endpoints[endpointId] = value.ToString();
                }
            }
            get {
                if (!_endpoints.TryGetValue(endpointId, out var result)) {
                    return JValue.CreateNull();
                }
                return result;
            }
        }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="supervisor"></param>
        /// <param name="logger"></param>
        public OpcUaSupervisorSettings(IOpcUaSupervisorServices supervisor, ILogger logger) {
            _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _endpoints = new Dictionary<string, string>();
        }

        /// <summary>
        /// Apply changes
        /// </summary>
        /// <returns></returns>
        public async Task ApplyAsync() {
            foreach (var item in _endpoints.ToList()) {
                if (string.IsNullOrEmpty(item.Value)) {
                    try {
                        await _supervisor.StopTwinAsync(item.Key);
                    }
                    catch (Exception ex) {
                        _logger.Error($"Error stopping twin {item.Key}", () => ex);
                    }
                }
                else {
                    try {
                        if (item.Value.IsBase64()) {
                            await _supervisor.StartTwinAsync(item.Key, item.Value);
                            continue;
                        }
                    }
                    catch (Exception ex) {
                        _logger.Error($"Error starting twin {item.Key}", () => ex);
                    }
                }
                _endpoints.Remove(item.Value);
            }
            _endpoints.Clear();
        }

        private readonly Dictionary<string, string> _endpoints;
        private readonly IOpcUaSupervisorServices _supervisor;
        private readonly ILogger _logger;
    }
}
