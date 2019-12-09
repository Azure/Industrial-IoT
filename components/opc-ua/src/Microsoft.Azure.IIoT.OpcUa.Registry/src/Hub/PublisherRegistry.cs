// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Publisher registry which uses the IoT Hub twin services for supervisor
    /// identity management.
    /// </summary>
    public sealed class PublisherRegistry : IPublisherRegistry, IPublisherEndpointQuery {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="logger"></param>
        public PublisherRegistry(IIoTHubTwinServices iothub, ILogger logger) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<(string, EndpointModel)> FindPublisherEndpoint(
            string endpointId, CancellationToken ct) {

            // Get the endpoint and the endpoints supervisor
            var device = await _iothub.GetAsync(endpointId, null, ct);
            var registration = device.ToEndpointRegistration(false);

            var endpoint = registration.ToServiceModel();
            var supervisorId = endpoint?.Registration?.SupervisorId;

            if (string.IsNullOrEmpty(supervisorId)) {
                // No supervisor set for the retrieved endpoint
                throw new ResourceInvalidStateException(
                    $"Endpoint {endpointId} has no supervisor");
            }

            // Get iotedge device
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out _);

            // Query for the publisher in the same edge device
            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = 'publisher' " +
                $"AND deviceId = '{deviceId}'";
            var devices = await _iothub.QueryAllDeviceTwinsAsync(query, ct);

            device = devices.SingleOrDefault();
            if (device == null) {
                throw new ResourceNotFoundException(
                    $"No publisher found for {endpointId} in {deviceId}");
            }
            var publisherId = PublisherModelEx.CreatePublisherId(
                device.Id, device.ModuleId);

            return (publisherId, endpoint.Registration.Endpoint);
        }

        /// <inheritdoc/>
        public async Task<PublisherModel> GetPublisherAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var deviceId = PublisherModelEx.ParseDeviceId(id, out var moduleId);
            var device = await _iothub.GetAsync(deviceId, moduleId, ct);
            var registration = device.ToRegistration(onlyServerState)
                as PublisherRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{id} is not a supervisor registration.");
            }
            return registration.ToServiceModel();
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
            var deviceId = SupervisorModelEx.ParseDeviceId(publisherId, out var moduleId);

            while (true) {
                try {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId) {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(publisherId));
                    }

                    var registration = twin.ToRegistration(true) as PublisherRegistration;
                    if (registration == null) {
                        throw new ResourceNotFoundException(
                            $"{publisherId} is not a publisher registration.");
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

                    if (request.Configuration != null) {
                        if (patched.Configuration == null) {
                            patched.Configuration = new PublisherConfigModel();
                        }
                        if (request.Configuration.JobOrchestratorUrl != null) {
                            patched.Configuration.JobOrchestratorUrl =
                                string.IsNullOrEmpty(
                                    request.Configuration.JobOrchestratorUrl.Trim()) ?
                                        null : request.Configuration.JobOrchestratorUrl;
                        }
                        if (request.Configuration.HeartbeatInterval != null) {
                            patched.Configuration.HeartbeatInterval =
                                request.Configuration.HeartbeatInterval;
                        }
                        if (request.Configuration.JobCheckInterval != null) {
                            patched.Configuration.JobCheckInterval =
                                request.Configuration.JobCheckInterval.Value.Ticks == 0 ?
                                    null : request.Configuration.JobCheckInterval;
                        }
                        if (request.Configuration.MaxWorkers != null) {
                            patched.Configuration.MaxWorkers =
                                request.Configuration.MaxWorkers <= 0 ?
                                    null : request.Configuration.MaxWorkers;
                        }
                        if (request.Configuration.Capabilities != null) {
                            patched.Configuration.Capabilities =
                                request.Configuration.Capabilities.Count == 0 ?
                                    null : request.Configuration.Capabilities;
                        }
                    }
                    // Patch
                    await _iothub.PatchAsync(registration.Patch(
                        patched.ToPublisherRegistration()), false, ct);
                    return;
                }
                catch (ResourceOutOfDateException ex) {
                    _logger.Debug(ex, "Retrying updating supervisor...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> ListPublishersAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = 'publisher' " +
                $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct);
            return new PublisherListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> QueryPublishersAsync(
            PublisherQueryModel model, bool onlyServerState, int? pageSize, CancellationToken ct) {

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = 'publisher'";

            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{TwinProperty.SiteId} = " +
                    $"'{model.SiteId}' OR properties.desired.{TwinProperty.SiteId} = " +
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
                        $"OR properties.reported.{TwinProperty.Connected} != true) ";
                }
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize, ct);
            return new PublisherListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
