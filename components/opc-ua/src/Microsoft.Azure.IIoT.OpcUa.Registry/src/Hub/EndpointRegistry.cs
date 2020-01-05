// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Endpoint registry services using the IoT Hub twin services for endpoint
    /// identity registration/retrieval.
    /// </summary>
    public sealed class EndpointRegistry : IEndpointRegistry, IApplicationEndpointRegistry,
        IEndpointBulkProcessor, IApplicationRegistryListener, IDisposable {

        /// <summary>
        /// Create endpoint registry
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        /// <param name="activator"></param>
        /// <param name="events"></param>
        public EndpointRegistry(IIoTHubTwinServices iothub, IEndpointEventBroker broker,
            ILogger logger, IActivationServices<EndpointRegistrationModel> activator,
            IApplicationRegistryEvents events = null) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activator = activator ?? throw new ArgumentNullException(nameof(activator));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));

            // Register for application registry events
            _unregister = events?.Register(this);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _unregister?.Invoke();
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            bool onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentException(nameof(endpointId));
            }
            var device = await _iothub.GetAsync(endpointId, null, ct);
            return TwinModelToEndpointRegistrationModel(device, onlyServerState, false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(string continuation,
            bool onlyServerState, int? pageSize, CancellationToken ct) {
            // Find all devices where endpoint information is configured
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(BaseRegistration.DeviceType)} = 'Endpoint' " +
                $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct);

            return new EndpointInfoListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(d => TwinModelToEndpointRegistrationModel(d, onlyServerState, true))
                    .Where(x => x != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel model, bool onlyServerState, int? pageSize,
            CancellationToken ct) {

            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EndpointRegistration.DeviceType)} = 'Endpoint' ";

            if (!(model?.IncludeNotSeenSince ?? false)) {
                // Scope to non deleted twins
                query += $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)}) ";
            }
            if (model?.Url != null) {
                // If Url provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.EndpointUrlLC)} = " +
                    $"'{model.Url.ToLowerInvariant()}' ";
            }
            if (model?.Certificate != null) {
                // If cert provided, include it in search
                query += $"AND tags.{nameof(BaseRegistration.Thumbprint)} = " +
                    $"{model.Certificate.ToSha1Hash()} ";
            }
            if (model?.SecurityMode != null) {
                // If SecurityMode provided, include it in search
                query += $"AND properties.desired.{nameof(EndpointRegistration.SecurityMode)} = " +
                    $"'{model.SecurityMode}' ";
            }
            if (model?.SecurityPolicy != null) {
                // If SecurityPolicy uri provided, include it in search
                query += $"AND properties.desired.{nameof(EndpointRegistration.SecurityPolicy)} = " +
                    $"'{model.SecurityPolicy}' ";
            }
            if (model?.EndpointState != null && model?.Connected != false && model?.Activated != false) {
                query += $"AND properties.reported.{nameof(EndpointRegistration.State)} = " +
                    $"'{model.EndpointState}' ";

                // Force query for activated and connected
                model.Connected = true;
                model.Activated = true;
            }
            if (model?.Activated != null) {
                // If flag provided, include it in search
                if (model.Activated.Value) {
                    query += $"AND tags.{nameof(EndpointRegistration.Activated)} = true ";
                }
                else {
                    query += $"AND (tags.{nameof(EndpointRegistration.Activated)} != true " +
                        $"OR NOT IS_DEFINED(tags.{nameof(EndpointRegistration.Activated)})) ";
                }
            }
            if (model?.Connected != null) {
                // If flag provided, include it in search
                if (model.Connected.Value) {
                    query += $"AND connectionState = 'Connected' ";
                    // Do not use connected property as module might have exited before updating.
                }
                else {
                    query += $"AND (connectionState = 'Disconnected' " +
                        $"OR properties.reported.{TwinProperty.Connected} != true) ";
                }
            }
            var result = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize, ct);
            return new EndpointInfoListModel {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(t => t.ToEndpointRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
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
            var registration = twin.ToRegistration(true) as EndpointRegistration;
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
                await ActivateAsync(registration, context, ct);
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
            var registration = twin.ToRegistration(true) as EndpointRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{endpointId} is not an endpoint registration.");
            }

            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentException(
                    $"Twin {endpointId} not registered with a supervisor.");
            }

            if (registration.Activated ?? false) {
                await DeactivateAsync(registration, context, ct);
            }
        }

        /// <inheritdoc/>
        public Task OnApplicationNewAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnApplicationUpdatedAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task OnApplicationEnabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            var endpoints = await GetEndpointsAsync(application.ApplicationId, true);
            foreach (var registration in endpoints) {
                // Enable if disabled
                if (!(registration.IsDisabled ?? false)) {
                    continue;
                }
                try {
                    var endpoint = registration.ToServiceModel();
                    endpoint.NotSeenSince = null;
                    var update = endpoint.ToEndpointRegistration(false);
                    await _iothub.PatchAsync(registration.Patch(update));
                    await _broker.NotifyAllAsync(
                        l => l.OnEndpointEnabledAsync(context, endpoint));
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed re-enabling endpoint {id}",
                        registration.Id);
                    continue;
                }
                // Activate if it was activated before
                if (!(registration.Activated ?? false)) {
                    continue; // No need to re-activate on enable
                }
                try {
                    await ActivateAsync(registration, context);
                }
                catch (Exception ex) {
                    _logger.Error(ex,
                        "Failed activating re-enabled endpoint {id}", registration.Id);
                }
            }
        }

        /// <inheritdoc/>
        public async Task OnApplicationDisabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            // Disable endpoints
            var endpoints = await GetEndpointsAsync(application.ApplicationId, true);
            foreach (var registration in endpoints) {
                if (registration.Activated ?? false) {
                    try {
                        registration.Activated = false; // Prevent patching...
                        await DeactivateAsync(registration, context);
                        registration.Activated = true; // reset activated state
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Failed deactivating disabled endpoint {id}",
                            registration.Id);
                    }
                }
                // Disable if enabled
                if (!(registration.IsDisabled ?? false)) {
                    try {
                        var endpoint = registration.ToServiceModel();
                        endpoint.NotSeenSince = DateTime.UtcNow;
                        var update = endpoint.ToEndpointRegistration(true);
                        await _iothub.PatchAsync(registration.Patch(update));
                        await _broker.NotifyAllAsync(
                            l => l.OnEndpointDisabledAsync(context, endpoint));
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Failed disabling endpoint {id}", registration.Id);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task OnApplicationDeletedAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            // Get all endpoint registrations and for each one, call delete, if failure,
            // stop half way and throw and do not complete.
            var endpoints = await GetEndpointsAsync(application.ApplicationId, true);
            foreach (var registration in endpoints) {
                var endpoint = registration.ToServiceModel();
                try {
                    registration.Activated = false; // Prevents patching since we delete below.
                    await DeactivateAsync(registration, context);
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed deleting registered endpoint endpoint {id}",
                        registration.Id);
                }
                await _iothub.DeleteAsync(registration.DeviceId);
                await _broker.NotifyAllAsync(l => l.OnEndpointDeletedAsync(context, endpoint));
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<EndpointInfoModel>> GetApplicationEndpoints(
            string applicationId, bool includeDeleted, bool filterInactiveTwins, CancellationToken ct) {
            // Include deleted twins if the application itself is deleted.  Otherwise omit.
            var endpoints = await GetEndpointsAsync(applicationId, includeDeleted, ct);
            return endpoints
                .Where(e => !filterInactiveTwins || (e.Connected && (e.Activated ?? false)))
                .Select(e => e.ToServiceModel());
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryEventsAsync(IEnumerable<EndpointInfoModel> newEndpoints,
            DiscoveryResultModel result, string supervisorId, string applicationId, bool hardDelete) {

            if (newEndpoints == null) {
                throw new ArgumentNullException(nameof(newEndpoints));
            }

            var context = result.Context.Validate();

            var found = newEndpoints
                .Select(e => e.ToEndpointRegistration(false))
                .ToList();

            var existing = Enumerable.Empty<EndpointRegistration>();
            if (!string.IsNullOrEmpty(applicationId)) {
                // Merge with existing endpoints of the application
                existing = await GetEndpointsAsync(applicationId, true);
            }

            var remove = new HashSet<EndpointRegistration>(existing,
                EndpointRegistrationEx.Logical);
            var add = new HashSet<EndpointRegistration>(found,
                EndpointRegistrationEx.Logical);
            var unchange = new HashSet<EndpointRegistration>(existing,
                EndpointRegistrationEx.Logical);
            var change = new HashSet<EndpointRegistration>(found,
                EndpointRegistrationEx.Logical);

            unchange.IntersectWith(add);
            change.IntersectWith(remove);
            remove.ExceptWith(found);
            add.ExceptWith(existing);

            var added = 0;
            var updated = 0;
            var unchanged = 0;
            var removed = 0;

            // Remove or disable an endpoint
            foreach (var item in remove) {
                try {
                    // Only touch applications the supervisor owns.
                    if (item.SupervisorId == supervisorId) {
                        if (hardDelete) {
                            var device = await _iothub.GetAsync(item.DeviceId);
                            // First we update any supervisor registration
                            var existingEndpoint = device.ToEndpointRegistration(false);
                            if (!string.IsNullOrEmpty(existingEndpoint.SupervisorId)) {
                                await SetSupervisorTwinSecretAsync(existingEndpoint.SupervisorId,
                                    device.Id, null);
                            }
                            // Then hard delete...
                            await _iothub.DeleteAsync(item.DeviceId);
                            await _broker.NotifyAllAsync(l => l.OnEndpointDeletedAsync(context,
                                item.ToServiceModel()));
                        }
                        else if (!(item.IsDisabled ?? false)) {
                            var endpoint = item.ToServiceModel();
                            var update = endpoint.ToEndpointRegistration(true);
                            await _iothub.PatchAsync(item.Patch(update), true);
                            await _broker.NotifyAllAsync(
                                l => l.OnEndpointDisabledAsync(context, endpoint));
                        }
                        else {
                            unchanged++;
                            continue;
                        }
                        removed++;
                    }
                    else {
                        // Skip the ones owned by other supervisors
                        unchanged++;
                    }
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error(ex, "Exception during discovery removal.");
                }
            }

            // Update endpoints that were disabled
            foreach (var exists in unchange) {
                try {
                    if (exists.SupervisorId == null || exists.SupervisorId == supervisorId ||
                        (exists.IsDisabled ?? false)) {
                        // Get the new one we will patch over the existing one...
                        var patch = change.First(x =>
                            EndpointRegistrationEx.Logical.Equals(x, exists));
                        await ApplyActivationFilterAsync(result.DiscoveryConfig?.ActivationFilter,
                            patch, context);
                        if (exists != patch) {
                            await _iothub.PatchAsync(exists.Patch(patch), true);
                            var endpoint = patch.ToServiceModel();
                            await _broker.NotifyAllAsync(
                                l => l.OnEndpointUpdatedAsync(context, endpoint));
                            if (exists.IsDisabled ?? false) {
                                await _broker.NotifyAllAsync(
                                    l => l.OnEndpointEnabledAsync(context, endpoint));
                            }
                            updated++;
                            continue;
                        }
                    }
                    unchanged++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error(ex, "Exception during update.");
                }
            }

            // Add endpoint
            foreach (var item in add) {
                try {
                    await ApplyActivationFilterAsync(result.DiscoveryConfig?.ActivationFilter,
                        item, context);
                    await _iothub.CreateAsync(item.ToDeviceTwin(), true);

                    var endpoint = item.ToServiceModel();
                    await _broker.NotifyAllAsync(l => l.OnEndpointNewAsync(context, endpoint));
                    await _broker.NotifyAllAsync(l => l.OnEndpointEnabledAsync(context, endpoint));
                    added++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error(ex, "Exception during discovery addition.");
                }
            }

            if (added != 0 || removed != 0) {
                _logger.Information("processed endpoint results: {added} endpoints added, {updated} " +
                    "updated, {removed} removed or disabled, and {unchanged} unchanged.",
                    added, updated, removed, unchanged);
            }
        }

        /// <summary>
        /// Get all endpoints for application id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="includeDeleted"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IEnumerable<EndpointRegistration>> GetEndpointsAsync(
            string applicationId, bool includeDeleted, CancellationToken ct = default) {
            // Find all devices where endpoint information is configured
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(EndpointRegistration.ApplicationId)} = " +
                    $"'{applicationId}' AND " +
                $"tags.{nameof(BaseRegistration.DeviceType)} = 'Endpoint' ";

            if (!includeDeleted) {
                query += $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
            }

            var result = new List<DeviceTwinModel>();
            string continuation = null;
            do {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, null, null, ct);
                result.AddRange(devices.Items);
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
            return result
                .Select(d => d.ToEndpointRegistration(false))
                .Where(r => r != null);
        }

        /// <summary>
        /// Apply activation filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="registration"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<string> ApplyActivationFilterAsync(
            EndpointActivationFilterModel filter, EndpointRegistration registration,
            RegistryOperationContextModel context, CancellationToken ct = default) {
            if (filter == null || registration == null) {
                return null;
            }
            // TODO: Get trust list entry and validate endpoint.Certificate
            var mode = registration.SecurityMode ?? SecurityMode.None;
            if (!mode.MatchesFilter(filter.SecurityMode ?? SecurityMode.Best)) {
                return null;
            }
            var policy = registration.SecurityPolicy;
            if (filter.SecurityPolicies != null) {
                if (!filter.SecurityPolicies.Any(p =>
                    p.EqualsIgnoreCase(registration.SecurityPolicy))) {
                    return null;
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
                return secret;
            }
            catch (Exception ex) {
                _logger.Information(ex, "Failed activating {eeviceId} based off " +
                    "filter.  Manual activation required.", registration.DeviceId);
                return null;
            }
        }

        /// <summary>
        /// Activate
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ActivateAsync(EndpointRegistration registration,
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
                    var patch = endpoint.ToEndpointRegistration(registration.IsDisabled);

                    patch.Activated = true; // Mark registration as activated

                    await _iothub.PatchAsync(registration.Patch(patch), true, ct);
                }
                await _broker.NotifyAllAsync(l => l.OnEndpointActivatedAsync(context, endpoint));
            }
            catch (Exception ex) {
                // Undo activation
                await Try.Async(() => _activator.DeactivateEndpointAsync(
                    endpoint.Registration));
                await Try.Async(() => SetSupervisorTwinSecretAsync(
                    registration.SupervisorId, registration.DeviceId, null));
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
        private async Task DeactivateAsync(EndpointRegistration registration,
            RegistryOperationContextModel context, CancellationToken ct = default) {

            var endpoint = registration.ToServiceModel();

            // Deactivate twin in twin settings - do no matter what
            await SetSupervisorTwinSecretAsync(registration.SupervisorId,
                registration.DeviceId, null, ct);

            // Call down to supervisor to ensure deactivation is complete - do no matter what
            await Try.Async(() => _activator.DeactivateEndpointAsync(endpoint.Registration));

            try {
                // Mark as deactivated
                if (registration.Activated ?? false) {

                    var patch = endpoint.ToEndpointRegistration(registration.IsDisabled);

                    // Mark as deactivated
                    patch.Activated = false;

                    await _iothub.PatchAsync(registration.Patch(patch), true);
                }
                await _broker.NotifyAllAsync(l => l.OnEndpointDeactivatedAsync(context, endpoint));
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to deactivate twin");
                throw;
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
            string twinId, string secret, CancellationToken ct = default) {

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

        /// <summary>
        /// Convert device twin registration property to registration model
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState">Only desired should be returned
        /// this means that you will look at stale information.</param>
        /// <param name="skipInvalid"></param>
        /// <returns></returns>
        private static EndpointInfoModel TwinModelToEndpointRegistrationModel(
            DeviceTwinModel twin, bool onlyServerState, bool skipInvalid) {

            // Convert to twin registration
            var registration = twin.ToRegistration(onlyServerState) as EndpointRegistration;
            if (registration == null) {
                if (skipInvalid) {
                    return null;
                }
                throw new ResourceNotFoundException(
                    $"{twin.Id} is not a registered opc ua endpoint.");
            }
            return registration.ToServiceModel();
        }

        private readonly IActivationServices<EndpointRegistrationModel> _activator;
        private readonly IEndpointEventBroker _broker;
        private readonly Action _unregister;
        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
