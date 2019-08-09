// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Supervisor registry which uses the IoT Hub twin services for supervisor
    /// identity management.
    /// </summary>
    public sealed class SupervisorRegistry : ISupervisorRegistry {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="logger"></param>
        public SupervisorRegistry(IIoTHubTwinServices iothub, ILogger logger) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(id, out var moduleId);
            var device = await _iothub.GetAsync(deviceId, moduleId, ct);
            var registration = device.ToRegistration(onlyServerState)
                as SupervisorRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{id} is not a supervisor registration.");
            }
            return registration.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentException(nameof(supervisorId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out var moduleId);

            while (true) {
                try {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId) {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(supervisorId));
                    }

                    var registration = twin.ToRegistration(true) as SupervisorRegistration;
                    if (registration == null) {
                        throw new ResourceNotFoundException(
                            $"{supervisorId} is not a supervisor registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToServiceModel();
                    if (request.Discovery != null) {
                        patched.Discovery = (DiscoveryMode)request.Discovery;
                    }

                    if (request.SiteId != null) {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }

                    if (request.LogLevel != null) {
                        patched.LogLevel = request.LogLevel == SupervisorLogLevel.Information ?
                            null : request.LogLevel;
                    }

                    if (request.DiscoveryConfig != null) {
                        if (patched.DiscoveryConfig == null) {
                            patched.DiscoveryConfig = new DiscoveryConfigModel();
                        }
                        if (request.DiscoveryConfig.AddressRangesToScan != null) {
                            patched.DiscoveryConfig.AddressRangesToScan =
                                string.IsNullOrEmpty(
                                    request.DiscoveryConfig.AddressRangesToScan.Trim()) ?
                                        null : request.DiscoveryConfig.AddressRangesToScan;
                        }
                        if (request.DiscoveryConfig.PortRangesToScan != null) {
                            patched.DiscoveryConfig.PortRangesToScan =
                                string.IsNullOrEmpty(
                                    request.DiscoveryConfig.PortRangesToScan.Trim()) ?
                                        null : request.DiscoveryConfig.PortRangesToScan;
                        }
                        if (request.DiscoveryConfig.IdleTimeBetweenScans != null) {
                            patched.DiscoveryConfig.IdleTimeBetweenScans =
                                request.DiscoveryConfig.IdleTimeBetweenScans;
                        }
                        if (request.DiscoveryConfig.MaxNetworkProbes != null) {
                            patched.DiscoveryConfig.MaxNetworkProbes =
                                request.DiscoveryConfig.MaxNetworkProbes <= 0 ?
                                    null : request.DiscoveryConfig.MaxNetworkProbes;
                        }
                        if (request.DiscoveryConfig.NetworkProbeTimeout != null) {
                            patched.DiscoveryConfig.NetworkProbeTimeout =
                                request.DiscoveryConfig.NetworkProbeTimeout.Value.Ticks == 0 ?
                                    null : request.DiscoveryConfig.NetworkProbeTimeout;
                        }
                        if (request.DiscoveryConfig.MaxPortProbes != null) {
                            patched.DiscoveryConfig.MaxPortProbes =
                                request.DiscoveryConfig.MaxPortProbes <= 0 ?
                                    null : request.DiscoveryConfig.MaxPortProbes;
                        }
                        if (request.DiscoveryConfig.MinPortProbesPercent != null) {
                            patched.DiscoveryConfig.MinPortProbesPercent =
                                request.DiscoveryConfig.MinPortProbesPercent <= 0 ||
                                request.DiscoveryConfig.MinPortProbesPercent > 100 ?
                                    null : request.DiscoveryConfig.MinPortProbesPercent;
                        }
                        if (request.DiscoveryConfig.PortProbeTimeout != null) {
                            patched.DiscoveryConfig.PortProbeTimeout =
                                request.DiscoveryConfig.PortProbeTimeout.Value.Ticks == 0 ?
                                    null : request.DiscoveryConfig.PortProbeTimeout;
                        }
                        if (request.DiscoveryConfig.ActivationFilter != null) {
                            patched.DiscoveryConfig.ActivationFilter =
                                request.DiscoveryConfig.ActivationFilter.SecurityMode == null &&
                                request.DiscoveryConfig.ActivationFilter.SecurityPolicies == null &&
                                request.DiscoveryConfig.ActivationFilter.TrustLists == null ?
                                    null : request.DiscoveryConfig.ActivationFilter;
                        }
                    }
                    // Patch
                    await _iothub.PatchAsync(registration.Patch(
                        patched.ToSupervisorRegistration()), false, ct);
                    return;
                }
                catch (ResourceOutOfDateException ex) {
                    _logger.Debug(ex, "Retrying updating supervisor...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.kType} = 'supervisor' " +
                $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct);
            return new SupervisorListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToSupervisorRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel model, bool onlyServerState, int? pageSize, CancellationToken ct) {

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.kType} = 'supervisor'";

            if (model?.Discovery != null) {
                // If discovery mode provided, include it in search
                query += $"AND properties.desired.{nameof(SupervisorRegistration.Discovery)} = " +
                    $"'{model.Discovery}' ";
            }
            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{TwinProperty.kSiteId} = " +
                    $"'{model.SiteId}' OR properties.desired.{TwinProperty.kSiteId} = " +
                    $"'{model.SiteId}')";
            }
            if (model?.Connected != null) {
                // If flag provided, include it in search
                if (model.Connected.Value) {
                    query += $"AND connectionState = 'Connected' ";
                    // Do not use connected property as module might have exited before updating.
                }
                else {
                    query += $"AND (connectionState = 'Disconnected' " +
                        $"OR properties.reported.{TwinProperty.kConnected} != true) ";
                }
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize, ct);
            return new SupervisorListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToSupervisorRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
