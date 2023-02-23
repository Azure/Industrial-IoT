// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Registry {
    using Azure.IIoT.OpcUa.Services.Models;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
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
    public sealed class DiscovererRegistry : IDiscovererRegistry {
        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="broker"></param>
        public DiscovererRegistry(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            ILogger logger, IDiscovererRegistryListener broker = null) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _events = broker;
        }

        /// <inheritdoc/>
        public async Task<DiscovererModel> GetDiscovererAsync(string discovererId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentException(nameof(discovererId));
            }
            var deviceId = PublisherModelEx.ParseDeviceId(discovererId, out var moduleId);
            var device = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
            if (device.ToEntityRegistration() is not PublisherRegistration registration) {
                throw new ResourceNotFoundException(
                    $"{discovererId} is not a discoverer registration.");
            }
            return registration.ToDiscovererModel();
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
            var deviceId = PublisherModelEx.ParseDeviceId(discovererId, out var moduleId);

            while (true) {
                try {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId) {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(discovererId));
                    }

                    if (!(twin.ToEntityRegistration(true) is PublisherRegistration registration)) {
                        throw new ResourceNotFoundException(
                            $"{discovererId} is not a discoverer registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToDiscovererModel();
                    if (request.Discovery != null && request.Discovery != DiscoveryMode.Off) {
                        _logger.LogWarning("Discovery mode setting is no longer supported." +
                            " Changes will not take effect.");
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
                        _logger.LogWarning("Discovery configuration is no longer supported." +
                            " Changes will not take effect.");
                    }
                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(
                        patched.ToPublisherRegistration(), _serializer), false, ct).ConfigureAwait(false);

                    // Send update to through broker
                    registration = twin.ToEntityRegistration(true) as PublisherRegistration;
                    await (_events?.OnDiscovererUpdatedAsync(null, registration.ToDiscovererModel())).ConfigureAwait(false);
                    return;
                }
                catch (ResourceOutOfDateException ex) {
                    _logger.LogDebug(ex, "Retrying updating discoverer...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<DiscovererListModel> ListDiscoverersAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            const string query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Publisher}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct).ConfigureAwait(false);
            return new DiscovererListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToPublisherRegistration())
                    .Select(s => s.ToDiscovererModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<DiscovererListModel> QueryDiscoverersAsync(
            DiscovererQueryModel model, int? pageSize, CancellationToken ct) {
            // This is no longer supported, return empty result
            if (model?.Discovery != null && model?.Discovery != DiscoveryMode.Off) {
                return new DiscovererListModel {
                    Items = new List<DiscovererModel>()
                };
            }

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Publisher}'";

            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR properties.desired.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR deviceId ='{model.SiteId}') ";
            }
            if (model?.Connected != null) {
                // If flag provided, include it in search
                if (model.Connected.Value) {
                    query += "AND connectionState = 'Connected' ";
                }
                else {
                    query += "AND connectionState != 'Connected' ";
                }
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize, ct).ConfigureAwait(false);
            return new DiscovererListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToPublisherRegistration())
                    .Select(s => s.ToDiscovererModel())
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IJsonSerializer _serializer;
        private readonly IDiscovererRegistryListener _events;
        private readonly ILogger _logger;
    }
}
