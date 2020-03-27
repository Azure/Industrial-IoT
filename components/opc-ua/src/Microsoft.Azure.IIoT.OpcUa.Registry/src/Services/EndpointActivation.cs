// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Endpoint registry services using the IoT Hub twin services for endpoint
    /// identity registration/retrieval.
    /// </summary>
    public sealed class EndpointActivation : IEndpointActivation, IEndpointRegistryActivation {

        /// <summary>
        /// Create endpoint registry
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        /// <param name="activator"></param>
        /// <param name="serializer"></param>
        /// <param name="events"></param>
        public EndpointActivation(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            IRegistryEventBroker<IEndpointRegistryListener> broker,
            IActivationServices<EndpointRegistrationModel> activator,
            ILogger logger, IRegistryEvents<IApplicationRegistryListener> events = null) {

            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activator = activator ?? throw new ArgumentNullException(nameof(activator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        }

        /// <inheritdoc/>
        public async Task ActivateEndpointAsync(string endpointId,
            RegistryOperationContextModel context, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentException(nameof(endpointId));
            }
            context = context.Validate();

            // Get existing endpoint and compare to see if we need to patch.
            var twin = await _iothub.GetAsync(endpointId, null, ct);
            if (twin.Id != endpointId) {
                throw new ArgumentException("Id must be same as twin to activate",
                    nameof(endpointId));
            }

            // Convert to twin registration
            var registration = twin.ToEntityRegistration(true) as EndpointRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{endpointId} is not an endpoint registration.");
            }
            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentException(
                    $"Twin {endpointId} not registered with a supervisor.");
            }

            if (registration.IsDisabled ?? false) {
                throw new ResourceInvalidStateException(
                    $"{endpointId} is disabled - cannot activate");
            }

            if (!(registration.Activated ?? false)) {
                // Only activate if not yet activated
                await ActivateEndpointAsync(registration, context, ct);
            }
        }

        /// <inheritdoc/>
        public async Task DeactivateEndpointAsync(string endpointId,
            RegistryOperationContextModel context, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentException(nameof(endpointId));
            }
            context = context.Validate();
            // Get existing endpoint and compare to see if we need to patch.
            var twin = await _iothub.GetAsync(endpointId, null, ct);
            if (twin.Id != endpointId) {
                throw new ArgumentException("Id must be same as twin to deactivate",
                    nameof(endpointId));
            }
            // Convert to twin registration
            var registration = twin.ToEntityRegistration(true) as EndpointRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{endpointId} is not an endpoint registration.");
            }

            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentException(
                    $"Twin {endpointId} not registered with a supervisor.");
            }

            if (registration.Activated ?? false) {
                await DeactivateEndpointAsync(registration, context, ct);
            }
        }

        /// <summary>
        /// Activate
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task ActivateEndpointAsync(EndpointRegistration registration,
            RegistryOperationContextModel context, CancellationToken ct = default) {

            // Update supervisor settings
            var secret = await _iothub.GetPrimaryKeyAsync(registration.DeviceId, null, ct);
            var endpoint = registration.ToServiceModel();
            try {
                // Call down to supervisor to activate - this can fail
                await _activator.ActivateEndpointAsync(endpoint.Registration, secret, ct);

                // Update supervisor desired properties
                await SetSupervisorTwinSecretAsync(registration.SupervisorId,
                    registration.DeviceId, secret, ct);

                if (!(registration.Activated ?? false)) {

                    // Update twin activation status in twin settings
                    var patch = endpoint.ToEndpointRegistration(_serializer,
                        registration.IsDisabled);

                    patch.Activated = true; // Mark registration as activated

                    await _iothub.PatchAsync(registration.Patch(patch, _serializer), true, ct);
                }
                await _broker.NotifyAllAsync(l => l.OnEndpointActivatedAsync(context, endpoint));
            }
            catch (Exception ex) {
                // Undo activation
                await Try.Async(() => _activator.DeactivateEndpointAsync(
                    endpoint.Registration));
                await Try.Async(() => SetSupervisorTwinSecretAsync(
                    registration.SupervisorId, registration.DeviceId, null, CancellationToken.None));
                _logger.Error(ex, "Failed to activate twin");
                throw ex;
            }
        }

        /// <summary>
        /// Deactivate
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task DeactivateEndpointAsync(EndpointRegistration registration,
            RegistryOperationContextModel context, CancellationToken ct) {

            var endpoint = registration.ToServiceModel();

            // Deactivate twin in twin settings - do no matter what
            await SetSupervisorTwinSecretAsync(registration.SupervisorId,
                registration.DeviceId, null, ct);

            // Call down to supervisor to ensure deactivation is complete - do no matter what
            await Try.Async(() => _activator.DeactivateEndpointAsync(endpoint.Registration));

            try {
                // Mark as deactivated
                if (registration.Activated ?? false) {

                    var patch = endpoint.ToEndpointRegistration(
                        _serializer, registration.IsDisabled);

                    // Mark as deactivated
                    patch.Activated = false;

                    await _iothub.PatchAsync(registration.Patch(patch, _serializer), true);
                }
                await _broker.NotifyAllAsync(l => l.OnEndpointDeactivatedAsync(context, endpoint));
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to deactivate twin");
                throw;
            }
        }

        /// <summary>
        /// Apply activation filter
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="filter"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task ApplyActivationFilterAsync(
            EndpointRegistration registration, EndpointActivationFilterModel filter,
            RegistryOperationContextModel context, CancellationToken ct = default) {
            if (filter == null || registration == null) {
                return;
            }
            // TODO: Get trust list entry and validate endpoint.Certificate
            var mode = registration.SecurityMode ?? SecurityMode.None;
            if (!mode.MatchesFilter(filter.SecurityMode ?? SecurityMode.Best)) {
                return;
            }
            var policy = registration.SecurityPolicy;
            if (filter.SecurityPolicies != null) {
                if (!filter.SecurityPolicies.Any(p =>
                    p.EqualsIgnoreCase(registration.SecurityPolicy))) {
                    return;
                }
            }
            try {
                var endpoint = registration.ToServiceModel();
                // Get endpoint twin secret
                var secret = await _iothub.GetPrimaryKeyAsync(registration.DeviceId, null, ct);

                // Try activate endpoint - if possible...
                await _activator.ActivateEndpointAsync(endpoint.Registration, secret, ct);

                // Mark in supervisor
                await SetSupervisorTwinSecretAsync(registration.SupervisorId,
                    registration.DeviceId, secret, ct);

                registration.Activated = true;

                await _broker.NotifyAllAsync(
                    l => l.OnEndpointActivatedAsync(context, endpoint));
            }
            catch (Exception ex) {
                _logger.Information(ex, "Failed activating {eeviceId} based off " +
                    "filter.  Manual activation required.", registration.DeviceId);
            }
        }

        /// <summary>
        /// Enable or disable twin on supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="twinId"></param>
        /// <param name="secret"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task SetSupervisorTwinSecretAsync(string supervisorId,
            string twinId, string secret, CancellationToken ct) {

            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (string.IsNullOrEmpty(supervisorId)) {
                return; // ok, no supervisor
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out var moduleId);
            if (secret == null) {
                // Remove from supervisor - this disconnects the device
                await _iothub.UpdatePropertyAsync(deviceId, moduleId, twinId, null, ct);
                _logger.Information("Twin {twinId} deactivated on {supervisorId}.",
                    twinId, supervisorId);
            }
            else {
                // Update supervisor to start supervising this endpoint
                await _iothub.UpdatePropertyAsync(deviceId, moduleId, twinId, secret, ct);
                _logger.Information("Twin {twinId} activated on {supervisorId}.",
                    twinId, supervisorId);
            }
        }

        private readonly IActivationServices<EndpointRegistrationModel> _activator;
        private readonly IRegistryEventBroker<IEndpointRegistryListener> _broker;
        private readonly IJsonSerializer _serializer;
        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
