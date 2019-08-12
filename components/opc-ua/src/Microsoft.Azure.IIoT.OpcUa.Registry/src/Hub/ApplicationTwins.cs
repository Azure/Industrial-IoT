// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Application database using the IoT Hub device registry as repository.
    /// </summary>
    public sealed class ApplicationTwins : IApplicationRepository {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="logger"></param>
        public ApplicationTwins(IIoTHubTwinServices iothub, ILogger logger) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryAsync(
            ApplicationRegistrationQueryModel model, int? pageSize, CancellationToken ct) {

            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(ApplicationRegistration.DeviceType)} = 'Application' ";

            if (!(model?.IncludeNotSeenSince ?? false)) {
                // Scope to non deleted applications
                query += $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)}) ";
            }

            if (model?.Locale != null) {
                if (model?.ApplicationName != null) {
                    // If application name provided, include it in search
                    query += $"AND tags.{nameof(ApplicationRegistration.LocalizedNames)}" +
                        $".{model.Locale} = '{model.ApplicationName}' ";
                }
                else {
                    // Just search for locale
                    query += $"AND IS_DEFINED(tags.{nameof(ApplicationRegistration.LocalizedNames)}" +
                        $".{model.Locale}) ";
                }
            }
            else if (model?.ApplicationName != null) {
                // If application name provided, search for default name
                query += $"AND tags.{nameof(ApplicationRegistration.ApplicationName)} = " +
                    $"'{model.ApplicationName}' ";
            }
            if (model?.ProductUri != null) {
                // If product uri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.ProductUri)} = " +
                    $"'{model.ProductUri}' ";
            }
            if (model?.GatewayServerUri != null) {
                // If gateway uri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.GatewayServerUri)} = " +
                    $"'{model.GatewayServerUri}' ";
            }
            if (model?.DiscoveryProfileUri != null) {
                // If discovery profile uri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.DiscoveryProfileUri)} = " +
                    $"'{model.DiscoveryProfileUri}' ";
            }
            if (model?.ApplicationUri != null) {
                // If ApplicationUri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.ApplicationUriLC)} = " +
                    $"'{model.ApplicationUri.ToLowerInvariant()}' ";
            }
            if (model?.ApplicationType == ApplicationType.Client ||
                model?.ApplicationType == ApplicationType.ClientAndServer) {
                // If searching for clients include it in search
                query += $"AND tags.{nameof(ApplicationType.Client)} = true ";
            }
            if (model?.ApplicationType == ApplicationType.Server ||
                model?.ApplicationType == ApplicationType.ClientAndServer) {
                // If searching for servers include it in search
                query += $"AND tags.{nameof(ApplicationType.Server)} = true ";
            }
            if (model?.ApplicationType == ApplicationType.DiscoveryServer) {
                // If searching for servers include it in search
                query += $"AND tags.{nameof(ApplicationType.DiscoveryServer)} = true ";
            }
            if (model?.Capability != null) {
                // If Capabilities provided, filter results
                var tag = JTokenEx.SanitizePropertyName(model.Capability)
                    .ToUpperInvariant();
                query += $"AND tags.{tag} = true ";
            }
            if (model?.SiteOrSupervisorId != null) {
                // If ApplicationUri provided, include it in search
                query += $"AND tags.{nameof(BaseRegistration.SiteOrSupervisorId)} = " +
                    $"'{model.SiteOrSupervisorId}' ";
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize);
            return new ApplicationInfoListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => t.ToApplicationRegistration())
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var tag = nameof(BaseRegistration.SiteOrSupervisorId);
            var query = $"SELECT tags.{tag}, COUNT() FROM devices WHERE " +
                $"tags.{nameof(ApplicationRegistration.DeviceType)} = 'Application' " +
                $"GROUP BY tags.{tag}";
            var result = await _iothub.QueryAsync(query, continuation, pageSize);
            return new ApplicationSiteListModel {
                ContinuationToken = result.ContinuationToken,
                Sites = result.Result
                    .Select(o => o.GetValueOrDefault<string>(tag))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoModel> GetAsync(string applicationId,
            bool throwIfNotFound, CancellationToken ct) {
            var registration = await GetRegistrationAsync(applicationId, throwIfNotFound, ct);
            return registration?.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListAsync(
            string continuation, int? pageSize, bool? disabled, CancellationToken ct) {
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(ApplicationRegistration.DeviceType)} = 'Application' ";
            if (disabled != null) {
                if (disabled.Value) {
                    query +=
                        $"AND IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
                }
                else {
                    query +=
                        $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
                }
            }
            var result = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize, ct);
            return new ApplicationInfoListModel {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(t => t.ToApplicationRegistration())
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ApplicationInfoModel>> ListAllAsync(string siteId,
            string supervisorId, CancellationToken ct) {
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(BaseRegistration.DeviceType)} = 'Application' AND " +
                $"(tags.{nameof(ApplicationRegistration.SiteId)} = '{siteId}' OR" +
                $" tags.{nameof(BaseRegistration.SupervisorId)} = '{supervisorId}')";

            var twins = await _iothub.QueryAllDeviceTwinsAsync(query, ct);
            return twins
                .Select(t => t.ToApplicationRegistration())
                .Select(a => a.ToServiceModel());
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoModel> UpdateAsync(string applicationId,
            Func<ApplicationInfoModel, bool?, (bool?, bool?)> updater, CancellationToken ct) {
            while (true) {
                try {
                    var registration = await GetRegistrationAsync(applicationId, true, ct);
                    // Update registration from update request
                    var application = registration.ToServiceModel();
                    var (patch, disabled) = updater(application, registration.IsDisabled);
                    if (patch ?? false) {
                        var update = application.ToApplicationRegistration(disabled);
                        var twin = await _iothub.PatchAsync(registration.Patch(update));
                        registration = twin.ToApplicationRegistration();
                    }
                    return registration.ToServiceModel();
                }
                catch (ResourceOutOfDateException ex) {
                    // Retry create/update
                    _logger.Debug(ex, "Retry updating application...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoModel> AddAsync(
            ApplicationInfoModel application, bool? disabled, CancellationToken ct) {
            var registration = application.ToApplicationRegistration(disabled);
            var twin = await _iothub.CreateAsync(registration.ToDeviceTwin(), false, ct);
            var result = twin.ToApplicationRegistration().ToServiceModel();
            return result;
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoModel> DeleteAsync(string applicationId,
            Func<ApplicationInfoModel, bool> precondition, CancellationToken ct) {
            while (true) {
                try {
                    var registration = await GetRegistrationAsync(applicationId, false, ct);
                    if (registration == null) {
                        return null;
                    }
                    var application = registration.ToServiceModel();
                    if (precondition != null) {
                        var shouldDelete = precondition(application);
                        if (!shouldDelete) {
                            return null;
                        }
                    }
                    // Delete application
                    await _iothub.DeleteAsync(applicationId, null, registration.Etag, ct);
                    // return deleted entity
                    return application;
                }
                catch (ResourceOutOfDateException ex) {
                    // Retry create/update
                    _logger.Debug(ex, "Retry deleting application...");
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
        private async Task<ApplicationRegistration> GetRegistrationAsync(
            string applicationId, bool throwIfNotFound, CancellationToken ct) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }
            // Get existing application and compare to see if we need to patch.
            var twin = await _iothub.GetAsync(applicationId, null, ct);
            if (twin.Id != applicationId) {
                throw new ArgumentException("Id must be same as application to patch",
                    nameof(applicationId));
            }

            // Convert to application registration
            var registration = twin.ToRegistration() as ApplicationRegistration;
            if (registration == null && throwIfNotFound) {
                throw new ResourceNotFoundException("Not an application registration");
            }
            return registration;
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
    }
}
