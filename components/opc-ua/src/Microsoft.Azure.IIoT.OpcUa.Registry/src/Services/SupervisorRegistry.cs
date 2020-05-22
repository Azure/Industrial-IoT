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
    using Microsoft.Azure.IIoT.Serializers;
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
        /// <param name="serializer"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        public SupervisorRegistry(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            IRegistryEventBroker<ISupervisorRegistryListener> broker, ILogger logger) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(id, out var moduleId);
            var device = await _iothub.GetAsync(deviceId, moduleId, ct);
            var registration = device.ToEntityRegistration(onlyServerState)
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

                    var registration = twin.ToEntityRegistration(true) as SupervisorRegistration;
                    if (registration == null) {
                        throw new ResourceNotFoundException(
                            $"{supervisorId} is not a supervisor registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToServiceModel();

                    if (request.SiteId != null) {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }

                    if (request.LogLevel != null) {
                        patched.LogLevel = request.LogLevel == TraceLogLevel.Information ?
                            null : request.LogLevel;
                    }

                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(
                        patched.ToSupervisorRegistration(), _serializer), false, ct);

                    // Send update to through broker
                    registration = twin.ToEntityRegistration(true) as SupervisorRegistration;
                    await _broker.NotifyAllAsync(l => l.OnSupervisorUpdatedAsync(null,
                        registration.ToServiceModel()));
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
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Supervisor}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
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
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Supervisor}'";

            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR properties.desired.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR deviceId = '{model.SiteId}') ";
            }

            if (EndpointInfoModelEx.IsEndpointId(model?.EndpointId)) {
                // If endpoint id provided include in search
                query += $"AND IS_DEFINED(properties.desired.{model.EndpointId}) ";
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
            return new SupervisorListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToSupervisorRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IRegistryEventBroker<ISupervisorRegistryListener> _broker;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
    }
}
