// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services
{
    using Azure.IIoT.OpcUa.Publisher.Service.Services.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Azure;
    using Furly.Azure.IoT;
    using Furly.Exceptions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Discoverer registry which uses the IoT Hub twin services for discoverer
    /// identity management.
    /// </summary>
    public sealed class DiscovererRegistry : IDiscovererRegistry
    {
        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="logger"></param>
        /// <param name="broker"></param>
        public DiscovererRegistry(IIoTHubTwinServices iothub, ILogger<DiscovererRegistry> logger,
            IDiscovererRegistryListener? broker = null)
        {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _events = broker;
        }

        /// <inheritdoc/>
        public async Task<DiscovererModel> GetDiscovererAsync(string discovererId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (!HubResource.Parse(discovererId, out _, out var deviceId, out var moduleId,
                out var error))
            {
                throw new ArgumentException(error, nameof(discovererId));
            }
            var device = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
            if (device.ToEntityRegistration() is not PublisherRegistration registration)
            {
                throw new ResourceNotFoundException(
                    $"{discovererId} is not a discoverer registration.");
            }
            return registration.ToDiscovererModel() ?? throw new ResourceInvalidStateException(
                    $"{discovererId} is not a valid discoverer registration.");
        }

        /// <inheritdoc/>
        public async Task UpdateDiscovererAsync(string discovererId,
            DiscovererUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (!HubResource.Parse(discovererId, out _, out var deviceId, out var moduleId,
                out var error))
            {
                throw new ArgumentException(error, nameof(discovererId));
            }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId)
                    {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(discovererId));
                    }

                    if (twin.ToEntityRegistration(true) is not PublisherRegistration registration)
                    {
                        throw new ResourceNotFoundException(
                            $"{discovererId} is not a publisher registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToDiscovererModel() ?? throw new ResourceInvalidStateException(
                            $"{discovererId} is not a valid publisher registration.");
                    if (request.Discovery != null && request.Discovery != DiscoveryMode.Off)
                    {
                        _logger.LogWarning("Discovery mode setting is no longer supported." +
                            " Changes will not take effect.");
                    }

                    if (request.SiteId != null)
                    {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }

                    if (request.DiscoveryConfig != null)
                    {
                        _logger.LogWarning("Discovery configuration is no longer supported." +
                            " Changes will not take effect.");
                    }
                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(
                        patched.ToPublisherRegistration()), false, ct).ConfigureAwait(false);

                    // Send update to through broker
                    if (_events != null)
                    {
                        patched = (twin.ToEntityRegistration(true) as PublisherRegistration).ToDiscovererModel();
                        if (patched != null)
                        {
                            await _events.OnDiscovererUpdatedAsync(null, patched).ConfigureAwait(false);
                        }
                    }
                    return;
                }
                catch (ResourceOutOfDateException ex)
                {
                    _logger.LogDebug(ex, "Retrying updating discoverer...");
                }
            }
            throw new OperationCanceledException();
        }

        /// <inheritdoc/>
        public async Task<DiscovererListModel> ListDiscoverersAsync(
            string? continuation, int? pageSize, CancellationToken ct)
        {
            const string query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{Constants.TwinPropertyTypeKey} = '{Constants.EntityTypePublisher}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct).ConfigureAwait(false);
            return new DiscovererListModel
            {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToPublisherRegistration())
                    .Select(s => s.ToDiscovererModel()!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<DiscovererListModel> QueryDiscoverersAsync(
            DiscovererQueryModel query, int? pageSize, CancellationToken ct)
        {
            // This is no longer supported, return empty result
            if (query?.Discovery != null && query?.Discovery != DiscoveryMode.Off)
            {
                return new DiscovererListModel
                {
                    Items = new List<DiscovererModel>()
                };
            }

            var sql = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{Constants.TwinPropertyTypeKey} = '{Constants.EntityTypePublisher}'";

            if (query?.SiteId != null)
            {
                // If site id provided, include it in search
                sql += $"AND (properties.reported.{Constants.TwinPropertySiteKey} = " +
                    $"'{query.SiteId}' OR properties.desired.{Constants.TwinPropertySiteKey} = " +
                    $"'{query.SiteId}' OR deviceId ='{query.SiteId}') ";
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
            return new DiscovererListModel
            {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToPublisherRegistration())
                    .Select(s => s.ToDiscovererModel()!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IDiscovererRegistryListener? _events;
        private readonly ILogger _logger;
    }
}
