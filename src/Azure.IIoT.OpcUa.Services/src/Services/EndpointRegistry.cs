// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services {
    using Azure.IIoT.OpcUa.Services.Models;
    using Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

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
        /// <param name="serializer"></param>
        /// <param name="events"></param>
        public EndpointRegistry(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            IRegistryEventBroker<IEndpointRegistryListener> broker, ILogger logger,
            IRegistryEvents<IApplicationRegistryListener> events = null) {

            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    await _iothub.PatchAsync(registration.Patch(update, _serializer));
                    await _broker.NotifyAllAsync(
                        l => l.OnEndpointEnabledAsync(context, endpoint));
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed re-enabling endpoint {id}",
                        registration.Id);
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task OnApplicationDisabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            // Disable endpoints
            var endpoints = await GetEndpointsAsync(application.ApplicationId, true);
            foreach (var registration in endpoints) {
                // Disable if enabled
                if (!(registration.IsDisabled ?? false)) {
                    try {
                        var endpoint = registration.ToServiceModel();
                        endpoint.NotSeenSince = DateTime.UtcNow;
                        var update = endpoint.ToEndpointRegistration(true);
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
                await _iothub.DeleteAsync(registration.DeviceId);
                var endpoint = registration.ToServiceModel();
                await _broker.NotifyAllAsync(l => l.OnEndpointDeletedAsync(context,
                    endpoint.Registration.Id, endpoint));
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<EndpointInfoModel>> GetApplicationEndpoints(
            string applicationId, bool includeDeleted, CancellationToken ct) {
            // Include deleted twins if the application itself is deleted.  Otherwise omit.
            var endpoints = await GetEndpointsAsync(applicationId, includeDeleted, ct);
            return endpoints
                .Select(e => e.ToServiceModel());
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryEventsAsync(IEnumerable<EndpointInfoModel> newEndpoints,
            DiscoveryResultModel result, string discovererId, string applicationId,
            bool hardDelete) {

            if (newEndpoints == null) {
                throw new ArgumentNullException(nameof(newEndpoints));
            }

            var context = result.Context.Validate();

            var found = newEndpoints
                .Select(e => e.ToEndpointRegistration(false, discovererId))
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

                                // Then hard delete...
                                await _iothub.DeleteAsync(item.DeviceId);
                                await _broker.NotifyAllAsync(l => l.OnEndpointDeletedAsync(context,
                                    item.DeviceId, item.ToServiceModel()));
                            }
                            else if (!(item.IsDisabled ?? false)) {
                                var endpoint = item.ToServiceModel();
                                var update = endpoint.ToEndpointRegistration(true);
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
                            // Skip the ones owned by other publishers
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

        private readonly IRegistryEventBroker<IEndpointRegistryListener> _broker;
        private readonly IJsonSerializer _serializer;
        private readonly Action _unregister;
        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
