// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IIoT.OpcTwin.Services.External;
    using Microsoft.Azure.IIoT.OpcTwin.Services.External.Models;
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using Microsoft.Azure.IIoT.Common.Diagnostics;
    using Microsoft.Azure.IIoT.Common.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint services using the IoT Hub twin services for endpoint
    /// identity registration/retrieval.
    /// </summary>
    public sealed class OpcUaRegistryServices : IOpcUaTwinRegistry, IOpcUaSupervisorRegistry,
        IOpcUaApplicationRegistry, IOpcUaRegistryMaintenance {

        /// <summary>
        /// Create using iot hub twin registry service client
        /// </summary>
        /// <param name="registry"></param>
        public OpcUaRegistryServices(IIoTHubTwinServices registry,
            IOpcUaValidationServices validator, ILogger logger) {

            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Read specific twin registration by twin id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public async Task<TwinInfoModel> GetTwinAsync(string id,
            bool onlyServerState) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var device = await _registry.GetAsync(id);
            return TwinModelToTwinRegistrationModel(device, onlyServerState, false);
        }

        /// <summary>
        /// List all twin registrations
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public async Task<TwinInfoListModel> ListTwinsAsync(string continuation,
            bool onlyServerState, int? pageSize) {
            // Find all devices where endpoint information is configured
            var query = $"SELECT * FROM devices WHERE " +
                $"IS_OBJECT(properties.desired.{k_endpointProperty})";
            var devices = await _registry.QueryAsync(query, continuation, pageSize);
            return new TwinInfoListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(d => TwinModelToTwinRegistrationModel(d, onlyServerState, true))
                    .Where(x => x != null)
                    .ToList()
            };
        }

        /// <summary>
        /// Find registration for the endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<TwinInfoModel> FindTwinAsync(EndpointModel endpoint,
            bool onlyServerState) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            var results = await _registry.QueryAsync("SELECT * FROM devices WHERE " +
                $"IS_OBJECT(properties.desired.{k_endpointProperty}) AND " +
                    $"tags.{nameof(OpcUaEndpointRegistration.EndpointUrlLC)} = " +
                        $"'{endpoint.Url.ToLowerInvariant()}'");
            foreach (var candidate in results) {
                var registration = OpcUaEndpointRegistration.FromTwin(candidate,
                    onlyServerState);
                if (registration.Matches(endpoint)) {
                    return registration.ToServiceModel();
                }
            }
            return null;
        }

        /// <summary>
        /// Find registration of endpoints using query specification
        /// </summary>
        /// <param name="model"></param>
        /// <param name="onlyServerState">Whether only
        /// desired twin state should be returned.
        /// </param>
        /// <returns></returns>
        public async Task<TwinInfoListModel> QueryTwinsAsync(
            TwinRegistrationQueryModel model, bool onlyServerState, int? pageSize) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaEndpointRegistration.DeviceType)} = 'Endpoint' ";

            if (model.IsTrusted != null) {
                // If application name provided, include it in search
                query += $"AND tags.{nameof(OpcUaEndpointRegistration.IsTrusted)} = " +
                    $"{model.IsTrusted} ";
            }
            if (model.Url != null) {
                // If Url provided, include it in search
                query += $"AND tags.{nameof(OpcUaEndpointRegistration.EndpointUrlLC)} = " +
                    $"'{model.Url.ToLowerInvariant()}' ";
            }
            if (model.SecurityMode != null) {
                // If SecurityMode provided, include it in search
                query += $"AND properties.desired.{k_endpointProperty}." +
                    $"{nameof(OpcUaEndpointRegistration.SecurityMode)} = '{model.SecurityMode}' ";
            }
            if (model.SecurityPolicy != null) {
                // If SecurityPolicy uri provided, include it in search
                query += $"AND properties.desired.{k_endpointProperty}." +
                    $"{nameof(OpcUaEndpointRegistration.SecurityPolicy)} = '{model.SecurityPolicy}' ";
            }
            if (model.TokenType != null) {
                // If TokenType provided, include it in search
                query += $"AND properties.desired.{k_endpointProperty}." +
                    $"{nameof(OpcUaEndpointRegistration.TokenType)} = '{model.TokenType}' ";
            }
            if (model.User != null) {
                // If User provided, include it in search
                query += $"AND properties.desired.{k_endpointProperty}." +
                    $"{nameof(OpcUaEndpointRegistration.User)} = '{model.User}' ";
            }
            var result = await _registry.QueryAsync(query, null, pageSize);
            return new TwinInfoListModel {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(t => OpcUaEndpointRegistration.FromTwin(t, onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Update twin registration
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task UpdateTwinAsync(TwinRegistrationUpdateModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.Id)) {
                throw new ArgumentException(nameof(request.Id));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var twin = await _registry.GetAsync(request.Id);
            if (twin.Id != request.Id) {
                throw new ArgumentException("Id must be same as twin to patch",
                    nameof(request.Id));
            }

            // Convert to twin registration
            var registration = OpcUaEndpointRegistration.FromTwin(twin, true);

            // Update registration from update request
            var patched = registration.ToServiceModel();
            if (request.User != null) {
                patched.Endpoint.User = string.IsNullOrEmpty(request.User) ?
                    null : request.User;
            }
            if (request.TokenType != null) {
                patched.Endpoint.TokenType = (TokenType)request.TokenType;
            }
            if ((patched.Endpoint.TokenType ?? TokenType.None) != TokenType.None) {
                patched.Endpoint.Token = request.Token;
            }
            else {
                patched.Endpoint.Token = null;
            }
            var isEnabled = (patched.Endpoint.IsTrusted ?? false);
            var enable = (request.IsTrusted ?? isEnabled);
            patched.Endpoint.IsTrusted = request.IsTrusted;

            if (request.Duplicate ?? false) {
                registration = new OpcUaEndpointRegistration();
            }

            // Patch
            await _registry.CreateOrUpdateAsync(registration.Patch(patched));

            // Enable/disable twin if needed
            if (isEnabled != enable) {
                await EnableTwinAsync(registration.SupervisorId,
                    registration.DeviceId, !enable);
            }
        }

        /// <summary>
        /// Register application in device twin registry and any endpoints
        /// associated with it.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationResultModel> RegisterAsync(
            ServerRegistrationRequestModel request) {

            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.DiscoveryUrl == null) {
                throw new ArgumentNullException(nameof(request.DiscoveryUrl));
            }

            var endpoints = Enumerable.Empty<OpcUaEndpointRegistration>();
            var application = new OpcUaApplicationRegistration();

            //
            // Read application from remote using the passed in discovery url
            //
            var discovered = await _validator.DiscoverApplicationAsync(
                new Uri(request.DiscoveryUrl));

            //
            // See if already something registered in this form
            //
            var existing = await _registry.QueryAsync("SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaTwinRegistration.ApplicationId)} = " +
                    $"'{discovered.Application.ApplicationId}'");

            if (existing.Any()) {
                // if so, get existing twins
                var twins = existing.Select(OpcUaTwinRegistration.ToRegistration);

                // Select endpoints and application to be patched below.
                endpoints = twins.OfType<OpcUaEndpointRegistration>();
                application = twins.OfType<OpcUaApplicationRegistration>().SingleOrDefault();
                if (application == null) {
                    throw new InvalidOperationException("No or more than one application " +
                        $"registered for id {discovered.Application.ApplicationId}");
                }
            }

            //
            // Create or patch existing application and update all endpoint twins
            //
            await _registry.CreateOrUpdateAsync(application.Patch(discovered.Application,
                false));
            await MergeEndpointsAsync(discovered.Endpoints.Select(e =>
                OpcUaEndpointRegistration.FromServiceModel(new TwinInfoModel {
                    ApplicationId = discovered.Application.ApplicationId,
                    Endpoint = e.Endpoint,
                    SecurityLevel = e.SecurityLevel,
                    Certificate = e.Certificate
                })), endpoints, true);
            _logger.Debug("Application registered.", () => discovered);

            return new ApplicationRegistrationResultModel {
                Id = discovered.Application.ApplicationId
            };
        }

        /// <summary>
        /// Register application record
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationResultModel> RegisterAsync(
            ApplicationRegistrationRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ApplicationUri == null) {
                throw new ArgumentNullException(nameof(request.ApplicationUri));
            }
            var registration = new OpcUaApplicationRegistration();
            await _registry.CreateOrUpdateAsync(registration.Patch(
                new ApplicationInfoModel {
                    ApplicationName = request.ApplicationName,
                    ProductUri = request.ProductUri,
                    DiscoveryUrls = request.DiscoveryUrls,
                    DiscoveryProfileUri = request.DiscoveryProfileUri,
                    ApplicationType = request.ApplicationType ?? ApplicationType.Server,
                    ApplicationUri = request.ApplicationUri,
                    Capabilities = request.Capabilities,
                    SupervisorId = null
                }));
            return new ApplicationRegistrationResultModel {
                Id = registration.ApplicationId
            };
        }

        /// <summary>
        /// Update application
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task UpdateApplicationAsync(ApplicationRegistrationUpdateModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.Id)) {
                throw new ArgumentException(nameof(request.Id));
            }

            // Get existing application and compare to see if we need to patch.
            var application = await _registry.GetAsync(request.Id);
            if (application.Id != request.Id) {
                throw new ArgumentException("Id must be same as application to patch",
                    nameof(request.Id));
            }

            // Convert to application registration
            var registration = OpcUaApplicationRegistration.FromTwin(application);

            // Update registration from update request
            var patched = registration.ToServiceModel();
            if (request.ApplicationName != null) {
                patched.ApplicationName = string.IsNullOrEmpty(request.ApplicationName) ?
                    null : request.ApplicationName;
            }
            if (request.ProductUri != null) {
                patched.ProductUri = string.IsNullOrEmpty(request.ProductUri) ?
                    null : request.ProductUri;
            }
            if (request.Certificate != null) {
                patched.Certificate = request.Certificate.Length == 0 ?
                    null : request.Certificate;
            }
            if (request.Capabilities != null) {
                patched.Capabilities = request.Capabilities.Count == 0 ?
                    null : request.Capabilities;
            }
            if (request.DiscoveryUrls != null) {
                patched.DiscoveryUrls = request.DiscoveryUrls.Count == 0 ?
                    null : request.DiscoveryUrls;
            }
            if (request.DiscoveryProfileUri != null) {
                patched.DiscoveryProfileUri = string.IsNullOrEmpty(request.DiscoveryProfileUri) ?
                    null : request.ApplicationName;
            }
            // Patch
            await _registry.CreateOrUpdateAsync(registration.Patch(patched));
        }

        /// <summary>
        /// List all servers with endpoints == twins
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize) {
            var query =  "SELECT * FROM devices WHERE IS_DEFINED" +
                $"(tags.{nameof(OpcUaApplicationRegistration.ApplicationUriLC)})";
            var result = await _registry.QueryAsync(query, continuation, pageSize);
            return new ApplicationInfoListModel {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(OpcUaApplicationRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Get full application model for specified application id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(string applicationId) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentException(nameof(applicationId));
            }
            var device = await _registry.GetAsync(applicationId);
            var endpoints = await ListEndpointsForApplicationAsync(applicationId);
            return new ApplicationRegistrationModel {
                Application = OpcUaApplicationRegistration.FromTwin(device).ToServiceModel(),
                Endpoints = endpoints.Select(t => new TwinRegistrationModel {
                    Endpoint = t.Endpoint,
                    Id = t.Id,
                    Certificate = t.Certificate,
                    SecurityLevel = t.SecurityLevel,
                    Connected = t.Connected
                }).ToList()
            }.SetSecurityAssessment();
        }

        /// <summary>
        /// Read full server model for specified server
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel model, int? pageSize) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaApplicationRegistration.DeviceType)} = 'Application' ";

            if (model.ApplicationName != null) {
                // If application name provided, include it in search
                query += $"AND tags.{nameof(OpcUaApplicationRegistration.ApplicationName)} = " +
                    $"'{model.ApplicationName}' ";
            }
            if (model.ProductUri != null) {
                // If product uri provided, include it in search
                query += $"AND tags.{nameof(OpcUaApplicationRegistration.ProductUri)} = " +
                    $"'{model.ProductUri}' ";
            }
            if (model.ApplicationUri != null) {
                // If ApplicationUri provided, include it in search
                query += $"AND tags.{nameof(OpcUaApplicationRegistration.ApplicationUriLC)} = " +
                    $"'{model.ApplicationUri.ToLowerInvariant()}' ";
            }

            if (model.ApplicationType == ApplicationType.Client) {
                // If searching for clients include it in search
                query += $"AND tags.{nameof(ApplicationType.Client)} = true ";
            }

            if (model.ApplicationType == ApplicationType.Server) {
                // If searching for servers include it in search
                query += $"AND tags.{nameof(ApplicationType.Server)} = true ";
            }

            if (model.Capabilities != null) {
                // If Capabilities provided, filter results
                foreach (var cap in model.Capabilities) {
                    query += $"AND tags.{cap.SanitizePropertyName().ToUpperInvariant()} = true ";
                }
            }

            var queryResult = await _registry.QueryAsync(query, null, pageSize);
            return new ApplicationInfoListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(OpcUaApplicationRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Delete application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public async Task UnregisterApplicationAsync(string applicationId) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentException(nameof(applicationId));
            }

            // Get all twin registrations and for each one, call delete, if failure,
            // stop half way and throw and do not complete.
            var endpoints = await ListEndpointsForApplicationAsync(applicationId);
            foreach(var twin in endpoints) {
                try {
                    if (!string.IsNullOrEmpty(twin.Endpoint.SupervisorId)) {
                        await EnableTwinAsync(twin.Endpoint.SupervisorId, twin.Id, true);
                    }
                }
                catch (Exception ex) {
                    _logger.Debug($"Failed unregistration of twin {twin.Id}", () => ex);
                }
                await _registry.DeleteAsync(twin.Id);
            }
            await _registry.DeleteAsync(applicationId);
        }

        /// <summary>
        /// Read supervisor using supervisor id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<SupervisorModel> GetSupervisorAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(id, out var moduleId);
            var device = await _registry.GetAsync(deviceId, moduleId);
            return OpcUaSupervisorRegistration.FromTwin(device).ToServiceModel();
        }

        /// <summary>
        /// Update supervisor
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task UpdateSupervisorAsync(SupervisorUpdateModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.Id)) {
                throw new ArgumentException(nameof(request.Id));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = SupervisorModelEx.ParseDeviceId(request.Id, out var moduleId);
            var twin = await _registry.GetAsync(deviceId, moduleId);
            if (twin.Id != deviceId && twin.ModuleId != moduleId) {
                throw new ArgumentException("Id must be same as twin to patch",
                    nameof(request.Id));
            }

            // Convert to supervisor registration
            var registration = OpcUaSupervisorRegistration.FromTwin(twin, true,
                out var tmp);

            // Update registration from update request
            var patched = registration.ToServiceModel();
            if (request.Discovery != null) {
                patched.Discovery = (DiscoveryMode)request.Discovery;
            }
            if (request.DiscoveryConfig != null) {
                patched.DiscoveryConfig = request.DiscoveryConfig;
            }
            // Patch
            await _registry.CreateOrUpdateAsync(registration.Patch(patched));
        }

        /// <summary>
        /// List all supervisors
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string continuation, int? pageSize) {
            var query = $"SELECT * FROM devices.modules WHERE " +
                $"properties.reported.type = 'supervisor'";
            var devices = await _registry.QueryAsync(query, continuation, pageSize);
            return new SupervisorListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(OpcUaSupervisorRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Process discovery sweep results from supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task ProcessDiscoveryAsync(string supervisorId,
            IEnumerable<DiscoveryEventModel> events, bool hardDelete) {

            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }

            // Get all applications for the supervisor
            var results = await _registry.QueryAsync("SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaTwinRegistration.DeviceType)} = 'Application' AND " +
                $"tags.{nameof(OpcUaTwinRegistration.SupervisorId)} = '{supervisorId}'");

            var existingApplications = results.Select(
                t => OpcUaApplicationRegistration.FromTwin(t));
            var discoveredApplications = events.Select(
                ev => OpcUaApplicationRegistration.FromServiceModel(ev.Application));
            await MergeApplicationsAsync(discoveredApplications, existingApplications,
                hardDelete);

            // Update endpoints of all existing applications
            foreach (var ev in events.GroupBy(k => k.Application.ApplicationId)) {

                var endpoints = await _registry.QueryAsync("SELECT * FROM devices WHERE " +
                    $"tags.{nameof(OpcUaTwinRegistration.DeviceType)} = 'Endpoint' AND " +
                    $"tags.{nameof(OpcUaTwinRegistration.SupervisorId)} = " +
                        $"'{supervisorId}' AND " +
                    $"tags.{nameof(OpcUaTwinRegistration.ApplicationId)} = " +
                        $"'{ev.Key}'");

                var existingEndpoints = endpoints
                    .Select(t => OpcUaEndpointRegistration.FromTwin(t, false));
                var discoveredEndpoints = ev
                    .Select(e => e.Endpoint)
                    .Where(e => e.Endpoint != null)
                    .Select(e => new TwinInfoModel {
                        Endpoint = e.Endpoint,
                        Certificate = e.Certificate,
                        SecurityLevel = e.SecurityLevel,
                        ApplicationId = ev.Key
                    })
                    .Select(OpcUaEndpointRegistration.FromServiceModel);

                await MergeEndpointsAsync(discoveredEndpoints, existingEndpoints,
                    hardDelete);
            }
        }

        /// <summary>
        /// Merge existing and newly found applications
        /// </summary>
        /// <param name="found"></param>
        /// <param name="existing"></param>
        /// <returns></returns>
        private async Task MergeApplicationsAsync(
            IEnumerable<OpcUaApplicationRegistration> found,
            IEnumerable<OpcUaApplicationRegistration> existing, bool hardDelete) {

            var remove = new HashSet<OpcUaApplicationRegistration>(existing,
                OpcUaApplicationRegistration.Logical);
            var add = new HashSet<OpcUaApplicationRegistration>(found,
                OpcUaApplicationRegistration.Logical);
            var unchange = new HashSet<OpcUaApplicationRegistration>(existing,
                OpcUaApplicationRegistration.Logical);

            unchange.IntersectWith(add);
            remove.ExceptWith(found);
            add.ExceptWith(existing);

            var added = 0;
            var unchanged = 0;
            var removed = 0;

            // Remove applications
            foreach (var item in remove) {
                try {
                    if (hardDelete) {
                        await UnregisterApplicationAsync(item.ApplicationId);
                    }
                    else if (!(item.IsDisabled ?? false)) {
                        // Disable
                        await DisableApplicationAsync(item);
                    }
                    else {
                        unchanged++;
                        continue;
                    }
                    removed++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error("Exception during application removal.", () => ex);
                }
            }

            // Update applications that were disabled
            foreach (var item in unchange) {
                if (item.IsDisabled ?? false) {
                    try {
                        await _registry.CreateOrUpdateAsync(
                            item.Patch(item.ToServiceModel(), false));
                        added++;
                    }
                    catch (Exception ex) {
                        unchanged++;
                        _logger.Error("Exception during enabling.", () => ex);
                    }
                }
                else {
                    unchanged++;
                }
            }

            // Add new applications
            foreach (var item in add) {
                try {
                    var twin = new OpcUaApplicationRegistration().Patch(item.ToServiceModel(),
                        false);
                    await _registry.CreateOrUpdateAsync(twin);
                    added++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error("Exception during discovery addition.", () => ex);
                }
            }

            if (added != 0 || removed != 0) {
                _logger.Info($"... processed discovery results: {added} applications added or " +
                    $"updated, {removed} removed or disabled, and {unchanged} unchanged.", 
                    () => { });
            }
        }

        /// <summary>
        /// Merge existing and newly found endpoints
        /// </summary>
        /// <param name="found"></param>
        /// <param name="existing"></param>
        /// <returns></returns>
        private async Task MergeEndpointsAsync(
            IEnumerable<OpcUaEndpointRegistration> found,
            IEnumerable<OpcUaEndpointRegistration> existing, bool hardDelete) {

            var remove = new HashSet<OpcUaEndpointRegistration>(existing,
                OpcUaEndpointRegistration.Logical);
            var add = new HashSet<OpcUaEndpointRegistration>(found,
                OpcUaEndpointRegistration.Logical);
            var unchange = new HashSet<OpcUaEndpointRegistration>(existing,
                OpcUaEndpointRegistration.Logical);

            unchange.IntersectWith(add);
            remove.ExceptWith(found);
            add.ExceptWith(existing);

            var added = 0;
            var unchanged = 0;
            var removed = 0;

            // Remove or disable an endpoint
            foreach (var item in remove) {
                try {
                    if (hardDelete) {
                        var device = await _registry.GetAsync(item.DeviceId);
                        // First we update any supervisor registration
                        var existingEndpoint = OpcUaEndpointRegistration.FromTwin(device, false);
                        if (!string.IsNullOrEmpty(existingEndpoint.SupervisorId)) {
                            await EnableTwinAsync(existingEndpoint.SupervisorId, device.Id, true);
                        }
                        // Then hard delete...
                        await _registry.DeleteAsync(item.DeviceId);
                    }
                    else if (!(item.IsDisabled ?? false)) {
                        await _registry.CreateOrUpdateAsync(
                            item.Patch(item.ToServiceModel(), true));
                    }
                    else {
                        unchanged++;
                        continue;
                    }
                    removed++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error($"Exception during discovery removal.", () => ex);
                }
            }

            // Update endpoints that were disabled
            foreach (var item in unchange) {
                if (item.IsDisabled ?? false) {
                    try {
                        await _registry.CreateOrUpdateAsync(
                            item.Patch(item.ToServiceModel(), false));
                        added++;
                    }
                    catch (Exception ex) {
                        unchanged++;
                        _logger.Error("Exception during enabling.", () => ex);
                    }
                }
                else {
                    unchanged++;
                }
            }

            // Add endpoint
            foreach (var item in add) {
                try {
                    var twin = new OpcUaEndpointRegistration().Patch(item.ToServiceModel(),
                        false);
                    await _registry.CreateOrUpdateAsync(twin);
                    added++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error("Exception during discovery addition.", () => ex);
                }
            }

            if (added != 0 || removed != 0) {
                _logger.Info($"processed endpoint results: {added} endpoints added or " +
                    $"updated, {removed} removed or disabled, and {unchanged} unchanged.", 
                    () => { });
            }
        }

        /// <summary>
        /// Disable application and all related endpoints
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        private async Task DisableApplicationAsync(OpcUaApplicationRegistration application) {
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaEndpointRegistration.ApplicationId)} = '" +
                    application.ApplicationId +
                $"' AND IS_OBJECT(properties.desired.{k_endpointProperty})";
            string continuation = null;
            do {
                var devices = await _registry.QueryAsync(query, null);
                foreach (var twin in devices.Items) {
                    var endpoint = OpcUaEndpointRegistration.FromTwin(twin, true);
                    try {
                        if (!string.IsNullOrEmpty(endpoint.SupervisorId)) {
                            await EnableTwinAsync(endpoint.SupervisorId, twin.Id, true);
                        }
                        await _registry.CreateOrUpdateAsync(endpoint.Patch(
                            endpoint.ToServiceModel(), true));
                    }
                    catch (Exception ex) {
                        _logger.Debug($"Failed disabling of twin {twin.Id}", () => ex);
                    }
                }
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
            await _registry.CreateOrUpdateAsync(application.Patch(application.ToServiceModel(), 
                true));
        }

        /// <summary>
        /// List all endpoints for application id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        private async Task<IEnumerable<TwinInfoModel>> ListEndpointsForApplicationAsync(
            string applicationId) {
            // Find all devices where endpoint information is configured
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaEndpointRegistration.ApplicationId)} = '{applicationId}' " +
                $"AND IS_OBJECT(properties.desired.{k_endpointProperty})";

            var result = new List<DeviceTwinModel>();
            string continuation = null;
            do {
                var devices = await _registry.QueryAsync(query, null);
                result.AddRange(devices.Items);
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
            return result
                .Select(d => TwinModelToTwinRegistrationModel(d, false, true))
                .Where(x => x != null);
        }

        /// <summary>
        /// Get full server model for specified server
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public async Task<ApplicationInfoModel> GetApplicationInfoAsync(string applicationId) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentException(nameof(applicationId));
            }
            var device = await _registry.GetAsync(applicationId);
            return OpcUaApplicationRegistration.FromTwin(device).ToServiceModel();
        }

        /// <summary>
        /// Enable or disable twin on supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="twinId"></param>
        /// <param name="disable"></param>
        /// <returns></returns>
        private async Task EnableTwinAsync(string supervisorId, string twinId,
            bool disable) {

            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (string.IsNullOrEmpty(supervisorId)) {
                return; // ok, no supervisor
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out var moduleId);
            // Remove from supervisor - this disconnects the device
            if (disable) {
                await _registry.UpdatePropertyAsync(deviceId, moduleId, twinId,
                    null);
            }
            // Enable
            else {
                var device = await _registry.GetRegistrationAsync(twinId);
                // Update supervisor to start supervising this endpoint
                await _registry.UpdatePropertyAsync(deviceId, moduleId, device.Id,
                    device.Authentication.PrimaryKey);
            }
        }

        /// <summary>
        /// Convert device twin registration property to registration model
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState">Only desired should be returned
        /// this means that you will look at stale information.</param>
        /// <returns></returns>
        private static TwinInfoModel TwinModelToTwinRegistrationModel(
            DeviceTwinModel twin, bool onlyServerState, bool skipInvalid) {
            var registration = OpcUaEndpointRegistration.FromTwin(twin, onlyServerState);
            if (registration == null) {
                if (skipInvalid) {
                    return null;
                }
                throw new ResourceNotFoundException(
                    $"{twin.Id} is not a registered opc ua twin");
            }
            return registration.ToServiceModel();
        }

        private const string k_endpointProperty = "endpoint";
        private readonly IIoTHubTwinServices _registry;
        private readonly IOpcUaValidationServices _validator;
        private readonly ILogger _logger;
    }
}
