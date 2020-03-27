// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Prometheus;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Application registry service.
    /// </summary>
    public sealed class ApplicationRegistry : IApplicationRegistry, IApplicationBulkProcessor {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="database"></param>
        /// <param name="endpoints"></param>
        /// <param name="bulk"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        public ApplicationRegistry(IApplicationRepository database,
            IApplicationEndpointRegistry endpoints, IEndpointBulkProcessor bulk,
            IRegistryEventBroker<IApplicationRegistryListener> broker,
            ILogger logger, IMetricsLogger metrics) {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _database = database ?? throw new ArgumentNullException(nameof(database));

            _bulk = bulk ?? throw new ArgumentNullException(nameof(bulk));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResultModel> RegisterApplicationAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ApplicationUri == null) {
                throw new ArgumentNullException(nameof(request.ApplicationUri));
            }

            var context = request.Context.Validate();

            var application = await _database.AddAsync(request.ToApplicationInfo(context),
                null, ct);

            await _broker.NotifyAllAsync(
                l => l.OnApplicationNewAsync(context, application));
            await _broker.NotifyAllAsync(
                l => l.OnApplicationEnabledAsync(context, application));

            return new ApplicationRegistrationResultModel {
                Id = application.ApplicationId
            };
        }

        /// <inheritdoc/>
        public async Task DisableApplicationAsync(string applicationId,
            RegistryOperationContextModel context, CancellationToken ct) {
            context = context.Validate();

            var app = await _database.UpdateAsync(applicationId, (application, disabled) => {
                // Disable application
                if (!(disabled ?? false)) {
                    application.NotSeenSince = DateTime.UtcNow;
                    application.Updated = context;
                    return (true, true);
                }
                return (null, null);
            }, ct);

            await _broker.NotifyAllAsync(l => l.OnApplicationDisabledAsync(context, app));
        }

        /// <inheritdoc/>
        public async Task EnableApplicationAsync(string applicationId,
            RegistryOperationContextModel context, CancellationToken ct) {
            context = context.Validate();

            var app = await _database.UpdateAsync(applicationId, (application, disabled) => {
                // Enable application
                if (disabled ?? false) {
                    application.NotSeenSince = null;
                    application.Updated = context;
                    return (true, false);
                }
                return (null, null);
            }, ct);

            await _broker.NotifyAllAsync(l => l.OnApplicationEnabledAsync(context, app));
        }

        /// <inheritdoc/>
        public async Task UnregisterApplicationAsync(string applicationId,
            RegistryOperationContextModel context, CancellationToken ct) {
            context = context.Validate();

            var app = await _database.DeleteAsync(applicationId, null, ct);
            if (app == null) {
                return;
            }

            await _broker.NotifyAllAsync(l => l.OnApplicationDeletedAsync(context, applicationId, app));
        }

        /// <inheritdoc/>
        public async Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var context = request.Context.Validate();

            var application = await _database.UpdateAsync(applicationId, (existing, _) => {
                existing.Patch(request);
                existing.Updated = context;
                return (true, null);
            }, ct);

            // Send update to through broker
            await _broker.NotifyAllAsync(l => l.OnApplicationUpdatedAsync(context, application));
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveTwins, CancellationToken ct) {
            var application = await _database.GetAsync(applicationId, true, ct);
            if (application == null) {
                return null;
            }
            var endpoints = await _endpoints.GetApplicationEndpoints(applicationId,
                application.NotSeenSince != null, filterInactiveTwins);
            return new ApplicationRegistrationModel {
                Application = application,
                Endpoints = endpoints
                    .Select(ep => ep.Registration)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            return _database.ListAsync(continuation, pageSize, false, ct);
        }

        /// <inheritdoc/>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenSince,
            RegistryOperationContextModel context, CancellationToken ct) {
            context = context.Validate();
            var absolute = DateTime.UtcNow - notSeenSince;
            string continuation = null;
            do {
                var applications = await _database.ListAsync(continuation, null, true, ct);
                continuation = applications?.ContinuationToken;
                if (applications?.Items == null) {
                    continue;
                }
                foreach (var application in applications.Items) {
                    if (application.NotSeenSince == null ||
                        application.NotSeenSince.Value >= absolute) {
                        // Skip
                        continue;
                    }
                    try {
                        // Delete if disabled state is reflected in the query result
                        var app = await _database.DeleteAsync(application.ApplicationId,
                            a => application.NotSeenSince != null &&
                                 application.NotSeenSince.Value < absolute, ct);
                        if (app == null) {
                            // Skip - already deleted or not satisfying condition
                            continue;
                        }
                        await _broker.NotifyAllAsync(
                            l => l.OnApplicationDeletedAsync(context, app.ApplicationId, app));
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Exception purging application {id} - continue",
                            application.ApplicationId);
                        continue;
                    }
                }
            }
            while (continuation != null);
        }

        /// <inheritdoc/>
        public Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel model, int? pageSize, CancellationToken ct) {
            return _database.QueryAsync(model, pageSize, ct);
        }

        /// <inheritdoc/>
        public Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            return _database.ListSitesAsync(continuation, pageSize, ct);
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryEventsAsync(string siteId, string discovererId,
            string supervisorId, DiscoveryResultModel result, IEnumerable<DiscoveryEventModel> events) {
            if (string.IsNullOrEmpty(siteId)) {
                throw new ArgumentNullException(nameof(siteId));
            }
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (result == null) {
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
            var existing = await _database.ListAllAsync(siteId, discovererId);

            var found = events.Select(ev => {
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
                    .Select(ev => {
                        //
                        // Ensure the site id and discoverer id in the found endpoints
                        // also set to a consistent value, same as applications earlier.
                        //
                        ev.Registration.SiteId = siteId;
                        ev.Registration.DiscovererId = discovererId;
                        return new EndpointInfoModel {
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

            if (!(result.RegisterOnly ?? false)) {
                // Remove applications
                foreach (var removal in remove) {
                    try {
                        // Only touch applications the discoverer owns.
                        if (removal.DiscovererId == discovererId) {
                            // Disable if not already disabled
                            var app = await _database.UpdateAsync(removal.ApplicationId,
                                (application, disabled) => {
                                    // Disable application
                                    if (!(disabled ?? false)) {
                                        application.NotSeenSince = DateTime.UtcNow;
                                        application.Updated = context;
                                        removed++;
                                        return (true, true);
                                    }
                                    unchanged++;
                                    return (null, null);
                                });

                            await _broker.NotifyAllAsync(
                                l => l.OnApplicationDisabledAsync(context, app));
                        }
                        else {
                            // Skip the ones owned by other discoverers
                            unchanged++;
                        }
                    }
                    catch (Exception ex) {
                        unchanged++;
                        _logger.Error(ex, "Exception during application disabling.");
                    }
                }
            }

            // ... add brand new applications
            foreach (var addition in add) {
                try {
                    var application = addition.Clone();
                    application.ApplicationId =
                        ApplicationInfoModelEx.CreateApplicationId(application);
                    application.Created = context;
                    application.NotSeenSince = null;
                    application.DiscovererId = discovererId;
                    application.SiteId = siteId;

                    var app = await _database.AddAsync(application, false);

                    // Notify addition!
                    await _broker.NotifyAllAsync(l => l.OnApplicationNewAsync(context, app));
                    await _broker.NotifyAllAsync(l => l.OnApplicationEnabledAsync(context, app));

                    // Now - add all new endpoints
                    endpoints.TryGetValue(app.ApplicationId, out var epFound);
                    await _bulk.ProcessDiscoveryEventsAsync(epFound, result,
                        discovererId, supervisorId, null, false);
                    added++;
                }
                catch (ConflictingResourceException) {
                    unchange.Add(addition); // Update the existing one
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error(ex, "Exception adding application from discovery.");
                }
            }

            // Update applications and endpoints ...
            foreach (var update in unchange) {
                try {
                    var wasDisabled = false;
                    var wasUpdated = false;

                    // Disable if not already disabled
                    var app = await _database.UpdateAsync(update.ApplicationId,
                        (application, disabled) => {
                            //
                            // Check whether another discoverer owns this application (discoverer
                            // id are not the same) and it is not disabled before updating it it.
                            //
                            if (update.DiscovererId != discovererId && !(disabled ?? false)) {
                                // TODO: Decide whether we merge newly found endpoints...
                                unchanged++;
                                return (null, null);
                            }

                            wasDisabled = (disabled ?? false) && (application.NotSeenSince != null);
                            wasUpdated = true;

                            application.Patch(update);
                            application.DiscovererId = discovererId;
                            application.SiteId = siteId;
                            application.NotSeenSince = null;
                            application.Updated = context;
                            updated++;
                            return (true, false);
                        });

                    if (wasDisabled) {
                        await _broker.NotifyAllAsync(l => l.OnApplicationEnabledAsync(context, app));
                    }

                    if (wasUpdated) {
                        // If this is our discoverer's application we update all endpoints also.
                        endpoints.TryGetValue(app.ApplicationId, out var epFound);

                        // TODO: Handle case where we take ownership of all endpoints
                        await _bulk.ProcessDiscoveryEventsAsync(epFound, result, discovererId,
                            supervisorId, app.ApplicationId, false);

                        await _broker.NotifyAllAsync(l => l.OnApplicationUpdatedAsync(context, app));
                    }
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error(ex, "Exception during update.");
                }
            }

            var log = added != 0 || removed != 0 || updated != 0;
#if DEBUG
            log = true;
#endif
            if (log) {
                _logger.Information("... processed discovery results from {discovererId}: " +
                    "{added} applications added, {updated} updated, {removed} disabled, and " +
                    "{unchanged} unchanged.", discovererId, added, updated, removed, unchanged);
                _metrics.TrackValue("applicationsAdded", added);
                _metrics.TrackValue("applicationsUpdated", updated);
                _metrics.TrackValue("applicationsUnchanged", unchanged);
                kAppsAdded.Set(added);
                kAppsUpdated.Set(updated);
                kAppsUnchanged.Set(unchanged);
            }
        }

        private readonly IApplicationRepository _database;
        private readonly ILogger _logger;
        private readonly IMetricsLogger _metrics;
        private readonly IEndpointBulkProcessor _bulk;
        private readonly IApplicationEndpointRegistry _endpoints;
        private readonly IRegistryEventBroker<IApplicationRegistryListener> _broker;
        private static readonly Gauge kAppsAdded = Metrics
            .CreateGauge("iiot_registry_applicationAdded", "Number of applications added ");
        private static readonly Gauge kAppsUpdated = Metrics
            .CreateGauge("iiot_registry_applicationsUpdated", "Number of applications updated ");
        private static readonly Gauge kAppsUnchanged = Metrics
            .CreateGauge("iiot_registry_applicationUnchanged", "Number of applications unchanged ");
    }
}
