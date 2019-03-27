// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Serilog;

    /// <summary>
    /// Client for Activation services in supervisor
    /// </summary>
    public sealed class ActivationClient : IActivationServices<EndpointRegistrationModel> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public ActivationClient(IMethodClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task ActivateEndpointAsync(EndpointRegistrationModel registration,
            string secret) {
            if (registration == null) {
                throw new ArgumentNullException(nameof(registration));
            }
            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentNullException(nameof(registration.SupervisorId));
            }
            if (string.IsNullOrEmpty(registration.Id)) {
                throw new ArgumentNullException(nameof(registration.Id));
            }
            if (string.IsNullOrEmpty(secret)) {
                throw new ArgumentNullException(nameof(secret));
            }
            if (!secret.IsBase64()) {
                throw new ArgumentException("not base64", nameof(secret));
            }
            await CallServiceOnSupervisor("ActivateEndpoint_V2", registration.SupervisorId, new {
                registration.Id,
                Secret = secret
            });
        }

        /// <inheritdoc/>
        public async Task DeactivateEndpointAsync(EndpointRegistrationModel registration) {
            if (registration == null) {
                throw new ArgumentNullException(nameof(registration));
            }
            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentNullException(nameof(registration.SupervisorId));
            }
            if (string.IsNullOrEmpty(registration.Id)) {
                throw new ArgumentNullException(nameof(registration.Id));
            }
            await CallServiceOnSupervisor("DeactivateEndpoint_V2", registration.SupervisorId,
                registration.Id);
        }

        /// <summary>
        /// Helper to invoke service
        /// </summary>
        /// <param name="service"></param>
        /// <param name="supervisorId"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private async Task CallServiceOnSupervisor(string service, string supervisorId,
            object payload) {
            var sw = Stopwatch.StartNew();
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId,
                out var moduleId);
            var result = await _client.CallMethodAsync(deviceId, moduleId, service,
                JsonConvertEx.SerializeObject(payload));
            _logger.Debug("Calling supervisor service '{service}' on " +
                "{deviceId}/{moduleId} took {elapsed} ms.", service, deviceId,
                moduleId, sw.ElapsedMilliseconds);
        }

        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
