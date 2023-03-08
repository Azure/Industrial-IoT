// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Registry
{
    using Azure.IIoT.OpcUa.Services.Registry.Models;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Furly.Azure.IoT;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor registry which uses the IoT Hub twin services
    /// for supervisor identity management.
    /// </summary>
    public sealed class SupervisorRegistry : ISupervisorRegistry
    {
        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="events"></param>
        public SupervisorRegistry(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            ILogger logger, ISupervisorRegistryListener events = null)
        {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _events = events;
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string supervisorId,
            bool onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(supervisorId))
            {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var deviceId = PublisherModelEx.ParseDeviceId(supervisorId, out var moduleId);
            var device = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
            if (device.ToEntityRegistration(onlyServerState) is not PublisherRegistration registration)
            {
                throw new ResourceNotFoundException(
                    $"{supervisorId} is not a supervisor registration.");
            }
            return registration.ToSupervisorModel();
        }

        /// <inheritdoc/>
        public async Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(supervisorId))
            {
                throw new ArgumentNullException(nameof(supervisorId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = PublisherModelEx.ParseDeviceId(supervisorId, out var moduleId);

            while (true)
            {
                try
                {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId)
                    {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(supervisorId));
                    }

                    if (!(twin.ToEntityRegistration(true) is PublisherRegistration registration))
                    {
                        throw new ResourceNotFoundException(
                            $"{supervisorId} is not a supervisor registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToSupervisorModel();

                    if (request.SiteId != null)
                    {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }

                    if (request.LogLevel != null)
                    {
                        patched.LogLevel = request.LogLevel == TraceLogLevel.Information ?
                            null : request.LogLevel;
                    }

                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(
                        patched.ToPublisherRegistration(), _serializer), false, ct).ConfigureAwait(false);

                    // Send update to through broker
                    registration = twin.ToEntityRegistration(true) as PublisherRegistration;
                    await (_events?.OnSupervisorUpdatedAsync(null, registration.ToSupervisorModel())).ConfigureAwait(false);
                    return;
                }
                catch (ResourceOutOfDateException ex)
                {
                    _logger.LogDebug(ex, "Retrying updating supervisor...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            const string query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Publisher}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct).ConfigureAwait(false);
            return new SupervisorListModel
            {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState))
                    .Select(s => s.ToSupervisorModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            var sql = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Publisher}'";

            if (query?.SiteId != null)
            {
                // If site id provided, include it in search
                sql += $"AND (properties.reported.{TwinProperty.SiteId} = " +
                    $"'{query.SiteId}' OR properties.desired.{TwinProperty.SiteId} = " +
                    $"'{query.SiteId}' OR deviceId = '{query.SiteId}') ";
            }

            if (EndpointInfoModelEx.IsEndpointId(query?.EndpointId))
            {
                // If endpoint id provided include in search
                sql += $"AND IS_DEFINED(properties.desired.{query.EndpointId}) ";
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
            return new SupervisorListModel
            {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState))
                    .Select(s => s.ToSupervisorModel())
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly ISupervisorRegistryListener _events;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
    }
}
