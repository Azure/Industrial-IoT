// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Newtonsoft.Json.Linq;
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
        /// <param name="iothub"></param>
        public OpcUaRegistryServices(IIoTHubTwinServices iothub, IHttpClient client,
            IOpcUaValidationServices validator, ILogger logger) {

            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _client = client ?? throw new ArgumentNullException(nameof(client));
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
            var device = await _iothub.GetAsync(id);
            return TwinModelToTwinRegistrationModel(device, onlyServerState, false);
        }

        /// <summary>
        /// List all twin registrations
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<TwinInfoListModel> ListTwinsAsync(string continuation,
            bool onlyServerState, int? pageSize) {
            // Find all devices where endpoint information is configured
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaTwinRegistration.DeviceType)} = 'Endpoint'";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize);

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
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public async Task<TwinInfoModel> FindTwinAsync(EndpointModel endpoint,
            bool onlyServerState) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            var results = await _iothub.QueryDeviceTwinsAsync("SELECT * FROM devices WHERE " +
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
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<TwinInfoListModel> QueryTwinsAsync(
            TwinRegistrationQueryModel model, bool onlyServerState, int? pageSize) {

            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaEndpointRegistration.DeviceType)} = 'Endpoint' ";

            if (model?.Url != null) {
                // If Url provided, include it in search
                query += $"AND tags.{nameof(OpcUaEndpointRegistration.EndpointUrlLC)} = " +
                    $"'{model.Url.ToLowerInvariant()}' ";
            }
            if (model?.Certificate != null) {
                // If cert provided, include it in search
                query += $"AND tags.{nameof(OpcUaTwinRegistration.Thumbprint)} = " +
                    $"{model.Certificate.ToSha1Hash()} ";
            }
            if (model?.Activated != null) {
                // If flag provided, include it in search
                query += $"AND tags.{nameof(OpcUaEndpointRegistration.Activated)} " +
                    $"{(model.Activated.Value ? "=" : "!=")} true ";
            }
            if (model?.SecurityMode != null) {
                // If SecurityMode provided, include it in search
                query += $"AND properties.desired.{nameof(OpcUaEndpointRegistration.SecurityMode)} = " +
                    $"'{model.SecurityMode}' ";
            }
            if (model?.SecurityPolicy != null) {
                // If SecurityPolicy uri provided, include it in search
                query += $"AND properties.desired.{nameof(OpcUaEndpointRegistration.SecurityPolicy)} = " +
                    $"'{model.SecurityPolicy}' ";
            }
            if (model?.TokenType != null) {
                // If TokenType provided, include it in search
                query += $"AND properties.desired.{nameof(OpcUaEndpointRegistration.TokenType)} = " +
                    $"'{model.TokenType}' ";
            }
            if (model?.User != null) {
                // If User provided, include it in search
                query += $"AND properties.desired.{nameof(OpcUaEndpointRegistration.User)} = " +
                    $"'{model.User}' ";
            }
            if (model?.Connected != null) {
                // If flag provided, include it in search
                query += $"AND properties.reported.{OpcUaTwinRegistration.kConnectedProp} " +
                    $"{(model.Connected.Value ? "=" : "!=")} true ";
            }
            var result = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize);
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
            var twin = await _iothub.GetAsync(request.Id);
            if (twin.Id != request.Id) {
                throw new ArgumentException("Id must be same as twin to patch",
                    nameof(request.Id));
            }

            // Convert to twin registration
            var registration = OpcUaEndpointRegistration.FromTwin(twin, true);

            // Update registration from update request
            var patched = registration.ToServiceModel();
            if (request.User != null) {
                patched.Registration.Endpoint.User = string.IsNullOrEmpty(request.User) ?
                    null : request.User;
                // Change user?  Always duplicate since id changes.
                request.Duplicate = true;
            }
            if (request.TokenType != null) {
                patched.Registration.Endpoint.TokenType = (TokenType)request.TokenType;
            }
            if ((patched.Registration.Endpoint.TokenType ?? TokenType.None) !=
                TokenType.None) {
                patched.Registration.Endpoint.Token = request.Token;
            }
            else {
                patched.Registration.Endpoint.Token = null;
            }

            // Check whether to enable or disable...
            var isEnabled = (patched.Activated ?? false);
            var enable = (request.Activate ?? isEnabled);
            patched.Activated = request.Activate;

            // Patch
            await _iothub.CreateOrUpdateAsync(OpcUaEndpointRegistration.Patch(
                (request.Duplicate ?? false) ?
                    null : registration,
                OpcUaEndpointRegistration.FromServiceModel(patched,
                    registration.IsDisabled)));
                        // To have duplicate item disabled, too if needed

            // Enable/disable twin if needed
            if (isEnabled != enable) {
                await EnableTwinAsync(registration.SupervisorId,
                    registration.DeviceId, enable);
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
            var existing = await _iothub.QueryDeviceTwinsAsync("SELECT * FROM devices WHERE " +
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
            await _iothub.CreateOrUpdateAsync(OpcUaApplicationRegistration.Patch(
                application,
                OpcUaApplicationRegistration.FromServiceModel(discovered.Application,
                    false)));
            await MergeEndpointsAsync(discovered.Application.SupervisorId,
                discovered.Endpoints.Select(e =>
                    OpcUaEndpointRegistration.FromServiceModel(new TwinInfoModel {
                        ApplicationId = discovered.Application.ApplicationId,
                        Registration = new TwinRegistrationModel {
                            Endpoint = e.Endpoint,
                            SiteId = application.SiteId,
                            SecurityLevel = e.SecurityLevel,
                            Certificate = e.Certificate
                    }}, false)), endpoints, true);
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
            var registration = OpcUaApplicationRegistration.FromServiceModel(
                new ApplicationInfoModel {
                    ApplicationName = request.ApplicationName,
                    ProductUri = request.ProductUri,
                    DiscoveryUrls = request.DiscoveryUrls,
                    DiscoveryProfileUri = request.DiscoveryProfileUri,
                    ApplicationType = request.ApplicationType ?? ApplicationType.Server,
                    ApplicationUri = request.ApplicationUri,
                    Capabilities = request.Capabilities,
                    SiteId = null
                });
            await _iothub.CreateOrUpdateAsync(OpcUaApplicationRegistration.Patch(
                null, registration));
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
            var application = await _iothub.GetAsync(request.Id);
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
            await _iothub.CreateOrUpdateAsync(OpcUaApplicationRegistration.Patch(
                registration, OpcUaApplicationRegistration.FromServiceModel(patched)));
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
            var device = await _iothub.GetAsync(applicationId);
            var endpoints = await GetEndpointsAsync(applicationId);
            return new ApplicationRegistrationModel {
                Application = OpcUaApplicationRegistration.FromTwin(device).ToServiceModel(),
                Endpoints = endpoints
                    .Select(e => e.ToServiceModel())
                    .Select(t => t.Registration)
                    .ToList()
            }.SetSecurityAssessment();
        }

        /// <summary>
        /// Query applications using specific criterial
        /// </summary>
        /// <param name="model"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel model, int? pageSize) {

            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaApplicationRegistration.DeviceType)} = 'Application' ";

            if (model?.ApplicationName != null) {
                // If application name provided, include it in search
                query += $"AND tags.{nameof(OpcUaApplicationRegistration.ApplicationName)} = " +
                    $"'{model.ApplicationName}' ";
            }

            if (model?.ProductUri != null) {
                // If product uri provided, include it in search
                query += $"AND tags.{nameof(OpcUaApplicationRegistration.ProductUri)} = " +
                    $"'{model.ProductUri}' ";
            }

            if (model?.ApplicationUri != null) {
                // If ApplicationUri provided, include it in search
                query += $"AND tags.{nameof(OpcUaApplicationRegistration.ApplicationUriLC)} = " +
                    $"'{model.ApplicationUri.ToLowerInvariant()}' ";
            }

            if (model?.ApplicationType == ApplicationType.Client) {
                // If searching for clients include it in search
                query += $"AND tags.{nameof(ApplicationType.Client)} = true ";
            }

            if (model?.ApplicationType == ApplicationType.Server) {
                // If searching for servers include it in search
                query += $"AND tags.{nameof(ApplicationType.Server)} = true ";
            }

            if (model?.Capabilities != null) {
                // If Capabilities provided, filter results
                foreach (var cap in model.Capabilities) {
                    query += $"AND tags.{cap.SanitizePropertyName().ToUpperInvariant()} = true ";
                }
            }

            if (model?.SiteOrSupervisorId != null) {
                // If ApplicationUri provided, include it in search
                query += $"AND tags.{nameof(OpcUaTwinRegistration.SiteOrSupervisorId)} = " +
                    $"'{model.SiteOrSupervisorId}' ";
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize);
            return new ApplicationInfoListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(OpcUaApplicationRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// List all applications
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize) {
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaApplicationRegistration.DeviceType)} = 'Application' ";
            var result = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize);
            return new ApplicationInfoListModel {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(OpcUaApplicationRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Get list of registered application sites to group applications visually
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize) {
            var tag = nameof(OpcUaTwinRegistration.SiteOrSupervisorId);
            var query = $"SELECT tags.{tag}, COUNT() FROM devices WHERE " +
                $"tags.{nameof(OpcUaApplicationRegistration.DeviceType)} = 'Application' " +
                $"GROUP BY tags.{tag}";
            var result = await _iothub.QueryAsync(query, continuation, pageSize);
            return new ApplicationSiteListModel {
                ContinuationToken = result.ContinuationToken,
                Sites = result.Result
                    .Select(o => o.Get<string>(tag, null))
                    .Where(s => !string.IsNullOrEmpty(s))
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
            var result = await GetEndpointsAsync(applicationId);
            foreach(var twin in result) {
                try {
                    if (!string.IsNullOrEmpty(twin.SupervisorId)) {
                        await EnableTwinAsync(twin.SupervisorId,
                            twin.DeviceId, false);
                    }
                }
                catch (Exception ex) {
                    _logger.Debug($"Failed unregistration of twin {twin.DeviceId}",
                        () => ex);
                }
                await _iothub.DeleteAsync(twin.DeviceId);
            }
            await _iothub.DeleteAsync(applicationId);
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
            var device = await _iothub.GetAsync(deviceId, moduleId);
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
            var twin = await _iothub.GetAsync(deviceId, moduleId);
            if (twin.Id != deviceId && twin.ModuleId != moduleId) {
                throw new ArgumentException("Id must be same as twin to patch",
                    nameof(request.Id));
            }

            // Convert to supervisor registration
            var registration = OpcUaSupervisorRegistration.FromTwin(twin, true,
                out var tmp);

            // Update registration from update request
            var notifyModeChange = false;
            var patched = registration.ToServiceModel();
            if (request.Discovery != null) {
                notifyModeChange = (patched.Discovery != request.Discovery);
                patched.Discovery = (DiscoveryMode)request.Discovery;
            }

            if (request.SiteId != null) {
                patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                    null : request.SiteId;
            }

            if (request.DiscoveryConfig != null) {
                if (patched.DiscoveryConfig == null) {
                    patched.DiscoveryConfig = new DiscoveryConfigModel();
                }
                if (request.DiscoveryConfig.AddressRangesToScan != null) {
                    patched.DiscoveryConfig.AddressRangesToScan =
                        string.IsNullOrEmpty(
                            request.DiscoveryConfig.AddressRangesToScan.Trim()) ?
                                null : request.DiscoveryConfig.AddressRangesToScan;
                }
                if (request.DiscoveryConfig.PortRangesToScan != null) {
                    patched.DiscoveryConfig.PortRangesToScan =
                        string.IsNullOrEmpty(
                            request.DiscoveryConfig.PortRangesToScan.Trim()) ?
                                null : request.DiscoveryConfig.PortRangesToScan;
                }
                if (request.DiscoveryConfig.IdleTimeBetweenScans != null) {
                    patched.DiscoveryConfig.IdleTimeBetweenScans =
                        request.DiscoveryConfig.IdleTimeBetweenScans;
                }
                if (request.DiscoveryConfig.MaxNetworkProbes != null) {
                    patched.DiscoveryConfig.MaxNetworkProbes =
                        request.DiscoveryConfig.MaxNetworkProbes <= 0 ?
                            null : request.DiscoveryConfig.MaxNetworkProbes;
                }
                if (request.DiscoveryConfig.NetworkProbeTimeout != null) {
                    patched.DiscoveryConfig.NetworkProbeTimeout =
                        request.DiscoveryConfig.NetworkProbeTimeout.Value.Ticks == 0 ?
                            null : request.DiscoveryConfig.NetworkProbeTimeout;
                }
                if (request.DiscoveryConfig.MaxPortProbes != null) {
                    patched.DiscoveryConfig.MaxPortProbes =
                        request.DiscoveryConfig.MaxPortProbes <= 0 ?
                            null : request.DiscoveryConfig.MaxPortProbes;
                }
                if (request.DiscoveryConfig.MinPortProbesPercent != null) {
                    patched.DiscoveryConfig.MinPortProbesPercent =
                        request.DiscoveryConfig.MinPortProbesPercent <= 0 ||
                        request.DiscoveryConfig.MinPortProbesPercent > 100 ?
                            null : request.DiscoveryConfig.MinPortProbesPercent;
                }
                if (request.DiscoveryConfig.PortProbeTimeout != null) {
                    patched.DiscoveryConfig.PortProbeTimeout =
                        request.DiscoveryConfig.PortProbeTimeout.Value.Ticks == 0 ?
                            null : request.DiscoveryConfig.PortProbeTimeout;
                }
            }
            // Patch
            await _iothub.CreateOrUpdateAsync(OpcUaSupervisorRegistration.Patch(
                registration, OpcUaSupervisorRegistration.FromServiceModel(patched)));
            if (notifyModeChange) {
                await NotifyDiscoveryCallbacks(request.Id, patched.Discovery);
            }
        }

        /// <summary>
        /// List all supervisors
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string continuation, int? pageSize) {
            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{OpcUaTwinRegistration.kTypeProp} = 'supervisor'";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize);
            return new SupervisorListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(OpcUaSupervisorRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Query supervisors based on query criteria
        /// </summary>
        /// <param name="model"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel model, int? pageSize) {

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{OpcUaTwinRegistration.kTypeProp} = 'supervisor'";

            if (model?.Discovery != null) {
                // If discovery mode provided, include it in search
                query += $"AND properties.desired.{nameof(OpcUaSupervisorRegistration.Discovery)} = " +
                    $"'{model.Discovery}' ";
            }

            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{OpcUaTwinRegistration.kSiteIdProp} = " +
                    $"'{model.SiteId}' OR properties.desired.{OpcUaTwinRegistration.kSiteIdProp} = " +
                    $"'{model.SiteId}')";
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize);
            return new SupervisorListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(OpcUaSupervisorRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Unregister disabled applications older than specified time.
        /// </summary>
        /// <param name="notSeenSince"></param>
        /// <returns></returns>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenSince) {
            var absolute = DateTime.UtcNow - notSeenSince;
            var query = "SELECT * FROM devices WHERE " +
            //    $"tags.{nameof(OpcUaTwinRegistration.NotSeenSince)} = '{absolute}' AND " +
                $"tags.{nameof(OpcUaTwinRegistration.DeviceType)} = 'Application'";
            string continuation = null;
            do {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation);
                foreach (var twin in devices.Items) {
                    var application = OpcUaApplicationRegistration.FromTwin(twin);
                    if (application.NotSeenSince == null ||
                        application.NotSeenSince.Value >= absolute) {
                        // Skip
                        continue;
                    }
                    await UnregisterApplicationAsync(application.ApplicationId);
                }
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
        }

        /// <summary>
        /// Process discovery sweep results from supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="events"></param>
        /// <param name="hardDelete"></param>
        /// <returns></returns>
        public async Task ProcessDiscoveryAsync(string supervisorId,
            IEnumerable<DiscoveryEventModel> events, bool hardDelete) {

            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }

            //
            // Now also get all applications for this supervisor or the site the application
            // was found in.  There should only be one site in the found application set
            // or none, otherwise, throw.  The OR covers where site of a supervisor was
            // changed after a discovery run (same supervisor that registered, but now
            // different site reported).
            //
            var sites = events.Select(e => e.Application.SiteId).Distinct();
            if (sites.Count() > 1) {
                throw new ArgumentException("Unexpected number of sites in discovery");
            }
            var siteId = sites.SingleOrDefault() ?? supervisorId;
            var results = await _iothub.QueryDeviceTwinsAsync("SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaTwinRegistration.DeviceType)} = 'Application' AND " +
                $"(tags.{nameof(OpcUaApplicationRegistration.SiteId)} = '{siteId}' OR" +
                $" tags.{nameof(OpcUaTwinRegistration.SupervisorId)} = '{supervisorId}')");

            var existing = results.Select(
                t => OpcUaApplicationRegistration.FromTwin(t));
            var found = events.Select(
                ev => OpcUaApplicationRegistration.FromServiceModel(ev.Application, false));

            // Create endpoints lookup table per found application id
            var endpoints = events.GroupBy(k => k.Application.ApplicationId).ToDictionary(
                group => group.Key,
                group => group
                    .Select(ev =>
                        OpcUaEndpointRegistration.FromServiceModel(new TwinInfoModel {
                            ApplicationId = group.Key,
                            Registration = ev.Registration
                        }, false))
                    .ToList());
            //
            // Merge found with existing applications. For disabled applications this will
            // take ownership regardless of supervisor, unfound applications are only disabled
            // and existing ones are patched only if they were previously reported by the same
            // supervisor.  New ones are simply added.
            //
            var remove = new HashSet<OpcUaApplicationRegistration>(existing,
                OpcUaApplicationRegistration.Logical);
            var add = new HashSet<OpcUaApplicationRegistration>(found,
                OpcUaApplicationRegistration.Logical);
            var unchange = new HashSet<OpcUaApplicationRegistration>(existing,
                OpcUaApplicationRegistration.Logical);
            var change = new HashSet<OpcUaApplicationRegistration>(found,
                OpcUaApplicationRegistration.Logical);

            unchange.IntersectWith(add);
            change.IntersectWith(remove);
            remove.ExceptWith(found);
            add.ExceptWith(existing);

            var added = 0;
            var updated = 0;
            var unchanged = 0;
            var removed = 0;

            // Remove applications
            foreach (var item in remove) {
                try {
                    // Only touch applications the supervisor owns.
                    if (item.SupervisorId == supervisorId) {
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
                    else {
                        // Skip the ones owned by other supervisors
                        unchanged++;
                    }
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error("Exception during application removal.", () => ex);
                }
            }

            // Update applications and ...
            foreach (var item in unchange) {
                try {
                    if (item.SupervisorId == supervisorId || (item.IsDisabled ?? false)) {
                        // Get the new one we will patch over the existing one...
                        var patch = change.First(x =>
                            OpcUaApplicationRegistration.Logical.Equals(x, item));
                        if (item != patch) {

                            await _iothub.CreateOrUpdateAsync(
                                OpcUaApplicationRegistration.Patch(item, patch));
                            updated++;
                        }
                        else {
                            unchanged++;
                        }

                        endpoints.TryGetValue(patch.ApplicationId, out var epFound);
                        var epExisting = await GetEndpointsAsync(patch.ApplicationId);
                        // TODO: Handle case where we take ownership of all endpoints
                        await MergeEndpointsAsync(supervisorId, epFound, epExisting,
                            hardDelete);
                    }
                    else {
                        // TODO: Decide whether we merge endpoints...
                        unchanged++;
                    }
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error("Exception during update.", () => ex);
                }
            }

            // ... add brand new applications
            foreach (var item in add) {
                try {
                    var twin = OpcUaApplicationRegistration.Patch(null, item);
                    await _iothub.CreateOrUpdateAsync(twin);

                    // Add all new endpoints
                    endpoints.TryGetValue(item.ApplicationId, out var epFound);
                    await MergeEndpointsAsync(supervisorId,
                        epFound, Enumerable.Empty<OpcUaEndpointRegistration>(), hardDelete);
                    added++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error("Exception during discovery addition.", () => ex);
                }
            }

            // Notify callbacks
            await NotifyDiscoveryCallbacks(supervisorId, null);

            if (added != 0 || removed != 0) {
                _logger.Info($"... processed discovery results: {added} applications added, " +
                    $"{updated} enabled, {removed} disabled, and {unchanged} unchanged.",
                    () => { });
            }
        }

        /// <summary>
        /// Merge existing and newly found endpoints
        /// </summary>
        /// <param name="found"></param>
        /// <param name="existing"></param>
        /// <returns></returns>
        private async Task MergeEndpointsAsync(string supervisorId,
            IEnumerable<OpcUaEndpointRegistration> found,
            IEnumerable<OpcUaEndpointRegistration> existing, bool hardDelete) {

            if (found == null) {
                throw new ArgumentNullException(nameof(found));
            }
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            var remove = new HashSet<OpcUaEndpointRegistration>(existing,
                OpcUaEndpointRegistration.Logical);
            var add = new HashSet<OpcUaEndpointRegistration>(found,
                OpcUaEndpointRegistration.Logical);
            var unchange = new HashSet<OpcUaEndpointRegistration>(existing,
                OpcUaEndpointRegistration.Logical);
            var change = new HashSet<OpcUaEndpointRegistration>(found,
                OpcUaEndpointRegistration.Logical);

            unchange.IntersectWith(add);
            change.IntersectWith(remove);
            remove.ExceptWith(found);
            add.ExceptWith(existing);

            var added = 0;
            var updated = 0;
            var unchanged = 0;
            var removed = 0;

            // Remove or disable an endpoint
            foreach (var item in remove) {
                try {
                    // Only touch applications the supervisor owns.
                    if (item.SupervisorId == supervisorId) {
                        if (hardDelete) {
                            var device = await _iothub.GetAsync(item.DeviceId);
                            // First we update any supervisor registration
                            var existingEndpoint = OpcUaEndpointRegistration.FromTwin(device, false);
                            if (!string.IsNullOrEmpty(existingEndpoint.SupervisorId)) {
                                await EnableTwinAsync(existingEndpoint.SupervisorId, device.Id, false);
                            }
                            // Then hard delete...
                            await _iothub.DeleteAsync(item.DeviceId);
                        }
                        else if (!(item.IsDisabled ?? false)) {
                            await _iothub.CreateOrUpdateAsync(
                                OpcUaEndpointRegistration.Patch(item,
                                    OpcUaEndpointRegistration.FromServiceModel(
                                        item.ToServiceModel(), true)));
                        }
                        else {
                            unchanged++;
                            continue;
                        }
                        removed++;
                    }
                    else {
                        // Skip the ones owned by other supervisors
                        unchanged++;
                    }
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error($"Exception during discovery removal.", () => ex);
                }
            }

            // Update endpoints that were disabled
            foreach (var item in unchange) {
                try {
                    if (item.SupervisorId == supervisorId || (item.IsDisabled ?? false)) {
                        // Get the new one we will patch over the existing one...
                        var patch = change.First(x =>
                            OpcUaEndpointRegistration.Logical.Equals(x, item));
                        if (item != patch) {
                            await _iothub.CreateOrUpdateAsync(
                                OpcUaEndpointRegistration.Patch(item, patch));
                            updated++;
                            continue;
                        }
                    }
                    unchanged++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error("Exception during update.", () => ex);
                }
            }

            // Add endpoint
            foreach (var item in add) {
                try {
                    await _iothub.CreateOrUpdateAsync(
                        OpcUaEndpointRegistration.Patch(null, item));
                    added++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error("Exception during discovery addition.", () => ex);
                }
            }

            if (added != 0 || removed != 0) {
                _logger.Info($"processed endpoint results: {added} endpoints added, {updated} " +
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
                $"tags.{nameof(OpcUaEndpointRegistration.ApplicationId)} = " +
                    $"'{application.ApplicationId}' AND " +
                $"tags.{nameof(OpcUaTwinRegistration.DeviceType)} = 'Endpoint'";
            string continuation = null;
            do {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation);
                foreach (var twin in devices.Items) {
                    var endpoint = OpcUaEndpointRegistration.FromTwin(twin, true);
                    if (endpoint.IsDisabled ?? false) {
                        continue;
                    }
                    try {
                        if (endpoint.Activated ?? false) {
                            if (!string.IsNullOrEmpty(endpoint.SupervisorId)) {
                                await EnableTwinAsync(endpoint.SupervisorId, twin.Id, false);
                            }
                        }
                        await _iothub.CreateOrUpdateAsync(OpcUaEndpointRegistration.Patch(
                            endpoint, OpcUaEndpointRegistration.FromServiceModel(
                                endpoint.ToServiceModel(), true))); // Disable
                    }
                    catch (Exception ex) {
                        _logger.Debug($"Failed disabling of twin {twin.Id}", () => ex);
                    }
                }
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
            if (application.IsDisabled ?? false) {
                return;
            }
            await _iothub.CreateOrUpdateAsync(OpcUaApplicationRegistration.Patch(
                application, OpcUaApplicationRegistration.FromServiceModel(
                    application.ToServiceModel(), true)));
        }

        /// <summary>
        /// Get all endpoints for application id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        private async Task<IEnumerable<OpcUaEndpointRegistration>> GetEndpointsAsync(
            string applicationId) {
            // Find all devices where endpoint information is configured
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(OpcUaEndpointRegistration.ApplicationId)} = " +
                    $"'{applicationId}' AND " +
                $"tags.{nameof(OpcUaTwinRegistration.DeviceType)} = 'Endpoint'";

            var result = new List<DeviceTwinModel>();
            string continuation = null;
            do {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, null);
                result.AddRange(devices.Items);
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
            return result
                .Select(d => OpcUaEndpointRegistration.FromTwin(d, false))
                .Where(r => r != null);
        }

        /// <summary>
        /// Enable or disable twin on supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="twinId"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        private async Task EnableTwinAsync(string supervisorId, string twinId,
            bool enable = true) {

            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (string.IsNullOrEmpty(supervisorId)) {
                return; // ok, no supervisor
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out var moduleId);
            // Remove from supervisor - this disconnects the device
            if (!enable) {
                await _iothub.UpdatePropertyAsync(deviceId, moduleId, twinId,
                    null);
            }
            // Enable
            else {
                var device = await _iothub.GetRegistrationAsync(twinId);
                // Update supervisor to start supervising this endpoint
                await _iothub.UpdatePropertyAsync(deviceId, moduleId, device.Id,
                    device.Authentication.PrimaryKey);
            }
        }

        /// <summary>
        /// Notify discovery callbacks about change
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="change"></param>
        /// <returns></returns>
        private async Task NotifyDiscoveryCallbacks(string supervisorId,
            DiscoveryMode? change) {
            try {
                var supervisor = await GetSupervisorAsync(supervisorId);
                var ev = change?.ToString() ?? "Completed";
                if (supervisor?.DiscoveryCallbacks?.Any() ?? false) {
                    await Task.WhenAll(supervisor.DiscoveryCallbacks.Select(async uri => {
                        var builder = new UriBuilder(uri);
                        if (string.IsNullOrEmpty(builder.Query)) {
                            builder.Query = "?";
                        }
                        else {
                            builder.Query += "&";
                        }
                        builder.Query +=
                            $"event={ev.UrlEncode()}&supervisorId={supervisorId.UrlEncode()}";
                        if (!string.IsNullOrEmpty(supervisor.SiteId)) {
                            builder.Query += $"&siteId ={supervisor.SiteId.UrlEncode()}";
                        }
                        await _client.GetAsync(new HttpRequest {
                            Uri = builder.Uri
                        });
                    }));
                }
            }
            catch (Exception ex) {
                _logger.Debug($"Failed to notify callbacks.  Continue...",
                    () => ex);
                // Continue...
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

        private readonly IIoTHubTwinServices _iothub;
        private readonly IHttpClient _client;
        private readonly IOpcUaValidationServices _validator;
        private readonly ILogger _logger;
    }
}
