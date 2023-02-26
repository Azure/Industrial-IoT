// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Services
{
    using Azure.IIoT.OpcUa.Services.Registry.Models;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Extensions.Logging;
    using Prometheus;
    using System;
    using System.Collections.Generic;
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
        public ApplicationRegistry(IIoTHubTwinServices iothub, IJsonSerializer serializer,
            ILogger logger, IEndpointRegistryListener endpointEvents = null,
            IApplicationRegistryListener applicationEvents = null)
        {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _endpointEvents = endpointEvents;
            _applicationEvents = applicationEvents;
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResponseModel> RegisterApplicationAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ApplicationUri == null)
            {
                throw new ArgumentException("Application Uri missing", nameof(request));
            }

            var context = request.Context.Validate();

            var application = await AddApplicationAsync(request.ToApplicationInfo(context),
                null, ct).ConfigureAwait(false);

            await (_applicationEvents?.OnApplicationNewAsync(context, application)).ConfigureAwait(false);
            await HandleApplicationEnabledAsync(context, application).ConfigureAwait(false);

            return new ApplicationRegistrationResponseModel
            {
                Id = application.ApplicationId
            };
        }

        /// <inheritdoc/>
        public async Task DisableApplicationAsync(string applicationId,
            OperationContextModel context, CancellationToken ct)
        {
            context = context.Validate();

            var app = await UpdateApplicationAsync(applicationId, (application, disabled) =>
            {
                // Disable application
                if (!(disabled ?? false))
                {
                    application.NotSeenSince = DateTime.UtcNow;
                    application.Updated = context;
                    return (true, true);
                }
                return (null, null);
            }, ct).ConfigureAwait(false);

            await HandleApplicationDisabledAsync(context, app).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task EnableApplicationAsync(string applicationId,
            OperationContextModel context, CancellationToken ct)
        {
            context = context.Validate();

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
            }, ct).ConfigureAwait(false);

            await (_applicationEvents?.OnApplicationEnabledAsync(context, app)).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UnregisterApplicationAsync(string applicationId,
            OperationContextModel context, CancellationToken ct)
        {
            context = context.Validate();

            await DeleteEndpointsAsync(context, applicationId).ConfigureAwait(false);

            var app = await DeleteApplicationAsync(applicationId, null, ct).ConfigureAwait(false);
            if (app == null)
            {
                return;
            }
            await (_applicationEvents?.OnApplicationDeletedAsync(context, applicationId, app)).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var context = request.Context.Validate();

            var application = await UpdateApplicationAsync(applicationId, (existing, _) =>
            {
                existing.Patch(request);
                existing.Updated = context;
                return (true, null);
            }, ct).ConfigureAwait(false);

            // Send update to through broker
            await (_applicationEvents?.OnApplicationUpdatedAsync(context, application)).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel model, int? pageSize, CancellationToken ct)
        {
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Application}' ";

            if (!(model?.IncludeNotSeenSince ?? false))
            {
                // Scope to non deleted applications
                query += $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)}) ";
            }

            if (model?.Locale != null)
            {
                if (model?.ApplicationName != null)
                {
                    // If application name provided, include it in search
                    query += $"AND tags.{nameof(ApplicationRegistration.LocalizedNames)}" +
                        $".{model.Locale} = '{model.ApplicationName}' ";
                }
                else
                {
                    // Just search for locale
                    query += $"AND IS_DEFINED(tags.{nameof(ApplicationRegistration.LocalizedNames)}" +
                        $".{model.Locale}) ";
                }
            }
            else if (model?.ApplicationName != null)
            {
                // If application name provided, search for default name
                query += $"AND tags.{nameof(ApplicationRegistration.ApplicationName)} = " +
                    $"'{model.ApplicationName}' ";
            }
            if (model?.DiscovererId != null)
            {
                // If discoverer provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.DiscovererId)} = " +
                    $"'{model.DiscovererId}' ";
            }
            if (model?.ProductUri != null)
            {
                // If product uri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.ProductUri)} = " +
                    $"'{model.ProductUri}' ";
            }
            if (model?.GatewayServerUri != null)
            {
                // If gateway uri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.GatewayServerUri)} = " +
                    $"'{model.GatewayServerUri}' ";
            }
            if (model?.DiscoveryProfileUri != null)
            {
                // If discovery profile uri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.DiscoveryProfileUri)} = " +
                    $"'{model.DiscoveryProfileUri}' ";
            }
            if (model?.ApplicationUri != null)
            {
                // If ApplicationUri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.ApplicationUriLC)} = " +
                    $"'{model.ApplicationUri.ToLowerInvariant()}' ";
            }
            if (model?.ApplicationType is ApplicationType.Client or
                ApplicationType.ClientAndServer)
            {
                // If searching for clients include it in search
                query += $"AND tags.{nameof(ApplicationType.Client)} = true ";
            }
            if (model?.ApplicationType is ApplicationType.Server or
                ApplicationType.ClientAndServer)
            {
                // If searching for servers include it in search
                query += $"AND tags.{nameof(ApplicationType.Server)} = true ";
            }
            if (model?.ApplicationType == ApplicationType.DiscoveryServer)
            {
                // If searching for servers include it in search
                query += $"AND tags.{nameof(ApplicationType.DiscoveryServer)} = true ";
            }
            if (model?.Capability != null)
            {
                // If Capabilities provided, filter results
                var tag = SanitizePropertyName(model.Capability)
                    .ToUpperInvariant();
                query += $"AND tags.{tag} = true ";
            }
            if (model?.SiteOrGatewayId != null)
            {
                // If site or gateway id search provided, include it in search
                query += $"AND tags.{nameof(EntityRegistration.SiteOrGatewayId)} = " +
                    $"'{model.SiteOrGatewayId}' ";
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize, ct: ct).ConfigureAwait(false);
            return new ApplicationInfoListModel
            {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToApplicationRegistration())
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct)
        {
            const string tag = nameof(EntityRegistration.SiteOrGatewayId);
            var query = $"SELECT tags.{tag}, COUNT() FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Application}' " +
                $"GROUP BY tags.{tag}";
            var result = await _iothub.QueryAsync(query, continuation, pageSize, ct).ConfigureAwait(false);
            return new ApplicationSiteListModel
            {
                ContinuationToken = result.ContinuationToken,
                Sites = result.Result
                    .Select(o => o.GetValueOrDefault<string>(tag))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveTwins, CancellationToken ct)
        {
            var registration = await GetApplicationRegistrationAsync(applicationId, true, ct).ConfigureAwait(false);
            var application = registration.ToServiceModel();

            // Include deleted twins if the application itself is deleted.  Otherwise omit.
            var endpoints = await GetEndpointsAsync(applicationId,
                application.NotSeenSince != null, ct).ConfigureAwait(false);
            return new ApplicationRegistrationModel
            {
                Application = application,
                Endpoints = endpoints
                    .Select(e => e.ToServiceModel())
                    .Select(ep => ep.Registration)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize, CancellationToken ct)
        {
            const string query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Application}' ";
            var result = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct).ConfigureAwait(false);
            return new ApplicationInfoListModel
            {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(t => t.ToApplicationRegistration())
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenSince,
            OperationContextModel context, CancellationToken ct)
        {
            context = context.Validate();
            var absolute = DateTime.UtcNow - notSeenSince;
            string continuation = null;
            do
            {
                var applications = await ListApplicationsAsync(continuation, null, ct).ConfigureAwait(false);
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
                        await DeleteEndpointsAsync(context, application.ApplicationId).ConfigureAwait(false);

                        // Delete if disabled state is reflected in the query result
                        var app = await DeleteApplicationAsync(application.ApplicationId,
                            _ => application.NotSeenSince < absolute, ct).ConfigureAwait(false);
                        if (app == null)
                        {
                            // Skip - already deleted or not satisfying condition
                            continue;
                        }
                        await (_applicationEvents?.OnApplicationDeletedAsync(context,
                            app.ApplicationId, app)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception purging application {Id} - continue",
                            application.ApplicationId);
                        continue;
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
            return TwinModelToEndpointRegistrationModel(device, onlyServerState, false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(string continuation,
            bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            // Find all devices where endpoint information is configured
            const string query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Endpoint}'";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct).ConfigureAwait(false);

            return new EndpointInfoListModel
            {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(d => TwinModelToEndpointRegistrationModel(d, onlyServerState, true))
                    .Where(x => x != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel model, bool onlyServerState, int? pageSize,
            CancellationToken ct)
        {
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Endpoint}' ";

            if (!(model?.IncludeNotSeenSince ?? false))
            {
                // Scope to non deleted twins
                query += $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)}) ";
            }
            if (model?.Url != null)
            {
                // If Url provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.EndpointUrlLC)} = " +
                    $"'{model.Url.ToLowerInvariant()}' ";
            }
            if (model?.ApplicationId != null)
            {
                // If application id provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.ApplicationId)} = " +
                    $"'{model.ApplicationId}' ";
            }
            if (model?.DiscovererId != null)
            {
                // If discoverer provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.DiscovererId)} = " +
                    $"'{model.DiscovererId}' ";
            }
            if (model?.SiteOrGatewayId != null)
            {
                // If site or gateway provided, include it in search
                query += $"AND tags.{nameof(EntityRegistration.SiteOrGatewayId)} = " +
                    $"'{model.SiteOrGatewayId}' ";
            }
            if (model?.Certificate != null)
            {
                // If cert thumbprint provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.Thumbprint)} = " +
                    $"{model.Certificate} ";
            }
            if (model?.SecurityMode != null)
            {
                // If SecurityMode provided, include it in search
                query += $"AND properties.desired.{nameof(EndpointRegistration.SecurityMode)} = " +
                    $"'{model.SecurityMode}' ";
            }
            if (model?.SecurityPolicy != null)
            {
                // If SecurityPolicy uri provided, include it in search
                query += $"AND properties.desired.{nameof(EndpointRegistration.SecurityPolicy)} = " +
                    $"'{model.SecurityPolicy}' ";
            }
            var result = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize, ct).ConfigureAwait(false);
            return new EndpointInfoListModel
            {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(t => t.ToEndpointRegistration(onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public Task<ApplicationRegistrationModel> AddDiscoveredApplicationAsync(
            ApplicationRegistrationModel application, CancellationToken ct = default)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryEventsAsync(string siteId, string discovererId,
            DiscoveryResultModel result, IEnumerable<DiscoveryEventModel> events)
        {
            if (string.IsNullOrEmpty(siteId))
            {
                throw new ArgumentNullException(nameof(siteId));
            }
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            var context = result.Context.Validate();

            //
            // Get all applications for this discoverer or the site the application
            // was found in.  There should only be one site in the found application set
            // or none, otherwise, throw.  The OR covers where site of a discoverer was
            // changed after a discovery run (same discoverer that registered, but now
            // different site reported).
            //
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Application}' AND " +
                $"(tags.{nameof(ApplicationRegistration.SiteId)} = '{siteId}' OR" +
                $" tags.{nameof(ApplicationRegistration.DiscovererId)} = '{discovererId}')";

            var twins = await _iothub.QueryAllDeviceTwinsAsync(query).ConfigureAwait(false);
            var existing = twins
                .Select(t => t.ToApplicationRegistration())
                .Select(a => a.ToServiceModel());

            var found = events.Select(ev =>
            {
                //
                // Ensure we set the site id and discoverer id in the found applications
                // to a consistent value.  This works around where the reported events
                // do not contain what we were asked to process with.
                //
                ev.Application.SiteId = siteId;
                ev.Application.DiscovererId = discovererId;
                return ev.Application;
            });

            // Create endpoints lookup table per found application id
            var endpoints = events.GroupBy(k => k.Application.ApplicationId).ToDictionary(
                group => group.Key,
                group => group
                    .Select(ev =>
                    {
                        //
                        // Ensure the site id and discoverer id in the found endpoints
                        // also set to a consistent value, same as applications earlier.
                        //
                        ev.Registration.SiteId = siteId;
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
                                        application.NotSeenSince = DateTime.UtcNow;
                                        application.Updated = context;
                                        removed++;
                                        wasUpdated = true;
                                        return (true, true);
                                    }
                                    unchanged++;
                                    return (null, null);
                                }, default).ConfigureAwait(false);

                            if (wasUpdated)
                            {
                                await (_applicationEvents?.OnApplicationUpdatedAsync(context, app)).ConfigureAwait(false);
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
                    var application = addition.Clone();
                    application.ApplicationId =
                        ApplicationInfoModelEx.CreateApplicationId(application);
                    application.Created = context;
                    application.NotSeenSince = null;
                    application.DiscovererId = discovererId;
                    application.SiteId = siteId;

                    var app = await AddApplicationAsync(application, false, default).ConfigureAwait(false);

                    // Notify addition!
                    await (_applicationEvents?.OnApplicationNewAsync(context, app)).ConfigureAwait(false);
                    await HandleApplicationEnabledAsync(context, app).ConfigureAwait(false);

                    // Now - add all new endpoints
                    endpoints.TryGetValue(app.ApplicationId, out var epFound);
                    await ProcessDiscoveryEventsAsync(epFound, result,
                        discovererId, null, false).ConfigureAwait(false);
                    added++;
                }
                catch (ConflictingResourceException)
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
                        }, default).ConfigureAwait(false);

                    if (wasDisabled)
                    {
                        await HandleApplicationEnabledAsync(context, app).ConfigureAwait(false);
                    }

                    if (wasUpdated)
                    {
                        // If this is our discoverer's application we update all endpoints also.
                        endpoints.TryGetValue(app.ApplicationId, out var epFound);

                        // TODO: Handle case where we take ownership of all endpoints
                        await ProcessDiscoveryEventsAsync(epFound, result, discovererId,
                            app.ApplicationId, false).ConfigureAwait(false);

                        await (_applicationEvents?.OnApplicationUpdatedAsync(context, app)).ConfigureAwait(false);
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
            kAppsAdded.Set(added);
            kAppsUpdated.Set(updated);
            kAppsUnchanged.Set(unchanged);
        }

        /// <inheritdoc/>
        private async Task ProcessDiscoveryEventsAsync(IEnumerable<EndpointInfoModel> newEndpoints,
            DiscoveryResultModel result, string discovererId, string applicationId,
            bool hardDelete)
        {
            if (newEndpoints == null)
            {
                throw new ArgumentNullException(nameof(newEndpoints));
            }

            var context = result.Context.Validate();

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

            if (!(result.RegisterOnly ?? false))
            {
                // Remove or disable an endpoint
                foreach (var item in remove)
                {
                    try
                    {
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
                                await (_endpointEvents?.OnEndpointDeletedAsync(context,
                                    item.DeviceId, item.ToServiceModel())).ConfigureAwait(false);
                            }
                            else if (!(item.IsDisabled ?? false))
                            {
                                var endpoint = item.ToServiceModel();
                                var update = endpoint.ToEndpointRegistration(true);
                                await _iothub.PatchAsync(item.Patch(update, _serializer), true).ConfigureAwait(false);
                                await (_endpointEvents?.OnEndpointDisabledAsync(context, endpoint)).ConfigureAwait(false);
                            }
                            else
                            {
                                unchanged++;
                                continue;
                            }
                            removed++;
                        }
                        else
                        {
                            // Skip the ones owned by other publishers
                            unchanged++;
                        }
                    }
                    catch (Exception ex)
                    {
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
                        await _iothub.PatchAsync(exists.Patch(patch, _serializer), true).ConfigureAwait(false);
                        var endpoint = patch.ToServiceModel();

                        // await _broker.NotifyAllAsync(
                        //     l => l.OnEndpointUpdatedAsync(context, endpoint));
                        if (exists.IsDisabled ?? false)
                        {
                            await (_endpointEvents?.OnEndpointEnabledAsync(context, endpoint)).ConfigureAwait(false);
                        }
                        updated++;
                        continue;
                    }
                    unchanged++;
                }
                catch (Exception ex)
                {
                    unchanged++;
                    _logger.LogError(ex, "Exception during update.");
                }
            }

            // Add endpoint
            foreach (var item in add)
            {
                try
                {
                    await _iothub.CreateOrUpdateAsync(item.ToDeviceTwin(_serializer), true).ConfigureAwait(false);

                    var endpoint = item.ToServiceModel();
                    await (_endpointEvents?.OnEndpointNewAsync(context, endpoint)).ConfigureAwait(false);
                    await (_endpointEvents?.OnEndpointEnabledAsync(context, endpoint)).ConfigureAwait(false);
                    added++;
                }
                catch (Exception ex)
                {
                    unchanged++;
                    _logger.LogError(ex, "Exception adding endpoint from discovery.");
                }
            }

            if (added != 0 || removed != 0)
            {
                _logger.LogInformation("processed endpoint results: {Added} endpoints added, {Updated} " +
                    "updated, {Removed} removed or disabled, and {Unchanged} unchanged.",
                    added, updated, removed, unchanged);
            }
        }

        /// <summary>
        /// Update application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="updater"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<ApplicationInfoModel> UpdateApplicationAsync(string applicationId,
            Func<ApplicationInfoModel, bool?, (bool?, bool?)> updater, CancellationToken ct)
        {
            while (true)
            {
                try
                {
                    var registration = await GetApplicationRegistrationAsync(applicationId, true, ct).ConfigureAwait(false);
                    // Update registration from update request
                    var application = registration.ToServiceModel();
                    var (patch, disabled) = updater(application, registration.IsDisabled);
                    if (patch ?? false)
                    {
                        var update = application.ToApplicationRegistration(disabled);
                        var twin = await _iothub.PatchAsync(registration.Patch(update, _serializer), ct: ct).ConfigureAwait(false);
                        registration = twin.ToApplicationRegistration();
                    }
                    return registration.ToServiceModel();
                }
                catch (ResourceOutOfDateException ex)
                {
                    // Retry create/update
                    _logger.LogDebug(ex, "Retry updating application...");
                    continue;
                }
            }
        }

        /// <summary>
        /// Add application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="disabled"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<ApplicationInfoModel> AddApplicationAsync(
            ApplicationInfoModel application, bool? disabled, CancellationToken ct)
        {
            var registration = application.ToApplicationRegistration(disabled);
            var twin = await _iothub.CreateOrUpdateAsync(
                registration.ToDeviceTwin(_serializer), false, ct).ConfigureAwait(false);
            return twin.ToApplicationRegistration().ToServiceModel();
        }

        /// <summary>
        /// Delete application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="precondition"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<ApplicationInfoModel> DeleteApplicationAsync(string applicationId,
            Func<ApplicationInfoModel, bool> precondition, CancellationToken ct)
        {
            while (true)
            {
                try
                {
                    var registration = await GetApplicationRegistrationAsync(applicationId, false, ct).ConfigureAwait(false);
                    if (registration == null)
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
                    await _iothub.DeleteAsync(applicationId, null, registration.Etag, ct).ConfigureAwait(false);
                    // return deleted entity
                    return application;
                }
                catch (ResourceOutOfDateException ex)
                {
                    // Retry create/update
                    _logger.LogDebug(ex, "Retry deleting application...");
                    continue;
                }
            }
        }

        /// <summary>
        /// Get registration
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="throwIfNotFound"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ResourceNotFoundException"></exception>
        private async Task<ApplicationRegistration> GetApplicationRegistrationAsync(
            string applicationId, bool throwIfNotFound, CancellationToken ct)
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
            var registration = twin.ToEntityRegistration() as ApplicationRegistration;
            if (registration == null && throwIfNotFound)
            {
                throw new ResourceNotFoundException("Not an application registration");
            }
            return registration;
        }

        /// <summary>
        /// Try handle enabling of application
        /// </summary>
        /// <param name="context"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        private async Task HandleApplicationEnabledAsync(OperationContextModel context,
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
                    endpoint.NotSeenSince = null;
                    var update = endpoint.ToEndpointRegistration(false);
                    await _iothub.PatchAsync(registration.Patch(update, _serializer)).ConfigureAwait(false);
                    await (_endpointEvents?.OnEndpointEnabledAsync(context, endpoint)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed re-enabling endpoint {Id}", registration.Id);
                    continue;
                }
            }
            await (_applicationEvents?.OnApplicationEnabledAsync(context, application)).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle disabling of application
        /// </summary>
        /// <param name="context"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        public async Task HandleApplicationDisabledAsync(OperationContextModel context,
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
                        endpoint.NotSeenSince = DateTime.UtcNow;
                        var update = endpoint.ToEndpointRegistration(true);
                        await _iothub.PatchAsync(registration.Patch(update, _serializer)).ConfigureAwait(false);
                        await (_endpointEvents?.OnEndpointDisabledAsync(context, endpoint)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed disabling endpoint {Id}", registration.Id);
                    }
                }
            }
            await (_applicationEvents?.HandleApplicationDisabledAsync(context, application)).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle application deletion
        /// </summary>
        /// <param name="context"></param>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        private async Task DeleteEndpointsAsync(OperationContextModel context,
            string applicationId)
        {
            // Get all endpoint registrations and for each one, call delete, if failure,
            // stop half way and throw and do not complete.
            var endpoints = await GetEndpointsAsync(applicationId, true).ConfigureAwait(false);
            foreach (var registration in endpoints)
            {
                await _iothub.DeleteAsync(registration.DeviceId).ConfigureAwait(false);
                var endpoint = registration.ToServiceModel();
                await (_endpointEvents?.OnEndpointDeletedAsync(context,
                    endpoint.Registration.Id, endpoint)).ConfigureAwait(false);
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
                $"tags.{nameof(EntityRegistration.DeviceType)} = '{IdentityType.Endpoint}' ";

            if (!includeDeleted)
            {
                query += $"AND NOT IS_DEFINED(tags.{nameof(EntityRegistration.NotSeenSince)})";
            }

            var result = new List<DeviceTwinModel>();
            string continuation = null;
            do
            {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, null, null, ct).ConfigureAwait(false);
                result.AddRange(devices.Items);
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
            return result
                .Select(d => d.ToEndpointRegistration(false))
                .Where(r => r != null);
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
        private static EndpointInfoModel TwinModelToEndpointRegistrationModel(
            DeviceTwinModel twin, bool onlyServerState, bool skipInvalid)
        {
            // Convert to twin registration
            var registration = twin.ToEntityRegistration(onlyServerState) as EndpointRegistration;
            if (registration == null)
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

        /// <summary>
        /// Replace whitespace in a property name
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SanitizePropertyName(string value)
        {
            var chars = new char[value.Length];
            for (var i = 0; i < value.Length; i++)
            {
                chars[i] = !char.IsLetterOrDigit(value[i]) ? '_' : value[i];
            }
            return new string(chars);
        }
        private static readonly Gauge kAppsAdded = Metrics
            .CreateGauge("iiot_registry_applicationAdded", "Number of applications added ");
        private static readonly Gauge kAppsUpdated = Metrics
            .CreateGauge("iiot_registry_applicationsUpdated", "Number of applications updated ");
        private static readonly Gauge kAppsUnchanged = Metrics
            .CreateGauge("iiot_registry_applicationUnchanged", "Number of applications unchanged ");

        private readonly IEndpointRegistryListener _endpointEvents;
        private readonly IApplicationRegistryListener _applicationEvents;
        private readonly IJsonSerializer _serializer;
        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
