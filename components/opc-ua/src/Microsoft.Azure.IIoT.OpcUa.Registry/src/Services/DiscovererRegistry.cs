// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Discoverer registry which uses the IoT Hub twin services for discoverer
    /// identity management.
    /// </summary>
    public sealed class DiscovererRegistry : IDiscovererRegistry {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="broker"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public DiscovererRegistry(IIoTHubTwinServices iothub,
            IRegistryEventBroker<IDiscovererRegistryListener> broker,
            IJsonSerializer serializer, ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<DiscovererModel> GetDiscovererAsync(string id,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var deviceId = DiscovererModelEx.ParseDeviceId(id, out var moduleId);
            var device = await _iothub.GetAsync(deviceId, moduleId, ct);
            var registration = device.ToEntityRegistration()
                as DiscovererRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{id} is not a discoverer registration.");
            }
            return registration.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task UpdateDiscovererAsync(string discovererId,
            DiscovererUpdateModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentException(nameof(discovererId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = DiscovererModelEx.ParseDeviceId(discovererId, out var moduleId);

            while (true) {
                try {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId) {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(discovererId));
                    }

                    var registration = twin.ToEntityRegistration(true) as DiscovererRegistration;
                    if (registration == null) {
                        throw new ResourceNotFoundException(
                            $"{discovererId} is not a discoverer registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToServiceModel();
                    if (request.Discovery != null) {
                        patched.RequestedMode = (DiscoveryMode)request.Discovery;
                    }

                    if (request.SiteId != null) {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }

                    if (request.LogLevel != null) {
                        patched.LogLevel = request.LogLevel == TraceLogLevel.Information ?
                            null : request.LogLevel;
                    }

                    if (request.DiscoveryConfig != null) {
                        if (patched.RequestedConfig == null) {
                            patched.RequestedConfig = new DiscoveryConfigModel();
                        }
                        if (request.DiscoveryConfig.AddressRangesToScan != null) {
                            patched.RequestedConfig.AddressRangesToScan =
                                string.IsNullOrEmpty(
                                    request.DiscoveryConfig.AddressRangesToScan.Trim()) ?
                                        null : request.DiscoveryConfig.AddressRangesToScan;
                        }
                        if (request.DiscoveryConfig.PortRangesToScan != null) {
                            patched.RequestedConfig.PortRangesToScan =
                                string.IsNullOrEmpty(
                                    request.DiscoveryConfig.PortRangesToScan.Trim()) ?
                                        null : request.DiscoveryConfig.PortRangesToScan;
                        }
                        if (request.DiscoveryConfig.IdleTimeBetweenScans != null) {
                            patched.RequestedConfig.IdleTimeBetweenScans =
                                request.DiscoveryConfig.IdleTimeBetweenScans.Value.Ticks < 0 ?
                                    null : request.DiscoveryConfig.IdleTimeBetweenScans;
                        }
                        if (request.DiscoveryConfig.MaxNetworkProbes != null) {
                            patched.RequestedConfig.MaxNetworkProbes =
                                request.DiscoveryConfig.MaxNetworkProbes <= 0 ?
                                    null : request.DiscoveryConfig.MaxNetworkProbes;
                        }
                        if (request.DiscoveryConfig.NetworkProbeTimeout != null) {
                            patched.RequestedConfig.NetworkProbeTimeout =
                                request.DiscoveryConfig.NetworkProbeTimeout.Value.Ticks <= 0 ?
                                    null : request.DiscoveryConfig.NetworkProbeTimeout;
                        }
                        if (request.DiscoveryConfig.MaxPortProbes != null) {
                            patched.RequestedConfig.MaxPortProbes =
                                request.DiscoveryConfig.MaxPortProbes <= 0 ?
                                    null : request.DiscoveryConfig.MaxPortProbes;
                        }
                        if (request.DiscoveryConfig.MinPortProbesPercent != null) {
                            patched.RequestedConfig.MinPortProbesPercent =
                                request.DiscoveryConfig.MinPortProbesPercent <= 0 ||
                                request.DiscoveryConfig.MinPortProbesPercent > 100 ?
                                    null : request.DiscoveryConfig.MinPortProbesPercent;
                        }
                        if (request.DiscoveryConfig.PortProbeTimeout != null) {
                            patched.RequestedConfig.PortProbeTimeout =
                                request.DiscoveryConfig.PortProbeTimeout.Value.Ticks <= 0 ?
                                    null : request.DiscoveryConfig.PortProbeTimeout;
                        }
                        if (request.DiscoveryConfig.ActivationFilter != null) {
                            patched.RequestedConfig.ActivationFilter =
                                request.DiscoveryConfig.ActivationFilter.SecurityMode == null &&
                                request.DiscoveryConfig.ActivationFilter.SecurityPolicies == null &&
                                request.DiscoveryConfig.ActivationFilter.TrustLists == null ?
                                    null : request.DiscoveryConfig.ActivationFilter;
                        }
                        if (request.DiscoveryConfig.DiscoveryUrls != null) {
                            patched.RequestedConfig.DiscoveryUrls =
                                request.DiscoveryConfig.DiscoveryUrls.Count == 0 ?
                                    null : request.DiscoveryConfig.DiscoveryUrls;
                        }
                    }
                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(
                        patched.ToDiscovererRegistration(), _serializer), false, ct);

                    // Send update to through broker
                    registration = twin.ToEntityRegistration(true) as DiscovererRegistration;
                    await _broker.NotifyAllAsync(l => l.OnDiscovererUpdatedAsync(null,
                        registration.ToServiceModel()));
                    return;
                }
                catch (ResourceOutOfDateException ex) {
                    _logger.Debug(ex, "Retrying updating discoverer...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<DiscovererListModel> ListDiscoverersAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Discoverer}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct);
            return new DiscovererListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToDiscovererRegistration())
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<DiscovererListModel> QueryDiscoverersAsync(
            DiscovererQueryModel model, int? pageSize, CancellationToken ct) {

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Discoverer}'";

            if (model?.Discovery != null) {
                // If reported discovery mode provided, include it in search
                query += $"AND (properties.reported.{nameof(DiscovererRegistration.Discovery)} = " +
                    $"'{model.Discovery}' " +
                         $"OR properties.desired.{nameof(DiscovererRegistration.Discovery)} = " +
                    $"'{model.Discovery}')";
            }
            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR properties.desired.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR deviceId ='{model.SiteId}') ";
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

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize, ct);
            return new DiscovererListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToDiscovererRegistration())
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IJsonSerializer _serializer;
        private readonly IRegistryEventBroker<IDiscovererRegistryListener> _broker;
        private readonly ILogger _logger;
    }
}
