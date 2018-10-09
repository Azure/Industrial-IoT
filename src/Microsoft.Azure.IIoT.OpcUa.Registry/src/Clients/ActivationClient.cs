// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Client for Activation services in supervisor
    /// </summary>
    public sealed class ActivationClient : IActivationServices<TwinRegistrationModel> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        public ActivationClient(IIoTHubTwinServices twin, ILogger logger) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task ActivateTwinAsync(TwinRegistrationModel registration,
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
            await CallServiceOnSupervisor("ActivateTwin_V1", registration, new {
                registration.Id,
                Secret = secret
            });
        }

        /// <inheritdoc/>
        public async Task DeactivateTwinAsync(TwinRegistrationModel registration) {
            if (registration == null) {
                throw new ArgumentNullException(nameof(registration));
            }
            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentNullException(nameof(registration.SupervisorId));
            }
            if (string.IsNullOrEmpty(registration.Id)) {
                throw new ArgumentNullException(nameof(registration.Id));
            }
            await CallServiceOnSupervisor("DeactivateTwin_V1", registration,
                registration.Id);
        }

        /// <summary>
        /// Helper to invoke service
        /// </summary>
        /// <param name="service"></param>
        /// <param name="registration"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private async Task CallServiceOnSupervisor(string service,
            TwinRegistrationModel registration, object payload) {
            var sw = Stopwatch.StartNew();
            var deviceId = SupervisorModelEx.ParseDeviceId(registration.SupervisorId,
                out var moduleId);
            var result = await _twin.CallMethodAsync(deviceId, moduleId,
                new MethodParameterModel {
                    Name = service,
                    JsonPayload = JsonConvertEx.SerializeObject(payload)
                });
            _logger.Debug($"Calling supervisor service '{service}' on {deviceId}/{moduleId} " +
                $"took {sw.ElapsedMilliseconds} ms and returned {result.Status}!");
            if (result.Status != 200) {
                throw new MethodCallStatusException(result.Status, result.JsonPayload);
            }
        }

        private readonly IIoTHubTwinServices _twin;
        private readonly ILogger _logger;
    }
}
