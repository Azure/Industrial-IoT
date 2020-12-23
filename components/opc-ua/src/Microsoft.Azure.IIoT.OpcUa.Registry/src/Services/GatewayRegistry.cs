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
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Edge registry which uses the IoT Hub twin services for gateway
    /// identity management.
    /// </summary>
    public sealed class GatewayRegistry : IGatewayRegistry {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        public GatewayRegistry(IIoTHubTwinServices iothub,
            IRegistryEventBroker<IGatewayRegistryListener> broker, ILogger logger) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<GatewayInfoModel> GetGatewayAsync(string gatewayId,
            bool onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(gatewayId)) {
                throw new ArgumentException(nameof(gatewayId));
            }
            var deviceId = gatewayId;
            var device = await _iothub.GetAsync(deviceId, null, ct);
            var registration = device.ToEntityRegistration()
                as GatewayRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{gatewayId} is not a gateway registration.");
            }

            var modules = await _iothub.QueryAllDeviceTwinsAsync(
                $"SELECT * FROM devices.modules WHERE deviceId = '{device.Id}'", ct);
            var gatewayModules = new GatewayModulesModel();
            foreach (var module in modules) {
                var entity = module.ToEntityRegistration(onlyServerState);
                switch (entity) {
                    case SupervisorRegistration sr:
                        gatewayModules.Supervisor = sr.ToServiceModel();
                        break;
                    case PublisherRegistration pr:
                        gatewayModules.Publisher = pr.ToServiceModel();
                        break;
                    case DiscovererRegistration dr:
                        gatewayModules.Discoverer = dr.ToServiceModel();
                        break;
                    default:
                        // might add module to dictionary in the future
                        break;
                }
            }
            return new GatewayInfoModel {
                Gateway = registration.ToServiceModel(),
                Modules = gatewayModules
            };
        }

        /// <inheritdoc/>
        public async Task UpdateGatewayAsync(string gatewayId,
            GatewayUpdateModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(gatewayId)) {
                throw new ArgumentException(nameof(gatewayId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = gatewayId;

            while (true) {
                try {
                    var twin = await _iothub.GetAsync(deviceId, null, ct);
                    if (twin.Id != deviceId) {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(gatewayId));
                    }

                    var registration = twin.ToEntityRegistration(true) as GatewayRegistration;
                    if (registration == null) {
                        throw new ResourceNotFoundException(
                            $"{gatewayId} is not a gateway registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToServiceModel();

                    if (request.SiteId != null) {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }
                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(
                        patched.ToGatewayRegistration()), false, ct);

                    // Send update to through broker
                    registration = twin.ToEntityRegistration(true) as GatewayRegistration;
                    await _broker.NotifyAllAsync(l => l.OnGatewayUpdatedAsync(null,
                        registration.ToServiceModel()));
                    return;
                }
                catch (ResourceOutOfDateException ex) {
                    _logger.Debug(ex, "Retrying updating gateway...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<GatewayListModel> ListGatewaysAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{TwinProperty.Type} = '{IdentityType.Gateway}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct);
            return new GatewayListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToGatewayRegistration())
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<GatewayListModel> QueryGatewaysAsync(
            GatewayQueryModel model, int? pageSize, CancellationToken ct) {

            var query = "SELECT * FROM devices WHERE " +
                $"tags.{TwinProperty.Type} = '{IdentityType.Gateway}' ";

            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query +=
$"AND (tags.{TwinProperty.SiteId} = '{model.SiteId}' OR deviceId = '{model.SiteId}') ";
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
            return new GatewayListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToGatewayRegistration())
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IRegistryEventBroker<IGatewayRegistryListener> _broker;
        private readonly ILogger _logger;
    }
}
