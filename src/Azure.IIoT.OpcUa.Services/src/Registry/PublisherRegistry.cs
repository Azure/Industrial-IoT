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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher registry which uses the IoT Hub twin services for supervisor
    /// identity management.
    /// </summary>
    public sealed class PublisherRegistry : IPublisherRegistry {
        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="events"></param>
        public PublisherRegistry(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            ILogger logger, IPublisherRegistryListener events = null) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _events = events;
        }

        /// <inheritdoc/>
        public async Task<PublisherModel> GetPublisherAsync(string publisherId,
            bool onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(publisherId)) {
                throw new ArgumentException(nameof(publisherId));
            }
            var deviceId = PublisherModelEx.ParseDeviceId(publisherId, out var moduleId);
            var device = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
            if (!(device.ToEntityRegistration(onlyServerState) is PublisherRegistration registration)) {
                throw new ResourceNotFoundException(
                    $"{publisherId} is not a publisher registration.");
            }
            return registration.ToPublisherModel();
        }

        /// <inheritdoc/>
        public async Task UpdatePublisherAsync(string publisherId,
            PublisherUpdateModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(publisherId)) {
                throw new ArgumentException(nameof(publisherId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = PublisherModelEx.ParseDeviceId(publisherId, out var moduleId);

            while (true) {
                try {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId) {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(publisherId));
                    }

                    if (!(twin.ToEntityRegistration(true) is PublisherRegistration registration)) {
                        throw new ResourceNotFoundException(
                            $"{publisherId} is not a publisher registration.");
                    }
                    // Update registration from update request
                    var patched = registration.ToPublisherModel();
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
                        patched.ToPublisherRegistration(), _serializer), false, ct).ConfigureAwait(false);

                    // Send update to through broker
                    registration = twin.ToEntityRegistration(true) as PublisherRegistration;
                    await (_events?.OnPublisherUpdatedAsync(null, registration.ToPublisherModel())).ConfigureAwait(false);
                    return;
                }
                catch (ResourceOutOfDateException ex) {
                    _logger.LogDebug(ex, "Retrying updating supervisor...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> ListPublishersAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            const string query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Publisher}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct).ConfigureAwait(false);
            return new PublisherListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState))
                    .Select(s => s.ToPublisherModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> QueryPublishersAsync(
            PublisherQueryModel model, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Publisher}'";

            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR properties.desired.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR deviceId = '{model.SiteId}') ";
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
            return new PublisherListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState))
                    .Select(s => s.ToPublisherModel())
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IJsonSerializer _serializer;
        private readonly IPublisherRegistryListener _events;
        private readonly ILogger _logger;
    }
}
