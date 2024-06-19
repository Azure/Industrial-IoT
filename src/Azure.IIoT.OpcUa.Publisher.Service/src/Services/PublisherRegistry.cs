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
    /// Publisher registry which uses the IoT Hub twin services for supervisor
    /// identity management.
    /// </summary>
    public sealed class PublisherRegistry : IPublisherRegistry
    {
        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="logger"></param>
        /// <param name="events"></param>
        public PublisherRegistry(IIoTHubTwinServices iothub, ILogger<PublisherRegistry> logger,
            IPublisherRegistryListener? events = null)
        {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _events = events;
        }

        /// <inheritdoc/>
        public async Task<PublisherModel> GetPublisherAsync(string publisherId,
            bool onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(publisherId))
            {
                throw new ArgumentNullException(nameof(publisherId));
            }
            if (!HubResource.Parse(publisherId, out _, out var deviceId, out var moduleId, out var error))
            {
                throw new ArgumentException(error, nameof(publisherId));
            }
            var device = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
            if (device.ToEntityRegistration(onlyServerState) is not PublisherRegistration registration)
            {
                throw new ResourceNotFoundException($"{publisherId} is not a publisher registration.");
            }
            return registration.ToPublisherModel()
                ?? throw new ResourceInvalidStateException($"{publisherId} is not a valid publisher model.");
        }

        /// <inheritdoc/>
        public async Task UpdatePublisherAsync(string publisherId,
            PublisherUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(publisherId))
            {
                throw new ArgumentNullException(nameof(publisherId));
            }
            if (!HubResource.Parse(publisherId, out _, out var deviceId, out var moduleId, out var error))
            {
                throw new ArgumentException(error, nameof(publisherId));
            }
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var twin = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
                    if (twin.Id != deviceId && twin.ModuleId != moduleId)
                    {
                        throw new ArgumentException("Id must be same as twin to patch",
                            nameof(publisherId));
                    }

                    if (twin.ToEntityRegistration(true) is not PublisherRegistration registration)
                    {
                        throw new ResourceNotFoundException(
                            $"{publisherId} is not a publisher registration.");
                    }
                    // Update registration from update request
                    var patched = registration.ToPublisherModel()
                        ?? throw new ResourceInvalidStateException($"{publisherId} is not a valid publisher model.");
                    if (request.SiteId != null)
                    {
                        patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                            null : request.SiteId;
                    }

                    if (request.ApiKey != null)
                    {
                        patched.ApiKey = string.IsNullOrEmpty(request.ApiKey) ?
                            null : request.ApiKey;
                    }

                    // Patch
                    twin = await _iothub.PatchAsync(registration.Patch(
                        patched.ToPublisherRegistration()), false, ct).ConfigureAwait(false);

                    if (_events != null)
                    {
                        patched = (twin.ToEntityRegistration(true) as PublisherRegistration).ToPublisherModel(true);
                        if (patched != null)
                        {
                            await _events.OnPublisherUpdatedAsync(null, patched).ConfigureAwait(false);
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
        public async Task<PublisherListModel> ListPublishersAsync(
            string? continuation, bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            const string query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{Constants.TwinPropertyTypeKey} = '{Constants.EntityTypePublisher}' " +
                $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct).ConfigureAwait(false);
            return new PublisherListModel
            {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState).ToPublisherModel(true)!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> QueryPublishersAsync(
            PublisherQueryModel query, bool onlyServerState, int? pageSize, CancellationToken ct)
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

            var queryResult = await _iothub.QueryDeviceTwinsAsync(sql, null,
                pageSize, ct).ConfigureAwait(false);
            return new PublisherListModel
            {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToPublisherRegistration(onlyServerState).ToPublisherModel(true)!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IPublisherRegistryListener? _events;
        private readonly ILogger _logger;
    }
}
