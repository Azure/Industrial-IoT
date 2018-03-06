// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Controllers {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.Devices.Edge;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor settings controller
    /// </summary>
    [Version(1)]
    public class OpcUaSupervisorSettings : IOpcUaSupervisorSettings, ISettingsController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="supervisor"></param>
        /// <param name="logger"></param>
        public OpcUaSupervisorSettings(IOpcUaSupervisorServices supervisor,
            IOpcUaDiscoveryServices discovery, ILogger logger) {
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Set secret key for endpoint to start or stop twin host
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public Task SetAsync(string endpointId, JToken secret) {
            string key;
            switch (secret.Type) {
                case JTokenType.Null:
                    key = null;
                    break;
                case JTokenType.String:
                    key = (string)secret;
                    break;
                default:
                    throw new NotSupportedException("bad key value");
            }
            if (string.IsNullOrEmpty(key)) {
                return _supervisor.StopTwinAsync(endpointId);
            }
            return _supervisor.StartTwinAsync(endpointId, key);
        }

        /// <summary>
        /// Called based on the reported type property
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task SetTypeAsync(string value) {
            if (value != "supervisor") {
                throw new InvalidProgramException("Not supervisor");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called to enable or disable discovery
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public Task SetDiscoveringAsync(bool enable) {
            if (enable) {
                return _discovery.StartDiscoveryAsync();
            }
            return _discovery.StopDiscoveryAsync();
        }

        private readonly IOpcUaDiscoveryServices _discovery;
        private readonly IOpcUaSupervisorServices _supervisor;
        private readonly ILogger _logger;
    }
}
