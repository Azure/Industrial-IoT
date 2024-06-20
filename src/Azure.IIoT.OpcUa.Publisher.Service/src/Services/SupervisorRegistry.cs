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
        /// <param name="logger"></param>
        /// <param name="events"></param>
        /// <param name="timeProvider"></param>
        public SupervisorRegistry(IIoTHubTwinServices iothub, ILogger<SupervisorRegistry> logger,
            ISupervisorRegistryListener? events = null, TimeProvider? timeProvider = null)
        {
            _iothub = iothub;
            _logger = logger;
            _events = events;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string supervisorId, bool onlyServerState,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(supervisorId))
            {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            if (!HubResource.Parse(supervisorId, out _, out var deviceId, out var moduleId, out var error))
            {
                throw new ArgumentException(error, nameof(supervisorId));
            }
            var device = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
            if (device.ToEntityRegistration(onlyServerState) is not PublisherRegistration registration)
            {
                throw new ResourceNotFoundException($"{supervisorId} is not a supervisor registration.");
            }
            return registration.ToSupervisorModel()
                ?? throw new ResourceInvalidStateException($"{supervisorId} is not a valid supervisor.");
        }

        /// <inheritdoc/>
        public async Task UpdateSupervisorAsync(string supervisorId, SupervisorUpdateModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(supervisorId))
            {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            if (!HubResource.Parse(supervisorId, out _, out var deviceId, out var moduleId, out var error))
            {
                throw new ArgumentException(error, nameof(supervisorId));
            }
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId)
                    {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(supervisorId));
                    }

                    if (twin.ToEntityRegistration(true) is not PublisherRegistration registration)
                    {
                        throw new ResourceNotFoundException($"{supervisorId} is not a supervisor registration.");
                    }

                    // Update registration from update request
                    var patched = registration.ToSupervisorModel()
                        ?? throw new ResourceInvalidStateException($"{supervisorId} is not a valid supervisor.");
                    if (request.SiteId != null)
                    {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }

                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(patched.ToPublisherRegistration(), _timeProvider),
                        false, ct).ConfigureAwait(false);

                    if (_events != null)
                    {
                        // Send update to through broker
                        patched = (twin.ToEntityRegistration(true) as PublisherRegistration).ToSupervisorModel();
                        if (patched != null)
                        {
                            await _events.OnSupervisorUpdatedAsync(null, patched).ConfigureAwait(false);
                        }
                    }
                    return;
                }
                catch (ResourceOutOfDateException ex)
                {
                    _logger.LogDebug(ex, "Retrying updating supervisor...");
                }
            }
            throw new OperationCanceledException();
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string? continuation, bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            const string query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{Constants.TwinPropertyTypeKey} = '{Constants.EntityTypePublisher}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct).ConfigureAwait(false);
            return new SupervisorListModel
            {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState)?.ToSupervisorModel()!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            var sql = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{Constants.TwinPropertyTypeKey} = '{Constants.EntityTypePublisher}'";

            if (query?.SiteId != null)
            {
                // If site id provided, include it in search
                sql += $"AND (properties.reported.{Constants.TwinPropertySiteKey} = " +
                    $"'{query.SiteId}' OR properties.desired.{Constants.TwinPropertySiteKey} = " +
                    $"'{query.SiteId}' OR deviceId = '{query.SiteId}') ";
            }

            if (EndpointInfoModelEx.IsEndpointId(query?.EndpointId))
            {
                // If endpoint id provided include in search
                sql += $"AND IS_DEFINED(properties.desired.{query!.EndpointId}) ";
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
                    .Select(t => t.ToPublisherRegistration(onlyServerState)?.ToSupervisorModel()!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly ISupervisorRegistryListener? _events;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
    }
}
