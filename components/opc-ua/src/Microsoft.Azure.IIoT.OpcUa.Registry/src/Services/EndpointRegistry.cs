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
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
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
        IEndpointBulkProcessor, IApplicationRegistryListener, IEndpointActivation, IDisposable {

        /// <summary>
        /// Create endpoint registry
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        /// <param name="activator"></param>
        /// <param name="certificates"></param>
        /// <param name="supervisors"></param>
        /// <param name="serializer"></param>
        /// <param name="events"></param>
        public EndpointRegistry(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            IRegistryEventBroker<IEndpointRegistryListener> broker,
            IActivationServices<EndpointRegistrationModel> activator,
            ICertificateServices<EndpointRegistrationModel> certificates,
            ISupervisorRegistry supervisors, ILogger logger,
            IRegistryEvents<IApplicationRegistryListener> events = null) {

            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
            _supervisors = supervisors ?? throw new ArgumentNullException(nameof(supervisors));
            _activator = activator ?? throw new ArgumentNullException(nameof(activator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Endpoint}'";
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
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Endpoint}' ";

            if (!(model?.IncludeNotSeenSince ?? false)) {
                // Scope to non deleted twins
                query += $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)}) ";
            }
            if (model?.Url != null) {
                // If Url provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.EndpointUrlLC)} = " +
                    $"'{model.Url.ToLowerInvariant()}' ";
            }
            if (model?.ApplicationId != null) {
                // If application id provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.ApplicationId)} = " +
                    $"'{model.ApplicationId}' ";
            }
            if (model?.SupervisorId != null) {
                // If supervisor provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.SupervisorId)} = " +
                    $"'{model.SupervisorId}' ";
            }
            if (model?.DiscovererId != null) {
                // If discoverer provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.DiscovererId)} = " +
                    $"'{model.DiscovererId}' ";
            }
            if (model?.SiteOrGatewayId != null) {
                // If site or gateway provided, include it in search
                query += $"AND tags.{nameof(EntityRegistration.SiteOrGatewayId)} = " +
                    $"'{model.SiteOrGatewayId}' ";
            }
            if (model?.Certificate != null) {
                // If cert thumbprint provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.Thumbprint)} = " +
                    $"{model.Certificate} ";
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
                }
                else {
                    query += $"AND connectionState != 'Connected' ";
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
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string endpointId, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentException(nameof(endpointId));
            }

            // Get existing endpoint - get should always throw
            var twin = await _iothub.GetAsync(endpointId, null, ct);

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

            var endpoint = registration.ToServiceModel();
            var rawCertificates = await _certificates.GetEndpointCertificateAsync(
                endpoint.Registration, ct);
            return rawCertificates.ToCertificateChain();
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

            if (registration.IsDisabled ?? false) {
                throw new ResourceInvalidStateException(
                    $"{endpointId} is disabled - cannot activate");
            }

            if (!(registration.Activated ?? false)) {
                // Activate if not yet activated
                try {
                    if (string.IsNullOrEmpty(registration.SupervisorId)) {
                        throw new ArgumentException(
                            $"Twin {endpointId} not registered with a supervisor.");
                    }
                    await ActivateAsync(registration, context, ct);
                }
                catch (Exception) {
                    // Try other supervisors as candidates
                    if (!await ActivateAsync(registration, null, context, ct)) {
                        throw;
                    }
                }
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
                    var update = endpoint.ToEndpointRegistration(_serializer, false);
                    await _iothub.PatchAsync(registration.Patch(update, _serializer));
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
                        var update = endpoint.ToEndpointRegistration(_serializer, true);
                        await _iothub.PatchAsync(registration.Patch(update, _serializer));
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
            string applicationId, ApplicationInfoModel application) {
            // Get all endpoint registrations and for each one, call delete, if failure,
            // stop half way and throw and do not complete.
            var endpoints = await GetEndpointsAsync(applicationId, true);
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
                await _broker.NotifyAllAsync(l => l.OnEndpointDeletedAsync(context,
                    endpoint.Registration.Id, endpoint));
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
            DiscoveryResultModel result, string discovererId, string supervisorId,
            string applicationId, bool hardDelete) {

            if (newEndpoints == null) {
                throw new ArgumentNullException(nameof(newEndpoints));
            }

            var context = result.Context.Validate();

            var found = newEndpoints
                .Select(e => e.ToEndpointRegistration(_serializer, false,
                    discovererId, supervisorId))
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

            if (!(result.RegisterOnly ?? false)) {
                // Remove or disable an endpoint
                foreach (var item in remove) {
                    try {
                        // Only touch applications the discoverer owns.
                        if (item.DiscovererId == discovererId) {
                            if (hardDelete) {
                                var device = await _iothub.GetAsync(item.DeviceId);
                                // First we update any registration
                                var existingEndpoint = device.ToEndpointRegistration(false);
                                if (!string.IsNullOrEmpty(existingEndpoint.SupervisorId)) {
                                    await ClearSupervisorTwinSecretAsync(device.Id,
                                        existingEndpoint.SupervisorId);
                                }
                                // Then hard delete...
                                await _iothub.DeleteAsync(item.DeviceId);
                                await _broker.NotifyAllAsync(l => l.OnEndpointDeletedAsync(context,
                                    item.DeviceId, item.ToServiceModel()));
                            }
                            else if (!(item.IsDisabled ?? false)) {
                                var endpoint = item.ToServiceModel();
                                var update = endpoint.ToEndpointRegistration(_serializer, true);
                                await _iothub.PatchAsync(item.Patch(update, _serializer), true);
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
            }

            // Update endpoints that were disabled
            foreach (var exists in unchange) {
                try {
                    if (exists.DiscovererId == null || exists.DiscovererId == discovererId ||
                        (exists.IsDisabled ?? false)) {
                        // Get the new one we will patch over the existing one...
                        var patch = change.First(x =>
                            EndpointRegistrationEx.Logical.Equals(x, exists));
                        if (exists.Activated ?? false) {
                            patch.ActivationState = exists.ActivationState;
                        }
                        else {
                            await ApplyActivationFilterAsync(result.DiscoveryConfig?.ActivationFilter,
                                patch, context);
                        }
                        if (exists != patch) {
                            await _iothub.PatchAsync(exists.Patch(patch, _serializer), true);
                            var endpoint = patch.ToServiceModel();

                            // await _broker.NotifyAllAsync(
                            //     l => l.OnEndpointUpdatedAsync(context, endpoint));
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
                    await _iothub.CreateOrUpdateAsync(item.ToDeviceTwin(_serializer), true);

                    var endpoint = item.ToServiceModel();
                    await _broker.NotifyAllAsync(l => l.OnEndpointNewAsync(context, endpoint));
                    await _broker.NotifyAllAsync(l => l.OnEndpointEnabledAsync(context, endpoint));
                    added++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error(ex, "Exception adding endpoint from discovery.");
                }
            }

            if (added != 0 || removed != 0) {
                _logger.Information("processed endpoint results: {added} endpoints added, {updated} " +
                    "updated, {removed} removed or disabled, and {unchanged} unchanged.",
                    added, updated, removed, unchanged);
            }
        }

        /// <inheritdoc/>
        public async Task SynchronizeActivationAsync(CancellationToken ct) {

            // Find all endpoints that are activated but not connected
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(EndpointRegistration.Activated)} = true AND " +
                $"NOT IS_DEFINED(tags.{nameof(EndpointRegistration.IsDisabled)}) AND " +
                $"connectionState != 'Connected' AND " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Endpoint}' ";

            var result = new List<DeviceTwinModel>();
            string continuation = null;
            do {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, null, null, ct);

                foreach (var endpoint in devices.Items
                    .Select(d => d.ToEndpointRegistration(false))) {

                    if (!string.IsNullOrEmpty(endpoint.SupervisorId)) {
                        try {
                            await ActivateAsync(endpoint, null, ct);
                            continue;
                        }
                        catch (Exception ex) {
                            _logger.Debug(ex, "Failed to activate disconnected twin - continue...");
                        }
                    }

                    var supervisorsThatWereManagingEndpoint = await Try.Async(() =>
                        ClearSupervisorTwinSecretAsync(endpoint.Id, endpoint.SupervisorId, ct));

                    // Try activate and assign a new supervisor
                    await ActivateAsync(endpoint, supervisorsThatWereManagingEndpoint, null, ct);
                }
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
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
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Endpoint}' ";

            if (!includeDeleted) {
                query += $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
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
                // Ensure device scope for the registration before getting the secret.
                // Changing device's scope regenerates the secret.
                await EnsureDeviceScopeForRegistrationAsync(registration, ct);

                // Get endpoint twin secret
                var secret = await _iothub.GetPrimaryKeyAsync(registration.DeviceId, null, ct);

                var endpoint = registration.ToServiceModel();

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
        /// Try to activate endpoint on any supervisor in site
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="additional"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<bool> ActivateAsync(EndpointRegistration endpoint,
            IEnumerable<string> additional, RegistryOperationContextModel context,
            CancellationToken ct) {
            // Get site of this endpoint
            var site = endpoint.SiteId;
            if (string.IsNullOrEmpty(site)) {
                // Use discovery id gateway part if no site found
                site = DiscovererModelEx.ParseDeviceId(endpoint.DiscovererId, out _);
                if (string.IsNullOrEmpty(site)) {
                    // Try supervisor id gateway part
                    site = DiscovererModelEx.ParseDeviceId(endpoint.SupervisorId, out _);
                }
            }

            // Get all supervisors in site
            endpoint.SiteId = site;
            var supervisorsInSite = await _supervisors.QueryAllSupervisorsAsync(
                new SupervisorQueryModel { SiteId = site });
            var candidateSupervisors = supervisorsInSite.Select(s => s.Id)
                .ToList().Shuffle();

            // Add all supervisors that managed this endpoint before.
            // TODO: Consider removing as it is a source of bugs
            if (additional != null) {
                candidateSupervisors.AddRange(additional);
            }

            // Remove previously failing one
            candidateSupervisors.Remove(endpoint.SupervisorId);
            // Loop through all randomly and try to take one that works.
            foreach (var supervisorId in candidateSupervisors) {
                endpoint.SupervisorId = supervisorId;
                endpoint.Activated = false;
                try {
                    await ActivateAsync(endpoint, context, ct);
                    _logger.Information("Activate twin on supervisor {supervisorId}!",
                        supervisorId);
                    // Done - endpoint was also patched thus has new supervisor id
                    return true;
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Failed to activate twin on supervisor {supervisorId} " +
                        "- trying next...", supervisorId);
                }
            }
            // Failed
            return false;
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

            // Ensure device scope for the registration before getting the secret.
            // Changing device's scope regenerates the secret.
            await EnsureDeviceScopeForRegistrationAsync(registration, ct);

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
                await Try.Async(() => ClearSupervisorTwinSecretAsync(
                    registration.DeviceId, registration.SupervisorId));
                _logger.Error(ex, "Failed to activate twin");
                throw;
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
            await ClearSupervisorTwinSecretAsync(registration.DeviceId,
                registration.SupervisorId, ct);

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
        /// Remove supervisor twin secret from all supervisors managing the endpoint.
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="supervisorId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IEnumerable<string>> ClearSupervisorTwinSecretAsync(
            string twinId, string supervisorId = null, CancellationToken ct = default) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            // Cleanup and remove endpoint from all supervisors
            var supervisors = await _supervisors.QueryAllSupervisorsAsync(
                new SupervisorQueryModel { EndpointId = twinId });
            var items = supervisors.Select(s => s.Id);
            if (!string.IsNullOrEmpty(supervisorId)) {
                items.Append(supervisorId);
            }
            var results = items.Distinct().ToList();
            foreach (var supervisor in results) {
                try {
                    var deviceId = SupervisorModelEx.ParseDeviceId(supervisor, out var moduleId);
                    // Remove from supervisor - this disconnects the device
                    await _iothub.UpdatePropertyAsync(deviceId, moduleId, twinId, null, ct);
                    _logger.Information("Twin {twinId} deactivated on {supervisorId}.",
                        twinId, supervisor);
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Twin {twinId} failed to deactivate on {supervisorId}.",
                        twinId, supervisor);
                }
            }
            return results;
        }

        /// <summary>
        /// Enable twin on supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="twinId"></param>
        /// <param name="secret"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task SetSupervisorTwinSecretAsync(string supervisorId,
            string twinId, string secret, CancellationToken ct = default) {

            if (string.IsNullOrEmpty(supervisorId)) {
                return; // ok, no supervisor
            }
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (string.IsNullOrEmpty(secret)) {
                throw new ArgumentNullException(nameof(secret));
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out var moduleId);
            // Update supervisor to start supervising this endpoint
            await _iothub.UpdatePropertyAsync(deviceId, moduleId, twinId, secret, ct);
            _logger.Information("Twin {twinId} activated on {supervisorId}.",
                twinId, supervisorId);
        }


        /// <summary>
        /// Ensure device scope for registration
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task EnsureDeviceScopeForRegistrationAsync(
            EndpointRegistration registration, CancellationToken ct = default) {

            // Ensure device scope is set to the owning edge gateway before activation
            var edgeScope = await GetSupervisorDeviceScopeAsync(registration.SupervisorId, ct);
            var deviceTwin = await _iothub.GetAsync(registration.DeviceId, ct: ct);
            if (!string.IsNullOrEmpty(edgeScope) && deviceTwin.DeviceScope != edgeScope) {
                deviceTwin.DeviceScope = edgeScope;
                await _iothub.CreateOrUpdateAsync(deviceTwin, true, ct: ct);
            }
        }

        /// <summary>
        /// Get device scope of the supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<string> GetSupervisorDeviceScopeAsync(string supervisorId,
            CancellationToken ct = default) {
            if (string.IsNullOrEmpty(supervisorId)) {
                return null; // No scope
            }
            var edgeDeviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out _);
            var edgeDeviceTwin = await _iothub.FindAsync(edgeDeviceId, ct: ct);
            return edgeDeviceTwin?.DeviceScope;
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
            var registration = twin.ToEntityRegistration(onlyServerState) as EndpointRegistration;
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
        private readonly ICertificateServices<EndpointRegistrationModel> _certificates;
        private readonly ISupervisorRegistry _supervisors;
        private readonly IRegistryEventBroker<IEndpointRegistryListener> _broker;
        private readonly IJsonSerializer _serializer;
        private readonly Action _unregister;
        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
