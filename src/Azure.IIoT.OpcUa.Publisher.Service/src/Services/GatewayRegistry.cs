// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services
{
    using Azure.IIoT.OpcUa.Publisher.Service.Services.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Azure.IoT;
    using Furly.Exceptions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge registry which uses the IoT Hub twin services for gateway
    /// identity management.
    /// </summary>
    public sealed class GatewayRegistry : IGatewayRegistry
    {
        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="logger"></param>
        /// <param name="events"></param>
        public GatewayRegistry(IIoTHubTwinServices iothub,
            ILogger<GatewayRegistry> logger, IGatewayRegistryListener? events = null)
        {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _events = events;
        }

        /// <inheritdoc/>
        public async Task<GatewayInfoModel> GetGatewayAsync(string gatewayId,
            bool onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(gatewayId))
            {
                throw new ArgumentNullException(nameof(gatewayId));
            }
            var deviceId = gatewayId;
            var device = await _iothub.GetAsync(deviceId, null, ct).ConfigureAwait(false);
            if (device.ToEntityRegistration() is not GatewayRegistration registration)
            {
                throw new ResourceNotFoundException(
                    $"{gatewayId} is not a gateway registration.");
            }

            var modules = await _iothub.QueryAllDeviceTwinsAsync(
                $"SELECT * FROM devices.modules WHERE deviceId = '{device.Id}'", ct).ConfigureAwait(false);
            var gatewayModules = new GatewayModulesModel();
            foreach (var module in modules)
            {
                switch (module.ToEntityRegistration(onlyServerState))
                {
                    case PublisherRegistration pr:
                        gatewayModules.Supervisor = pr.ToSupervisorModel();
                        gatewayModules.Discoverer = pr.ToDiscovererModel();
                        gatewayModules.Publisher = pr.ToPublisherModel();
                        break;
                    default:
                        // might add module to dictionary in the future
                        break;
                }
            }
            return new GatewayInfoModel
            {
                Gateway = registration.ToServiceModel(),
                Modules = gatewayModules
            };
        }

        /// <inheritdoc/>
        public async Task UpdateGatewayAsync(string gatewayId, GatewayUpdateModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(gatewayId))
            {
                throw new ArgumentNullException(nameof(gatewayId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = gatewayId;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var twin = await _iothub.GetAsync(deviceId, null, ct).ConfigureAwait(false);
                    if (twin.Id != deviceId)
                    {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(gatewayId));
                    }

                    if (twin.ToEntityRegistration(true) is not GatewayRegistration registration)
                    {
                        throw new ResourceNotFoundException($"{gatewayId} is not a gateway registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToServiceModel()
                        ?? throw new ResourceInvalidStateException($"{gatewayId} is not a valid gateway.");
                    if (request.SiteId != null)
                    {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }
                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(
                        patched.ToGatewayRegistration()), false, ct).ConfigureAwait(false);

                    // Send update to through broker
                    if (_events != null)
                    {
                        patched = (twin.ToEntityRegistration(true) as GatewayRegistration).ToServiceModel();
                        if (patched != null)
                        {
                            await _events.OnGatewayUpdatedAsync(null, patched).ConfigureAwait(false);
                        }
                    }
                    return;
                }
                catch (ResourceOutOfDateException ex)
                {
                    _logger.LogDebug(ex, "Retrying updating gateway...");
                }
            }
        }

        /// <inheritdoc/>
        public async Task<GatewayListModel> ListGatewaysAsync(
            string? continuation, int? pageSize, CancellationToken ct)
        {
            const string query = "SELECT * FROM devices WHERE " +
                $"tags.{Constants.TwinPropertyTypeKey} = '{Constants.EntityTypeGateway}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct).ConfigureAwait(false);
            return new GatewayListModel
            {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToGatewayRegistration().ToServiceModel()!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<GatewayListModel> QueryGatewaysAsync(
            GatewayQueryModel query, int? pageSize, CancellationToken ct)
        {
            var sql = "SELECT * FROM devices WHERE " +
                $"tags.{Constants.TwinPropertyTypeKey} = '{Constants.EntityTypeGateway}' ";

            if (query?.SiteId != null)
            {
                // If site id provided, include it in search
                sql +=
$"AND (tags.{Constants.TwinPropertySiteKey} = '{query.SiteId}' OR deviceId = '{query.SiteId}') ";
            }
            if (query?.Connected != null)
            {
                // If flag provided, include it in search
                if (query.Connected.Value)
                {
                    sql += "AND connectionState = 'Connected' ";
                }
                else
                {
                    sql += "AND connectionState != 'Connected' ";
                }
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(sql, null, pageSize, ct).ConfigureAwait(false);
            return new GatewayListModel
            {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToGatewayRegistration().ToServiceModel()!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IGatewayRegistryListener? _events;
        private readonly ILogger _logger;
    }
}
