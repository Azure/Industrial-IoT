// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Client for Activation services in supervisor
    /// </summary>
    public sealed class TwinModuleActivationClient : IActivationServices<EndpointRegistrationModel> {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public TwinModuleActivationClient(IMethodClient client, IJsonSerializer serializer,
            ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task ActivateEndpointAsync(EndpointRegistrationModel registration,
            string secret, CancellationToken ct) {
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
            await CallServiceOnSupervisorAsync("ActivateEndpoint_V2", registration.SupervisorId, new {
                registration.Id,
                Secret = secret
            }, ct);
        }

        /// <inheritdoc/>
        public async Task DeactivateEndpointAsync(EndpointRegistrationModel registration,
            CancellationToken ct) {
            if (registration == null) {
                throw new ArgumentNullException(nameof(registration));
            }
            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentNullException(nameof(registration.SupervisorId));
            }
            if (string.IsNullOrEmpty(registration.Id)) {
                throw new ArgumentNullException(nameof(registration.Id));
            }
            await CallServiceOnSupervisorAsync("DeactivateEndpoint_V2", registration.SupervisorId,
                registration.Id, ct);
        }

        /// <summary>
        /// Helper to invoke service
        /// </summary>
        /// <param name="service"></param>
        /// <param name="supervisorId"></param>
        /// <param name="payload"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task CallServiceOnSupervisorAsync(string service, string supervisorId,
            object payload, CancellationToken ct) {
            var sw = Stopwatch.StartNew();
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId,
                out var moduleId);
            var result = await _client.CallMethodAsync(deviceId, moduleId, service,
                _serializer.SerializeToString(payload), null, ct);
            _logger.Debug("Calling supervisor service '{service}' on " +
                "{deviceId}/{moduleId} took {elapsed} ms.", service, deviceId,
                moduleId, sw.ElapsedMilliseconds);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
