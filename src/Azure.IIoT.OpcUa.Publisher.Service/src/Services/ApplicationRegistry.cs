// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services
{
    using Azure.IIoT.OpcUa.Publisher.Service.Services.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Models;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Application and endpoint registry services using the IoT Hub
    /// twin services for identity registration/retrieval.
    /// </summary>
    public sealed class ApplicationRegistry : IApplicationRegistry, IEndpointRegistry,
        IApplicationBulkProcessor
    {
        /// <summary>
        /// Create endpoint registry
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="endpointEvents"></param>
        /// <param name="applicationEvents"></param>
        /// <param name="timeProvider"></param>
        public ApplicationRegistry(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            ILogger<ApplicationRegistry> logger, IEndpointRegistryListener? endpointEvents = null,
            IApplicationRegistryListener? applicationEvents = null, TimeProvider? timeProvider = null)
        {
            _iothub = iothub;
            _logger = logger;
            _serializer = serializer;
            _endpointEvents = endpointEvents;
            _applicationEvents = applicationEvents;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResponseModel> RegisterApplicationAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.ApplicationUri == null)
            {
                throw new ArgumentException("Application Uri missing", nameof(request));
            }

            var context = request.Context.Validate(_timeProvider);

            var application = await AddOrUpdateApplicationAsync(
                request.ToApplicationInfo(context), null, false, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Application could not be added.");
            if (_applicationEvents != null)
            {
                await _applicationEvents.OnApplicationNewAsync(context,
                    application).ConfigureAwait(false);
            }

            await HandleApplicationEnabledAsync(context, application).ConfigureAwait(false);

            return new ApplicationRegistrationResponseModel
            {
                Id = application.ApplicationId
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> AddDiscoveredApplicationAsync(
            ApplicationRegistrationModel application, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(application);
            if (application.Application?.DiscovererId == null)
            {
                throw new ArgumentException("Discoverer identity missing", nameof(application));
            }
            if (application.Application?.ApplicationUri == null)
            {
                throw new ArgumentException("Application Uri missing", nameof(application));
            }

            var registration = application.Application with
            {
                Created = application.Application.Created.Validate(_timeProvider),
                Updated = null
            };
            registration = await AddOrUpdateApplicationAsync(registration,
                false, true, ct).ConfigureAwait(false);
            if (registration == null)
            {
                throw new InvalidOperationException("Application could not be added.");
            }
            // Add endpoints
            IReadOnlyList<EndpointInfoModel>? endpoints = null;
            if (application.Endpoints != null)
            {
                endpoints = await AddEndpointsAsync(application.Endpoints
                    .Select(epRegistration => new EndpointInfoModel
                    {
                        ApplicationId = registration.ApplicationId,
                        Registration = epRegistration with
                        {
                            DiscovererId = registration.DiscovererId,
                            SiteId = registration.SiteId
                        }
                    }), registration.Created, true, application.Application.DiscovererId,
                    registration.ApplicationId, false).ConfigureAwait(false);
            }

            if (_applicationEvents != null)
            {
                await _applicationEvents.OnApplicationNewAsync(registration.Created,
                    registration).ConfigureAwait(false);
            }

            await HandleApplicationEnabledAsync(registration.Created,
                registration).ConfigureAwait(false);

            return new ApplicationRegistrationModel
            {
                Application = registration,
                Endpoints = endpoints?.Select(e => e.Registration).ToList()
            };
        }

        /// <inheritdoc/>
        public async Task DisableApplicationAsync(string applicationId,
            OperationContextModel? context, CancellationToken ct)
        {
            context = context.Validate(_timeProvider);

            var app = await UpdateApplicationAsync(applicationId, (application, disabled) =>
            {
                // Disable application
                if (!(disabled ?? false))
                {
                    application.NotSeenSince = _timeProvider.GetUtcNow();
                    application.Updated = context;
                    return (true, true);
                }
                return (null, null);
            }, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Failed to update application.");
            await HandleApplicationDisabledAsync(context, app).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task EnableApplicationAsync(string applicationId,
            OperationContextModel? context, CancellationToken ct)
        {
            context = context.Validate(_timeProvider);

            var app = await UpdateApplicationAsync(applicationId, (application, disabled) =>
            {
                // Enable application
                if (disabled ?? false)
                {
                    application.NotSeenSince = null;
                    application.Updated = context;
                    return (true, false);
                }
                return (null, null);
            }, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Failed to update application.");
            if (_applicationEvents != null)
            {
                await _applicationEvents.OnApplicationEnabledAsync(context,
                    app).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task UnregisterApplicationAsync(string applicationId,
            OperationContextModel? context, CancellationToken ct)
        {
            context = context.Validate(_timeProvider);

            await DeleteEndpointsAsync(context, applicationId).ConfigureAwait(false);

            var app = await DeleteApplicationAsync(applicationId, null, ct).ConfigureAwait(false);
            if (app == null)
            {
                return;
            }
            if (_applicationEvents != null)
            {
                await _applicationEvents.OnApplicationDeletedAsync(context,
                    applicationId, app).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            var context = request.Context.Validate(_timeProvider);

            var application = await UpdateApplicationAsync(applicationId, (existing, _) =>
            {
                existing.Patch(request);
                existing.Updated = context;
                return (true, null);
            }, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Failed to update application.");
            if (_applicationEvents != null)
            {
                await _applicationEvents.OnApplicationUpdatedAsync(context,
                    application).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize, CancellationToken ct)
        {
            var sql = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{Constants.EntityTypeApplication}' ";

            if (!(query?.IncludeNotSeenSince ?? false))
            {
                // Scope to non deleted applications
                sql += $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)}) ";
            }

            if (query?.Locale != null)
            {
                if (query.ApplicationName != null)
                {
                    // If application name provided, include it in search
                    sql += $"AND tags.{nameof(ApplicationRegistration.LocalizedNames)}" +
                        $".{query.Locale} = '{query.ApplicationName}' ";
                }
                else
                {
                    // Just search for locale
                    sql += $"AND IS_DEFINED(tags.{nameof(ApplicationRegistration.LocalizedNames)}" +
                        $".{query.Locale}) ";
                }
            }
            else if (query?.ApplicationName != null)
            {
                // If application name provided, search for default name
                sql += $"AND tags.{nameof(ApplicationRegistration.ApplicationName)} = " +
                    $"'{query.ApplicationName}' ";
            }
            if (query?.DiscovererId != null)
            {
                // If discoverer provided, include it in search
                sql += $"AND tags.{nameof(ApplicationRegistration.DiscovererId)} = " +
                    $"'{query.DiscovererId}' ";
            }
            if (query?.ProductUri != null)
            {
                // If product uri provided, include it in search
                sql += $"AND tags.{nameof(ApplicationRegistration.ProductUri)} = " +
                    $"'{query.ProductUri}' ";
            }
            if (query?.GatewayServerUri != null)
            {
                // If gateway uri provided, include it in search
                sql += $"AND tags.{nameof(ApplicationRegistration.GatewayServerUri)} = " +
                    $"'{query.GatewayServerUri}' ";
            }
            if (query?.DiscoveryProfileUri != null)
            {
                // If discovery profile uri provided, include it in search
                sql += $"AND tags.{nameof(ApplicationRegistration.DiscoveryProfileUri)} = " +
                    $"'{query.DiscoveryProfileUri}' ";
            }
            if (query?.ApplicationUri != null)
            {
                // If ApplicationUri provided, include it in search
#pragma warning disable CA1308 // Normalize strings to uppercase
                sql += $"AND tags.{nameof(ApplicationRegistration.ApplicationUriLC)} = " +
                    $"'{query.ApplicationUri.ToLowerInvariant()}' ";
#pragma warning restore CA1308 // Normalize strings to uppercase
            }
            if (query?.ApplicationType is ApplicationType.Client or
                ApplicationType.ClientAndServer)
            {
                // If searching for clients include it in search
                sql += $"AND tags.{nameof(ApplicationType.Client)} = true ";
            }
            if (query?.ApplicationType is ApplicationType.Server or
                ApplicationType.ClientAndServer)
            {
                // If searching for servers include it in search
                sql += $"AND tags.{nameof(ApplicationType.Server)} = true ";
            }
            if (query?.ApplicationType == ApplicationType.DiscoveryServer)
            {
                // If searching for servers include it in search
                sql += $"AND tags.{nameof(ApplicationType.DiscoveryServer)} = true ";
            }
            if (query?.Capability != null)
            {
                // If Capabilities provided, filter results
                var tag = query.Capability.SanitizePropertyName().ToUpperInvariant();
                sql += $"AND tags.{tag} = true ";
            }
            if (query?.SiteOrGatewayId != null)
            {
                // If site or gateway id search provided, include it in search
                sql += $"AND tags.{nameof(EntityRegistration.SiteOrGatewayId)} = " +
                    $"'{query.SiteOrGatewayId}' ";
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(sql, null, pageSize, ct: ct).ConfigureAwait(false);
            return new ApplicationInfoListModel
            {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToApplicationRegistration())
                    .Select(s => s.ToServiceModel()!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(string? continuation,
            int? pageSize, CancellationToken ct)
        {
            const string tag = nameof(EntityRegistration.SiteOrGatewayId);
            const string sql = $"SELECT tags.{tag}, COUNT() FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{Constants.EntityTypeApplication}' " +
                $"GROUP BY tags.{tag}";
            var result = await _iothub.QueryAsync(sql, continuation, pageSize,
                ct).ConfigureAwait(false);
            return new ApplicationSiteListModel
            {
                ContinuationToken = result.ContinuationToken,
                Sites = result.Result
                    .Select(o => o.GetValueOrDefault<string>(tag)!)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveEndpoints, CancellationToken ct)
        {
            var registration = await GetApplicationRegistrationAsync(applicationId,
                ct).ConfigureAwait(false);
            var application = registration.ToServiceModel()
                ?? throw new ResourceNotFoundException("Found registration is invalid.");

            // Include deleted twins if the application itself is deleted.  Otherwise omit.
            var endpoints = await GetEndpointsAsync(applicationId,
                application.NotSeenSince != null, ct).ConfigureAwait(false);
            return new ApplicationRegistrationModel
            {
                Application = application,
                Endpoints = endpoints
                    .Select(e => e.ToServiceModel()?.Registration!)
                    .Where(r => r != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string? continuation, int? pageSize, CancellationToken ct)
        {
            const string sql = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{Constants.EntityTypeApplication}' ";
            var result = await _iothub.QueryDeviceTwinsAsync(sql, continuation,
                pageSize, ct).ConfigureAwait(false);
            return new ApplicationInfoListModel
            {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(t => t.ToApplicationRegistration())
                    .Select(s => s.ToServiceModel()!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor,
            OperationContextModel? context, CancellationToken ct)
        {
            context = context.Validate(_timeProvider);
            var absolute = _timeProvider.GetUtcNow() - notSeenFor;
            string? continuation = null;
            do
            {
                var applications = await ListApplicationsAsync(continuation,
                    null, ct).ConfigureAwait(false);
                continuation = applications?.ContinuationToken;
                if (applications?.Items == null)
                {
                    continue;
                }
                foreach (var application in applications.Items)
                {
                    if (application.NotSeenSince == null ||
                        application.NotSeenSince.Value >= absolute)
                    {
                        // Skip
                        continue;
                    }
                    try
                    {
                        await DeleteEndpointsAsync(context,
                            application.ApplicationId).ConfigureAwait(false);

                        // Delete if disabled state is reflected in the query result
                        var app = await DeleteApplicationAsync(application.ApplicationId,
                            _ => application.NotSeenSince < absolute, ct).ConfigureAwait(false);
                        if (app == null)
                        {
                            // Skip - already deleted or not satisfying condition
                            continue;
                        }
                        if (_applicationEvents != null)
                        {
                            await _applicationEvents.OnApplicationDeletedAsync(context,
                                app.ApplicationId, app).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception purging application {Id} - continue",
                            application.ApplicationId);
                    }
                }
            }
            while (continuation != null);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            bool onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var device = await _iothub.GetAsync(endpointId, null, ct).ConfigureAwait(false);
            return TwinModelToEndpointRegistrationModel(device, onlyServerState, false)
                ?? throw new ResourceNotFoundException("Invalid endpoint found.");
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(string? continuation,
            bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            // Find all devices where endpoint information is configured
            const string query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{Constants.EntityTypeEndpoint}'";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation,
                pageSize, ct).ConfigureAwait(false);

            return new EndpointInfoListModel
            {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(d => TwinModelToEndpointRegistrationModel(d, onlyServerState, true)!)
                    .Where(x => x != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query, bool onlyServerState, int? pageSize,
            CancellationToken ct)
        {
            var sql = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{Constants.EntityTypeEndpoint}' ";

            if (!(query?.IncludeNotSeenSince ?? false))
            {
                // Scope to non deleted twins
                sql += $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)}) ";
            }
            if (query?.Url != null)
            {
                // If Url provided, include it in search
#pragma warning disable CA1308 // Normalize strings to uppercase
                sql += $"AND tags.{nameof(EndpointRegistration.EndpointUrlLC)} = " +
                    $"'{query.Url.ToLowerInvariant()}' ";
#pragma warning restore CA1308 // Normalize strings to uppercase
            }
            if (query?.ApplicationId != null)
            {
                // If application id provided, include it in search
                sql += $"AND tags.{nameof(EndpointRegistration.ApplicationId)} = " +
                    $"'{query.ApplicationId}' ";
            }
            if (query?.DiscovererId != null)
            {
                // If discoverer provided, include it in search
                sql += $"AND tags.{nameof(EndpointRegistration.DiscovererId)} = " +
                    $"'{query.DiscovererId}' ";
            }
            if (query?.SiteOrGatewayId != null)
            {
                // If site or gateway provided, include it in search
                sql += $"AND tags.{nameof(EntityRegistration.SiteOrGatewayId)} = " +
                    $"'{query.SiteOrGatewayId}' ";
            }
            if (query?.Certificate != null)
            {
                // If cert thumbprint provided, include it in search
                sql += $"AND tags.{nameof(EndpointRegistration.Thumbprint)} = " +
                    $"{query.Certificate} ";
            }
            if (query?.SecurityMode != null)
            {
                // If SecurityMode provided, include it in search
                sql += $"AND properties.desired.{nameof(EndpointRegistration.SecurityMode)} = " +
                    $"'{query.SecurityMode}' ";
            }
            if (query?.SecurityPolicy != null)
            {
                // If SecurityPolicy uri provided, include it in search
                sql += $"AND properties.desired.{nameof(EndpointRegistration.SecurityPolicy)} = " +
                    $"'{query.SecurityPolicy}' ";
            }
            var result = await _iothub.QueryDeviceTwinsAsync(sql, null,
                pageSize, ct).ConfigureAwait(false);
            return new EndpointInfoListModel
            {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(t => t.ToEndpointRegistration(onlyServerState)?.ToServiceModel()!)
                    .Where(s => s != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryEventsAsync(string siteId, string discovererId,
            DiscoveryResultModel result, IReadOnlyList<DiscoveryEventModel> events)
        {
            if (string.IsNullOrEmpty(siteId))
            {
                throw new ArgumentNullException(nameof(siteId));
            }
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            ArgumentNullException.ThrowIfNull(result);
            var context = result.Context.Validate(_timeProvider);

            //
            // Get all applications for this discoverer or the site the application
            // was found in.  There should only be one site in the found application set
            // or none, otherwise, throw.  The OR covers where site of a discoverer was
            // changed after a discovery run (same discoverer that registered, but now
            // different site reported).
            //
            var sql = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{Constants.EntityTypeApplication}' AND " +
                $"(tags.{nameof(ApplicationRegistration.SiteId)} = '{siteId}' OR" +
                $" tags.{nameof(ApplicationRegistration.DiscovererId)} = '{discovererId}')";

            var twins = await _iothub.QueryAllDeviceTwinsAsync(sql).ConfigureAwait(false);
            var existing = twins
                .Select(t => t.ToApplicationRegistration()?.ToServiceModel()!)
                .Where(s => s != null)
                .ToList();

            var found = events.Where(ev => ev.Application != null).Select(ev =>
            {
                //
                // Ensure we set the site id and discoverer id in the found applications
                // to a consistent value.  This works around where the reported events
                // do not contain what we were asked to process with.
                //
                ev.Application!.SiteId = siteId;
                ev.Application.DiscovererId = discovererId;
                return ev.Application;
            }).ToList();

            // Create endpoints lookup table per found application id
            var endpoints = events.Where(ev => ev.Application != null)
                .GroupBy(k => k.Application!.ApplicationId).ToDictionary(
                group => group.Key,
                group => group
                    .Where(ev => ev.Registration != null)
                    .Select(ev =>
                    {
                        //
                        // Ensure the site id and discoverer id in the found endpoints
                        // also set to a consistent value, same as applications earlier.
                        //
                        ev.Registration!.SiteId = siteId;
                        ev.Registration.DiscovererId = discovererId;
                        return new EndpointInfoModel
                        {
                            ApplicationId = group.Key,
                            Registration = ev.Registration
                        };
                    })
                    .ToList());

            //
            // Merge found with existing applications. For disabled applications this will
            // take ownership regardless of discoverer, unfound applications are only disabled
            // and existing ones are patched only if they were previously reported by the same
            // discoverer.  New ones are simply added.
            //
            var remove = new HashSet<ApplicationInfoModel>(existing,
                ApplicationInfoModelEx.LogicalEquality);
            var add = new HashSet<ApplicationInfoModel>(found,
                ApplicationInfoModelEx.LogicalEquality);
            var unchange = new HashSet<ApplicationInfoModel>(existing,
                ApplicationInfoModelEx.LogicalEquality);
            var change = new HashSet<ApplicationInfoModel>(found,
                ApplicationInfoModelEx.LogicalEquality);

            unchange.IntersectWith(add);
            change.IntersectWith(remove);
            remove.ExceptWith(found);
            add.ExceptWith(existing);

            var added = 0;
            var updated = 0;
            var unchanged = 0;
            var removed = 0;

            if (!(result.RegisterOnly ?? false))
            {
                // Remove applications
                foreach (var removal in remove)
                {
                    try
                    {
                        // Only touch applications the discoverer owns.
                        if (removal.DiscovererId == discovererId)
                        {
                            var wasUpdated = false;

                            // Disable if not already disabled
                            var app = await UpdateApplicationAsync(removal.ApplicationId,
                                (application, disabled) =>
                                {
                                    // Disable application
                                    if (!(disabled ?? false))
                                    {
                                        application.NotSeenSince = _timeProvider.GetUtcNow();
                                        application.Updated = context;
                                        removed++;
                                        wasUpdated = true;
                                        return (true, true);
                                    }
                                    unchanged++;
                                    return (null, null);
                                }, default).ConfigureAwait(false)
                                    ?? throw new InvalidOperationException("Failed to update application.");
                            if (wasUpdated && _applicationEvents != null)
                            {
                                await _applicationEvents.OnApplicationUpdatedAsync(context,
                                    app).ConfigureAwait(false);
                            }

                            await HandleApplicationDisabledAsync(context, app).ConfigureAwait(false);
                        }
                        else
                        {
                            // Skip the ones owned by other discoverers
                            unchanged++;
                        }
                    }
                    catch (Exception ex)
                    {
                        unchanged++;
                        _logger.LogError(ex, "Exception during application disabling.");
                    }
                }
            }

            // ... add brand new applications
            foreach (var addition in add)
            {
                try
                {
                    var application = addition.Clone(_timeProvider);
                    application.SiteId = siteId;
                    application.ApplicationId = ApplicationInfoModelEx.CreateApplicationId(application);
                    application.Created = context;
                    application.NotSeenSince = null;
                    application.DiscovererId = discovererId;

                    var app = await AddOrUpdateApplicationAsync(application, false,
                        false, default).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("Failed to add or update application.");
                    // Notify addition!
                    if (_applicationEvents != null)
                    {
                        await _applicationEvents.OnApplicationNewAsync(context,
                            app).ConfigureAwait(false);
                    }

                    await HandleApplicationEnabledAsync(context, app).ConfigureAwait(false);

                    // Now - add all new endpoints
                    if (endpoints.TryGetValue(app.ApplicationId, out var epFound))
                    {
                        await AddEndpointsAsync(epFound, result.Context, result.RegisterOnly ?? false,
                            discovererId, null, false).ConfigureAwait(false);
                    }

                    added++;
                }
                catch (ResourceConflictException)
                {
                    unchange.Add(addition); // Update the existing one
                }
                catch (Exception ex)
                {
                    unchanged++;
                    _logger.LogError(ex, "Exception adding application from discovery.");
                }
            }

            // Update applications and endpoints ...
            foreach (var update in unchange)
            {
                try
                {
                    var wasDisabled = false;
                    var wasUpdated = false;

                    // Disable if not already disabled
                    var app = await UpdateApplicationAsync(update.ApplicationId,
                        (application, disabled) =>
                        {
                            //
                            // Check whether another discoverer owns this application (discoverer
                            // id are not the same) and it is not disabled before updating it it.
                            //
                            if (update.DiscovererId != discovererId && !(disabled ?? false))
                            {
                                // TODO: Decide whether we merge newly found endpoints...
                                unchanged++;
                                return (null, null);
                            }

                            wasDisabled = (disabled ?? false) && application.NotSeenSince != null;
                            wasUpdated = true;

                            application.Update(update);
                            application.DiscovererId = discovererId;
                            application.SiteId = siteId;
                            application.NotSeenSince = null;
                            application.Updated = context;
                            updated++;
                            return (true, false);
                        }, default).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("Failed to update application.");
                    if (wasDisabled)
                    {
                        await HandleApplicationEnabledAsync(context, app).ConfigureAwait(false);
                    }

                    // If this is our discoverer's application we update all endpoints also.
                    if (wasUpdated && endpoints.TryGetValue(app.ApplicationId, out var epFound))
                    {
                        // TODO: Handle case where we take ownership of all endpoints
                        await AddEndpointsAsync(epFound, result.Context,
                            result.RegisterOnly ?? false, discovererId,
                            app.ApplicationId, false).ConfigureAwait(false);

                        if (_applicationEvents != null)
                        {
                            await _applicationEvents.OnApplicationUpdatedAsync(context,
                                app).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    unchanged++;
                    _logger.LogError(ex, "Exception during update.");
                }
            }

            var log = added != 0 || removed != 0 || updated != 0;
            _logger.LogInformation("... processed discovery results from {DiscovererId}: " +
                "{Added} applications added, {Updated} updated, {Removed} disabled, and " +
                "{Unchanged} unchanged.", discovererId, added, updated, removed, unchanged);
            kAppsAdded.Add(added);
            kAppsUpdated.Add(updated);
            kAppsUnchanged.Add(unchanged);
        }

        /// <summary>
        /// Register endpoints
        /// </summary>
        /// <param name="newEndpoints"></param>
        /// <param name="context"></param>
        /// <param name="registerOnly"></param>
        /// <param name="discovererId"></param>
        /// <param name="applicationId"></param>
        /// <param name="hardDelete"></param>
        /// <returns></returns>
        /// <exception cref="ResourceInvalidStateException"></exception>
        private async Task<IReadOnlyList<EndpointInfoModel>> AddEndpointsAsync(
            IEnumerable<EndpointInfoModel> newEndpoints, OperationContextModel? context,
            bool registerOnly, string discovererId, string? applicationId,
            bool hardDelete)
        {
            context = context.Validate(_timeProvider);
            var found = newEndpoints
                .Select(e => e.ToEndpointRegistration(false, discovererId))
                .ToList();

            var existing = Enumerable.Empty<EndpointRegistration>();
            if (!string.IsNullOrEmpty(applicationId))
            {
                // Merge with existing endpoints of the application
                existing = await GetEndpointsAsync(applicationId, true).ConfigureAwait(false);
            }

            var remove = new HashSet<EndpointRegistration>(existing,
                EndpointRegistrationEx.Logical);
            var add = new HashSet<EndpointRegistration>(found,
                EndpointRegistrationEx.Logical);
            var unchange = new HashSet<EndpointRegistration>(existing,
                EndpointRegistrationEx.Logical);
            var change = new HashSet<EndpointRegistration>(found,
                EndpointRegistrationEx.Logical);

            unchange.IntersectWith(add);
            change.IntersectWith(remove);
            remove.ExceptWith(found);
            add.ExceptWith(existing);

            var added = 0;
            var updated = 0;
            var unchanged = 0;
            var removed = 0;

            var all = new List<EndpointInfoModel>();

            if (!registerOnly)
            {
                // Remove or disable an endpoint
                foreach (var item in remove)
                {
                    try
                    {
                        if (item.DeviceId == null)
                        {
                            throw new ResourceInvalidStateException("Bad item found during discovery.");
                        }
                        // Only touch applications the discoverer owns.
                        if (item.DiscovererId == discovererId)
                        {
                            if (hardDelete)
                            {
                                var device = await _iothub.GetAsync(item.DeviceId).ConfigureAwait(false);
                                // First we update any registration
                                var existingEndpoint = device.ToEndpointRegistration(false);

                                // Then hard delete...
                                await _iothub.DeleteAsync(item.DeviceId).ConfigureAwait(false);
                                if (_endpointEvents != null)
                                {
                                    var endpoint = item.ToServiceModel();
                                    Debug.Assert(endpoint != null);
                                    await _endpointEvents.OnEndpointDeletedAsync(context,
                                        item.DeviceId, endpoint).ConfigureAwait(false);
                                }
                            }
                            else if (!(item.IsDisabled ?? false))
                            {
                                var endpoint = item.ToServiceModel();
                                Debug.Assert(endpoint != null);
                                var update = endpoint.ToEndpointRegistration(true);
                                await _iothub.PatchAsync(item.Patch(update, _serializer, _timeProvider),
                                    true).ConfigureAwait(false);
                                if (_endpointEvents != null)
                                {
                                    await _endpointEvents.OnEndpointDisabledAsync(context,
                                        endpoint).ConfigureAwait(false);
                                }

                                all.Add(endpoint);
                            }
                            else
                            {
                                var endpoint = item.ToServiceModel();
                                Debug.Assert(endpoint != null);
                                all.Add(endpoint);
                                unchanged++;
                                continue;
                            }
                            removed++;
                        }
                        else
                        {
                            // Skip the ones owned by other publishers
                            var endpoint = item.ToServiceModel();
                            Debug.Assert(endpoint != null);
                            all.Add(endpoint);
                            unchanged++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var endpoint = item.ToServiceModel();
                        Debug.Assert(endpoint != null);
                        all.Add(endpoint);
                        unchanged++;
                        _logger.LogError(ex, "Exception during discovery removal.");
                    }
                }
            }

            // Update endpoints that were disabled
            foreach (var exists in unchange)
            {
                try
                {
                    // Get the new one we will patch over the existing one...
                    var patch = change.First(x =>
                        EndpointRegistrationEx.Logical.Equals(x, exists));

                    if (exists != patch)
                    {
                        await _iothub.PatchAsync(exists.Patch(patch, _serializer, _timeProvider),
                            true).ConfigureAwait(false);
                        var endpoint = patch.ToServiceModel()
                            ?? throw new ResourceInvalidStateException("Bad item provided during discovery");
                        // await OnEndpointUpdatedAsync(context, endpoint);
                        if ((exists.IsDisabled ?? false) && _endpointEvents != null)
                        {
                            await _endpointEvents.OnEndpointEnabledAsync(context,
                                endpoint).ConfigureAwait(false);
                        }

                        updated++;
                        all.Add(endpoint);
                    }
                    else
                    {
                        unchanged++;
                        var endpoint = patch.ToServiceModel();
                        Debug.Assert(endpoint != null);
                        all.Add(endpoint);
                    }
                }
                catch (Exception ex)
                {
                    unchanged++;
                    _logger.LogError(ex, "Exception during update.");

                    var endpoint = exists.ToServiceModel();
                    Debug.Assert(endpoint != null);
                    all.Add(endpoint);
                }
            }

            // Add endpoint
            foreach (var item in add)
            {
                try
                {
                    var created = await _iothub.CreateOrUpdateAsync(item.ToDeviceTwin(_serializer, _timeProvider),
                        true).ConfigureAwait(false);

                    var endpoint = item.ToServiceModel()
                        ?? throw new ResourceInvalidStateException("Bad item provided during discovery");
                    if (_endpointEvents != null)
                    {
                        await _endpointEvents.OnEndpointNewAsync(context,
                            endpoint).ConfigureAwait(false);

                        await _endpointEvents.OnEndpointEnabledAsync(context,
                            endpoint).ConfigureAwait(false);
                    }

                    all.Add(endpoint);
                    added++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception adding endpoint from discovery.");
                }
            }

            if (added != 0 || removed != 0)
            {
                _logger.LogInformation("processed endpoint results: {Added} endpoints added, {Updated} " +
                    "updated, {Removed} removed or disabled, and {Unchanged} unchanged.",
                    added, updated, removed, unchanged);
            }

            return all;
        }

        /// <summary>
        /// Update application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="updater"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        private async Task<ApplicationInfoModel?> UpdateApplicationAsync(string applicationId,
            Func<ApplicationInfoModel, bool?, (bool?, bool?)> updater, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var registration = await GetApplicationRegistrationAsync(applicationId, ct).ConfigureAwait(false);
                    // Update registration from update request
                    var application = registration.ToServiceModel();
                    if (application == null)
                    {
                        return null;
                    }
                    var (patch, disabled) = updater(application, registration.IsDisabled);
                    if (patch ?? false)
                    {
                        var update = application.ToApplicationRegistration(disabled);
                        var twin = await _iothub.PatchAsync(registration.Patch(update, _serializer,
                            _timeProvider), ct: ct).ConfigureAwait(false);
                        registration = twin.ToApplicationRegistration();
                    }
                    return registration.ToServiceModel();
                }
                catch (ResourceOutOfDateException ex)
                {
                    // Retry create/update
                    _logger.LogDebug(ex, "Retry updating application...");
                }
            }
            throw new OperationCanceledException();
        }

        /// <summary>
        /// Add application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="disabled"></param>
        /// <param name="allowUpdate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<ApplicationInfoModel?> AddOrUpdateApplicationAsync(
            ApplicationInfoModel application, bool? disabled, bool allowUpdate, CancellationToken ct)
        {
            var registration = application.ToApplicationRegistration(disabled);
            var twin = await _iothub.CreateOrUpdateAsync(
                registration.ToDeviceTwin(_serializer, _timeProvider), allowUpdate, ct).ConfigureAwait(false);
            return twin.ToApplicationRegistration()?.ToServiceModel();
        }

        /// <summary>
        /// Delete application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="precondition"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        private async Task<ApplicationInfoModel?> DeleteApplicationAsync(string applicationId,
            Func<ApplicationInfoModel?, bool>? precondition, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var registration = await FindApplicationRegistrationAsync(applicationId, ct).ConfigureAwait(false);
                    if (registration is null)
                    {
                        return null;
                    }
                    var application = registration.ToServiceModel();
                    if (precondition != null)
                    {
                        var shouldDelete = precondition(application);
                        if (!shouldDelete)
                        {
                            return null;
                        }
                    }
                    // Delete application
                    await _iothub.DeleteAsync(applicationId,
                        /* registration.Etag, */ ct: ct).ConfigureAwait(false);
                    // return deleted entity
                    return application;
                }
                catch (ResourceOutOfDateException ex)
                {
                    // Retry create/update
                    _logger.LogDebug(ex, "Retry deleting application...");
                }
            }
            throw new OperationCanceledException();
        }

        /// <summary>
        /// Get registration
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ResourceNotFoundException"></exception>
        private async Task<ApplicationRegistration> GetApplicationRegistrationAsync(
            string applicationId, CancellationToken ct)
        {
            return await FindApplicationRegistrationAsync(applicationId, ct).ConfigureAwait(false)
                ?? throw new ResourceNotFoundException("Not an application registration");
        }

        /// <summary>
        /// Find registration
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private async Task<ApplicationRegistration?> FindApplicationRegistrationAsync(
            string applicationId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            // Get existing application and compare to see if we need to patch.
            var twin = await _iothub.GetAsync(applicationId, null, ct).ConfigureAwait(false);
            if (twin.Id != applicationId)
            {
                throw new ArgumentException("Id must be same as application to patch",
                    nameof(applicationId));
            }

            // Convert to application registration
            return twin.ToEntityRegistration() as ApplicationRegistration;
        }

        /// <summary>
        /// Try handle enabling of application
        /// </summary>
        /// <param name="context"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        private async Task HandleApplicationEnabledAsync(OperationContextModel? context,
            ApplicationInfoModel application)
        {
            var endpoints = await GetEndpointsAsync(application.ApplicationId, true).ConfigureAwait(false);
            foreach (var registration in endpoints)
            {
                // Enable if disabled
                if (!(registration.IsDisabled ?? false))
                {
                    continue;
                }
                try
                {
                    var endpoint = registration.ToServiceModel();
                    if (endpoint == null)
                    {
                        continue;
                    }
                    endpoint.NotSeenSince = null;
                    var update = endpoint.ToEndpointRegistration(false);
                    await _iothub.PatchAsync(registration.Patch(update,
                        _serializer, _timeProvider)).ConfigureAwait(false);
                    if (_endpointEvents != null)
                    {
                        await _endpointEvents.OnEndpointEnabledAsync(context,
                            endpoint).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed re-enabling endpoint {Id}", registration.Id);
                }
            }
            if (_applicationEvents != null)
            {
                await _applicationEvents.OnApplicationEnabledAsync(context,
                    application).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handle disabling of application
        /// </summary>
        /// <param name="context"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        public async Task HandleApplicationDisabledAsync(OperationContextModel? context,
            ApplicationInfoModel application)
        {
            // Disable endpoints
            var endpoints = await GetEndpointsAsync(application.ApplicationId, true).ConfigureAwait(false);
            foreach (var registration in endpoints)
            {
                // Disable if enabled
                if (!(registration.IsDisabled ?? false))
                {
                    try
                    {
                        var endpoint = registration.ToServiceModel();
                        if (endpoint == null)
                        {
                            continue;
                        }
                        endpoint.NotSeenSince = _timeProvider.GetUtcNow();
                        var update = endpoint.ToEndpointRegistration(true);
                        await _iothub.PatchAsync(registration.Patch(update,
                            _serializer, _timeProvider)).ConfigureAwait(false);
                        if (_endpointEvents != null)
                        {
                            await _endpointEvents.OnEndpointDisabledAsync(context,
                                endpoint).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed disabling endpoint {Id}", registration.Id);
                    }
                }
            }
            if (_applicationEvents != null)
            {
                await _applicationEvents.HandleApplicationDisabledAsync(context,
                    application).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handle application deletion
        /// </summary>
        /// <param name="context"></param>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        private async Task DeleteEndpointsAsync(OperationContextModel? context,
            string applicationId)
        {
            // Get all endpoint registrations and for each one, call delete, if failure,
            // stop half way and throw and do not complete.
            var endpoints = await GetEndpointsAsync(applicationId, true).ConfigureAwait(false);
            foreach (var registration in endpoints)
            {
                if (registration.DeviceId == null)
                {
                    continue;
                }
                await _iothub.DeleteAsync(registration.DeviceId).ConfigureAwait(false);
                var endpoint = registration.ToServiceModel();
                if (_endpointEvents != null && endpoint?.Registration?.Id != null)
                {
                    await _endpointEvents.OnEndpointDeletedAsync(context,
                        endpoint.Registration.Id, endpoint).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Get all endpoints for application id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="includeDeleted"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IEnumerable<EndpointRegistration>> GetEndpointsAsync(
            string applicationId, bool includeDeleted, CancellationToken ct = default)
        {
            // Find all devices where endpoint information is configured
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EndpointRegistration.ApplicationId)} = " +
                    $"'{applicationId}' AND " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{Constants.EntityTypeEndpoint}' ";

            if (!includeDeleted)
            {
                query += $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            }

            var result = new List<DeviceTwinModel>();
            string? continuation = null;
            do
            {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, null, null, ct).ConfigureAwait(false);
                result.AddRange(devices.Items);
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
            return result
                .Select(d => d.ToEndpointRegistration(false)!)
                .Where(r => r is not null);
        }

        /// <summary>
        /// Convert device twin registration property to registration model
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState">Only desired should be returned
        /// this means that you will look at stale information.</param>
        /// <param name="skipInvalid"></param>
        /// <returns></returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        private static EndpointInfoModel? TwinModelToEndpointRegistrationModel(
            DeviceTwinModel twin, bool onlyServerState, bool skipInvalid)
        {
            // Convert to twin registration
            if (twin.ToEntityRegistration(onlyServerState) is not EndpointRegistration registration)
            {
                if (skipInvalid)
                {
                    return null;
                }
                throw new ResourceNotFoundException(
                    $"{twin.Id} is not a registered opc ua endpoint.");
            }
            return registration.ToServiceModel();
        }

        private static readonly Counter<int> kAppsAdded = Diagnostics.Meter.CreateCounter<int>(
            "iiot_registry_applicationAdded", "Number of applications added ");
        private static readonly Counter<int> kAppsUpdated = Diagnostics.Meter.CreateCounter<int>(
            "iiot_registry_applicationsUpdated", "Number of applications updated ");
        private static readonly Counter<int> kAppsUnchanged = Diagnostics.Meter.CreateCounter<int>(
            "iiot_registry_applicationUnchanged", "Number of applications unchanged ");

        private readonly IEndpointRegistryListener? _endpointEvents;
        private readonly IApplicationRegistryListener? _applicationEvents;
        private readonly TimeProvider _timeProvider;
        private readonly IJsonSerializer _serializer;
        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
