// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Exceptions;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Utils;

    /// <summary>
    /// Endpoint services using the IoT Hub twin services for endpoint
    /// identity registration/retrieval.
    /// </summary>
    public sealed class RegistryServices : IEndpointRegistry, ISupervisorRegistry,
        IApplicationRegistry, IRegistryMaintenance {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="client"></param>
        /// <param name="activate"></param>
        /// <param name="logger"></param>
        public RegistryServices(IIoTHubTwinServices iothub, IHttpClient client,
            IActivationServices<EndpointRegistrationModel> activate, ILogger logger) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activator = activate ?? throw new ArgumentNullException(nameof(activate));
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoModel> GetEndpointAsync(string id,
            bool onlyServerState) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var device = await _iothub.GetAsync(id);
            return TwinModelToEndpointRegistrationModel(device, onlyServerState, false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(string continuation,
            bool onlyServerState, int? pageSize) {
            // Find all devices where endpoint information is configured
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(BaseRegistration.DeviceType)} = 'Endpoint' " +
                $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize);

            return new EndpointInfoListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(d => TwinModelToEndpointRegistrationModel(d, onlyServerState, true))
                    .Where(x => x != null)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel model, bool onlyServerState, int? pageSize) {

            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(EndpointRegistration.DeviceType)} = 'Endpoint' ";

            if (!(model?.IncludeNotSeenSince ?? false)) {
                // Scope to non deleted twins
                query += $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)}) ";
            }
            if (model?.Url != null) {
                // If Url provided, include it in search
                query += $"AND tags.{nameof(EndpointRegistration.EndpointUrlLC)} = " +
                    $"'{model.Url.ToLowerInvariant()}' ";
            }
            if (model?.Certificate != null) {
                // If cert provided, include it in search
                query += $"AND tags.{nameof(BaseRegistration.Thumbprint)} = " +
                    $"{model.Certificate.ToSha1Hash()} ";
            }
            if (model?.SecurityMode != null) {
                // If SecurityMode provided, include it in search
                query += $"AND properties.desired.{nameof(EndpointRegistration.SecurityMode)} = " +
                    $"'{model.SecurityMode}' ";
            }
            if (model?.SecurityPolicy != null) {
                // If SecurityPolicy uri provided, include it in search
                query += $"AND properties.desired.{nameof(EndpointRegistration.SecurityPolicy)} = " +
                    $"'{model.SecurityPolicy}' ";
            }
            if (model?.UserAuthentication != null) {
                // If TokenType provided, include it in search
                if (model.UserAuthentication.Value != CredentialType.None) {
                    query += $"AND properties.desired.{nameof(EndpointRegistration.CredentialType)} = " +
                            $"'{model.UserAuthentication}' ";
                }
                else {
                    query += $"AND (properties.desired.{nameof(EndpointRegistration.CredentialType)} = " +
                            $"'{model.UserAuthentication}' " +
                        $"OR NOT IS_DEFINED(tags.{nameof(EndpointRegistration.CredentialType)})) ";
                }
            }
            if (model?.Connected != null) {
                // If flag provided, include it in search
                if (model.Connected.Value) {
                    query += $"AND connectionState = 'Connected' ";
                        // Do not use connected property as module might have exited before updating.
                }
                else {
                    query += $"AND (connectionState = 'Disconnected' " +
                        $"OR properties.reported.{TwinProperty.kConnected} != true) ";
                }
            }
            if (model?.Activated != null) {
                // If flag provided, include it in search
                if (model.Activated.Value) {
                    query += $"AND tags.{nameof(EndpointRegistration.Activated)} = true ";
                }
                else {
                    query += $"AND (tags.{nameof(EndpointRegistration.Activated)} != true " +
                        $"OR NOT IS_DEFINED(tags.{nameof(EndpointRegistration.Activated)})) ";
                }
            }
            if (model?.EndpointState != null) {
                query += $"AND properties.reported.{nameof(EndpointRegistration.State)} = " +
                    $"'{model.EndpointState}' ";
            }
            var result = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize);
            return new EndpointInfoListModel {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(t => EndpointRegistration.FromTwin(t, onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task UpdateEndpointAsync(string endpointId,
            EndpointRegistrationUpdateModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentException(nameof(endpointId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var twin = await _iothub.GetAsync(endpointId);
            if (twin.Id != endpointId) {
                throw new ArgumentException("Id must be same as twin to patch",
                    nameof(endpointId));
            }

            // Convert to twin registration
            var registration = BaseRegistration.ToRegistration(twin, true)
                as EndpointRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{endpointId} is not a endpoint registration.");
            }

            // Update registration from update request
            var patched = registration.ToServiceModel();

            var duplicate = false;
            if (request.User != null) {
                patched.Registration.Endpoint.User = new CredentialModel();

                if (request.User.Type != null) {
                    // Change token type?  Always duplicate since id changes.
                    duplicate = request.User.Type !=
                        patched.Registration.Endpoint.User.Type;

                    patched.Registration.Endpoint.User.Type =
                        (CredentialType)request.User.Type;
                }
                if ((patched.Registration.Endpoint.User.Type
                    ?? CredentialType.None) != CredentialType.None) {
                    patched.Registration.Endpoint.User.Value =
                        request.User.Value;
                }
                else {
                    patched.Registration.Endpoint.User.Value = null;
                }
            }

            // Patch
            await _iothub.CreateOrUpdateAsync(EndpointRegistration.Patch(
                duplicate ? null : registration,
                EndpointRegistration.FromServiceModel(patched, registration.IsDisabled)));
                        // To have duplicate item disabled, too if needed
        }

        /// <inheritdoc/>
        public async Task ActivateEndpointAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var twin = await _iothub.GetAsync(id);
            if (twin.Id != id) {
                throw new ArgumentException("Id must be same as twin to activate",
                    nameof(id));
            }

            // Convert to twin registration
            var registration = BaseRegistration.ToRegistration(twin, true)
                as EndpointRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{id} is not an activatable endpoint registration.");
            }
            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentException($"Twin {id} not registered with a supervisor.");
            }

            if (!(registration.Activated ?? false)) {
                var patched = registration.ToServiceModel();
                patched.ActivationState = EndpointActivationState.Activated;

                // Update supervisor settings
                var secret = await _iothub.GetPrimaryKeyAsync(registration.DeviceId);
                try {
                    // Call down to supervisor to activate - this can fail
                    await _activator.ActivateEndpointAsync(patched.Registration, secret);

                    // Update supervisor desired properties
                    await SetSupervisorTwinSecretAsync(registration.SupervisorId,
                        registration.DeviceId, secret);
                    // Write twin activation status in twin settings
                    await _iothub.CreateOrUpdateAsync(EndpointRegistration.Patch(
                        registration, EndpointRegistration.FromServiceModel(patched,
                            registration.IsDisabled)));
                }
                catch (Exception ex) {
                    // Undo activation
                    await Try.Async(() => _activator.DeactivateEndpointAsync(
                        patched.Registration));
                    await Try.Async(() => SetSupervisorTwinSecretAsync(
                        registration.SupervisorId, registration.DeviceId, null));
                    _logger.Error(ex, "Failed to activate twin");
                    throw ex;
                }
            }
        }

        /// <inheritdoc/>
        public async Task DeactivateEndpointAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            // Get existing endpoint and compare to see if we need to patch.
            var twin = await _iothub.GetAsync(id);
            if (twin.Id != id) {
                throw new ArgumentException("Id must be same as twin to deactivate",
                    nameof(id));
            }
            // Convert to twin registration
            var registration = BaseRegistration.ToRegistration(twin, true)
                as EndpointRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{id} is not an activatable endpoint registration.");
            }
            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentException($"Twin {id} not registered with a supervisor.");
            }
            var patched = registration.ToServiceModel();

            // Deactivate twin in twin settings
            await SetSupervisorTwinSecretAsync(registration.SupervisorId,
                registration.DeviceId, null);
            // Call down to supervisor to ensure deactivation is complete
            await Try.Async(() => _activator.DeactivateEndpointAsync(patched.Registration));

            // Mark as deactivated
            if (registration.Activated ?? false) {
                patched.ActivationState = EndpointActivationState.Deactivated;
                await _iothub.CreateOrUpdateAsync(EndpointRegistration.Patch(
                    registration, EndpointRegistration.FromServiceModel(patched,
                        registration.IsDisabled)));
            }
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResultModel> RegisterAsync(
            ApplicationRegistrationRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ApplicationUri == null) {
                throw new ArgumentNullException(nameof(request.ApplicationUri));
            }
            var registration = ApplicationRegistration.FromServiceModel(
                new ApplicationInfoModel {
                    ApplicationName = request.ApplicationName,
                    Locale = request.Locale,
                    ProductUri = request.ProductUri,
                    DiscoveryUrls = request.DiscoveryUrls,
                    DiscoveryProfileUri = request.DiscoveryProfileUri,
                    ApplicationType = request.ApplicationType ?? ApplicationType.Server,
                    ApplicationUri = request.ApplicationUri,
                    Capabilities = request.Capabilities,
                    SiteId = null
                });
            await _iothub.CreateOrUpdateAsync(ApplicationRegistration.Patch(
                null, registration));
            return new ApplicationRegistrationResultModel {
                Id = registration.ApplicationId
            };
        }

        /// <inheritdoc/>
        public async Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentException(nameof(applicationId));
            }

            // Get existing application and compare to see if we need to patch.
            var application = await _iothub.GetAsync(applicationId);
            if (application.Id != applicationId) {
                throw new ArgumentException("Id must be same as application to patch",
                    nameof(applicationId));
            }

            // Convert to application registration
            var registration = BaseRegistration.ToRegistration(application)
                as ApplicationRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{applicationId} is not a registered application.");
            }

            // Update registration from update request
            var patched = registration.ToServiceModel();
            if (request.ApplicationName != null) {
                patched.ApplicationName = string.IsNullOrEmpty(request.ApplicationName) ?
                    null : request.ApplicationName;
            }
            if (request.Locale != null) {
                patched.Locale = string.IsNullOrEmpty(request.Locale) ?
                    null : request.Locale;
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
                    null : request.DiscoveryProfileUri;
            }
            // Patch
            await _iothub.CreateOrUpdateAsync(ApplicationRegistration.Patch(
                registration, ApplicationRegistration.FromServiceModel(patched)));
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveTwins) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentException(nameof(applicationId));
            }
            var device = await _iothub.GetAsync(applicationId);
            var registration = BaseRegistration.ToRegistration(device)
                as ApplicationRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{applicationId} is not an application registration.");
            }
            var application = registration.ToServiceModel();
            // Include deleted twins if the application itself is deleted.  Otherwise omit.
            var endpoints = await GetEndpointsAsync(applicationId,
                application.NotSeenSince != null);
            return new ApplicationRegistrationModel {
                Application = application,
                Endpoints = endpoints
                    .Where(e => !filterInactiveTwins || (e.Connected && (e.Activated ?? false)))
                    .Select(e => e.ToServiceModel())
                    .Select(t => t.Registration)
                    .ToList()
            }.SetSecurityAssessment();
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel model, int? pageSize) {

            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(ApplicationRegistration.DeviceType)} = 'Application' ";

            if (!(model?.IncludeNotSeenSince ?? false)) {
                // Scope to non deleted applications
                query += $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)}) ";
            }
            if (model?.ApplicationName != null) {
                // If application name provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.ApplicationName)} = " +
                    $"'{model.ApplicationName}' ";
            }
            if (model?.Locale != null) {
                // If application name locale provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.Locale)} = " +
                    $"'{model.Locale}' ";
            }
            if (model?.ProductUri != null) {
                // If product uri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.ProductUri)} = " +
                    $"'{model.ProductUri}' ";
            }
            if (model?.ApplicationUri != null) {
                // If ApplicationUri provided, include it in search
                query += $"AND tags.{nameof(ApplicationRegistration.ApplicationUriLC)} = " +
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
            if (model?.Capability != null) {
                // If Capabilities provided, filter results
                query += $"AND tags.{JTokenEx.SanitizePropertyName(model.Capability).ToUpperInvariant()} = true ";
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
                    .Select(ApplicationRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize) {
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(ApplicationRegistration.DeviceType)} = 'Application' " +
                $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
            var result = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize);
            return new ApplicationInfoListModel {
                ContinuationToken = result.ContinuationToken,
                Items = result.Items
                    .Select(ApplicationRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize) {
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
        public async Task UnregisterApplicationAsync(string applicationId) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentException(nameof(applicationId));
            }

            // Get all twin registrations and for each one, call delete, if failure,
            // stop half way and throw and do not complete.
            var result = await GetEndpointsAsync(applicationId, true);
            foreach(var twin in result) {
                try {
                    if (!string.IsNullOrEmpty(twin.SupervisorId)) {
                        await SetSupervisorTwinSecretAsync(twin.SupervisorId,
                            twin.DeviceId, null);
                    }
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Failed unregistration of twin {deviceId}",
                        twin.DeviceId);
                }
                await _iothub.DeleteAsync(twin.DeviceId);
            }
            await _iothub.DeleteAsync(applicationId);
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string id,
            bool onlyServerState) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(id, out var moduleId);
            var device = await _iothub.GetAsync(deviceId, moduleId);
            var registration = BaseRegistration.ToRegistration(device, onlyServerState)
                as SupervisorRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{id} is not a supervisor registration.");
            }
            return registration.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentException(nameof(supervisorId));
            }

            // Get existing endpoint and compare to see if we need to patch.
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out var moduleId);
            var twin = await _iothub.GetAsync(deviceId, moduleId);

            if (twin.Id != deviceId && twin.ModuleId != moduleId) {
                throw new ArgumentException("Id must be same as twin to patch",
                    nameof(supervisorId));
            }

            var registration = BaseRegistration.ToRegistration(twin, true)
                as SupervisorRegistration;
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{supervisorId} is not a supervisor registration.");
            }

            // Update registration from update request
            var patched = registration.ToServiceModel();
            if (request.Discovery != null) {
                patched.Discovery = (DiscoveryMode)request.Discovery;
            }

            if (request.SiteId != null) {
                patched.SiteId = string.IsNullOrEmpty(request.SiteId) ?
                    null : request.SiteId;
            }

            if (request.LogLevel != null) {
                patched.LogLevel = request.LogLevel == SupervisorLogLevel.Information ?
                    null : request.LogLevel;
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
                if (request.DiscoveryConfig.ActivationFilter != null) {
                    patched.DiscoveryConfig.ActivationFilter =
                        request.DiscoveryConfig.ActivationFilter.SecurityMode == null &&
                        request.DiscoveryConfig.ActivationFilter.SecurityPolicies == null &&
                        request.DiscoveryConfig.ActivationFilter.TrustLists == null ?
                            null : request.DiscoveryConfig.ActivationFilter;
                }
            }
            // Patch
            await _iothub.CreateOrUpdateAsync(SupervisorRegistration.Patch(
                registration, SupervisorRegistration.FromServiceModel(patched)));
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string continuation, bool onlyServerState, int? pageSize) {
            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.kType} = 'supervisor' " +
                $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
            var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation, pageSize);
            return new SupervisorListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(t => SupervisorRegistration.FromTwin(t, onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel model, bool onlyServerState, int? pageSize) {

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.kType} = 'supervisor'";

            if (model?.Discovery != null) {
                // If discovery mode provided, include it in search
                query += $"AND properties.desired.{nameof(SupervisorRegistration.Discovery)} = " +
                    $"'{model.Discovery}' ";
            }
            if (model?.SiteId != null) {
                // If site id provided, include it in search
                query += $"AND (properties.reported.{TwinProperty.kSiteId} = " +
                    $"'{model.SiteId}' OR properties.desired.{TwinProperty.kSiteId} = " +
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
                        $"OR properties.reported.{TwinProperty.kConnected} != true) ";
                }
            }

            var queryResult = await _iothub.QueryDeviceTwinsAsync(query, null, pageSize);
            return new SupervisorListModel {
                ContinuationToken = queryResult.ContinuationToken,
                Items = queryResult.Items
                    .Select(t => SupervisorRegistration.FromTwin(t, onlyServerState))
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenSince) {
            var absolute = DateTime.UtcNow - notSeenSince;
            var query = "SELECT * FROM devices WHERE " +
                $"tags.{nameof(BaseRegistration.DeviceType)} = 'Application' " +
            //    $"AND tags.{nameof(OpcUaEndpointRegistration.NotSeenSince)} <= '{absolute}' " +
                $"AND IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)}) ";
            string continuation = null;
            do {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation);
                foreach (var twin in devices.Items) {
                    var application = ApplicationRegistration.FromTwin(twin);
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

        /// <inheritdoc/>
        public async Task ProcessDiscoveryAsync(string supervisorId, DiscoveryResultModel result,
            IEnumerable<DiscoveryEventModel> events, bool hardDelete) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            if (result == null) {
                throw new ArgumentNullException(nameof(result));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }
            if ((result.RegisterOnly ?? false) && !events.Any()) {
                return;
            }
            var sites = events.Select(e => e.Application.SiteId).Distinct();
            if (sites.Count() > 1) {
                throw new ArgumentException("Unexpected number of sites in discovery");
            }
            var siteId = sites.SingleOrDefault() ?? supervisorId;

            try {
                //
                // Merge in global discovery configuration into the one sent by the supervisor.
                //
                var supervisor = await GetSupervisorAsync(supervisorId, false);
                if (result.DiscoveryConfig == null) {
                    // Use global discovery configuration
                    result.DiscoveryConfig = supervisor.DiscoveryConfig;
                }
                else {
                    if (supervisor.DiscoveryConfig?.Callbacks != null) {
                        if (result.DiscoveryConfig.Callbacks == null) {
                            result.DiscoveryConfig.Callbacks =
                                supervisor.DiscoveryConfig.Callbacks;
                        }
                        else {
                            result.DiscoveryConfig.Callbacks.AddRange(
                                supervisor.DiscoveryConfig.Callbacks);
                        }
                    }
                    if (result.DiscoveryConfig.ActivationFilter == null) {
                        // Use global activation filter
                        result.DiscoveryConfig.ActivationFilter =
                            supervisor.DiscoveryConfig?.ActivationFilter;
                    }
                }

                //
                // Now also get all applications for this supervisor or the site the application
                // was found in.  There should only be one site in the found application set
                // or none, otherwise, throw.  The OR covers where site of a supervisor was
                // changed after a discovery run (same supervisor that registered, but now
                // different site reported).
                //
                var twins = await _iothub.QueryDeviceTwinsAsync("SELECT * FROM devices WHERE " +
                    $"tags.{nameof(BaseRegistration.DeviceType)} = 'Application' AND " +
                    $"(tags.{nameof(ApplicationRegistration.SiteId)} = '{siteId}' OR" +
                    $" tags.{nameof(BaseRegistration.SupervisorId)} = '{supervisorId}')");
                var existing = twins
                    .Select(ApplicationRegistration.FromTwin);
                var found = events
                    .Select(ev => ApplicationRegistration.FromServiceModel(ev.Application,
                        false));

                // Create endpoints lookup table per found application id
                var endpoints = events.GroupBy(k => k.Application.ApplicationId).ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(ev =>
                            EndpointRegistration.FromServiceModel(new EndpointInfoModel {
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
                var remove = new HashSet<ApplicationRegistration>(existing,
                    ApplicationRegistration.Logical);
                var add = new HashSet<ApplicationRegistration>(found,
                    ApplicationRegistration.Logical);
                var unchange = new HashSet<ApplicationRegistration>(existing,
                    ApplicationRegistration.Logical);
                var change = new HashSet<ApplicationRegistration>(found,
                    ApplicationRegistration.Logical);

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
                            _logger.Error(ex, "Exception during application removal.");
                        }
                    }
                }

                // Update applications and ...
                foreach (var exists in unchange) {
                    try {
                        if (exists.SupervisorId == supervisorId || (exists.IsDisabled ?? false)) {
                            // Get the new one we will patch over the existing one...
                            var patch = change.First(x =>
                                ApplicationRegistration.Logical.Equals(x, exists));
                            if (exists != patch) {
                                await _iothub.CreateOrUpdateAsync(
                                    ApplicationRegistration.Patch(exists, patch));
                                updated++;
                            }
                            else {
                                unchanged++;
                            }

                            endpoints.TryGetValue(patch.ApplicationId, out var epFound);
                            var epExisting = await GetEndpointsAsync(patch.ApplicationId, true);
                            // TODO: Handle case where we take ownership of all endpoints
                            await MergeEndpointsAsync(result, supervisorId, epFound, epExisting,
                                hardDelete);
                        }
                        else {
                            // TODO: Decide whether we merge endpoints...
                            unchanged++;
                        }
                    }
                    catch (Exception ex) {
                        unchanged++;
                        _logger.Error(ex, "Exception during update.");
                    }
                }

                // ... add brand new applications
                foreach (var item in add) {
                    try {
                        var twin = ApplicationRegistration.Patch(null, item);
                        await _iothub.CreateOrUpdateAsync(twin);

                        // Add all new endpoints
                        endpoints.TryGetValue(item.ApplicationId, out var epFound);
                        await MergeEndpointsAsync(result, supervisorId,
                            epFound, Enumerable.Empty<EndpointRegistration>(), hardDelete);
                        added++;
                    }
                    catch (Exception ex) {
                        unchanged++;
                        _logger.Error(ex, "Exception during discovery addition.");
                    }
                }
                // Notify callbacks
                await CallDiscoveryCallbacksAsync(result, supervisorId, siteId, null);

                var log = added != 0 || removed != 0 || updated != 0;
#if DEBUG
                log = true;
#endif
                if (log) {
                    _logger.Information("... processed discovery results from {supervisorId}: " +
                        "{added} applications added, {updated} enabled, {removed} disabled, and " +
                        "{unchanged} unchanged.", supervisorId, added, updated, removed, unchanged);
                }
            }
            catch (Exception ex) {
                // Notify callbacks
                await CallDiscoveryCallbacksAsync(result, supervisorId, siteId, ex);
                throw ex;
            }
        }

        /// <summary>
        /// Merge existing and newly found endpoints
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="result"></param>
        /// <param name="found"></param>
        /// <param name="existing"></param>
        /// <param name="hardDelete"></param>
        /// <returns></returns>
        private async Task MergeEndpointsAsync(DiscoveryResultModel result,
            string supervisorId, IEnumerable<EndpointRegistration> found,
            IEnumerable<EndpointRegistration> existing, bool hardDelete) {

            if (found == null) {
                throw new ArgumentNullException(nameof(found));
            }
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            var remove = new HashSet<EndpointRegistration>(existing,
                EndpointRegistration.Logical);
            var add = new HashSet<EndpointRegistration>(found,
                EndpointRegistration.Logical);
            var unchange = new HashSet<EndpointRegistration>(existing,
                EndpointRegistration.Logical);
            var change = new HashSet<EndpointRegistration>(found,
                EndpointRegistration.Logical);

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
                            var existingEndpoint = EndpointRegistration.FromTwin(device, false);
                            if (!string.IsNullOrEmpty(existingEndpoint.SupervisorId)) {
                                await SetSupervisorTwinSecretAsync(existingEndpoint.SupervisorId,
                                    device.Id, null);
                            }
                            // Then hard delete...
                            await _iothub.DeleteAsync(item.DeviceId);
                        }
                        else if (!(item.IsDisabled ?? false)) {
                            await _iothub.CreateOrUpdateAsync(
                                EndpointRegistration.Patch(item,
                                    EndpointRegistration.FromServiceModel(
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
                    _logger.Error(ex, "Exception during discovery removal.");
                }
            }

            // Update endpoints that were disabled
            foreach (var exists in unchange) {
                try {
                    if (exists.SupervisorId == supervisorId || (exists.IsDisabled ?? false)) {
                        // Get the new one we will patch over the existing one...
                        var patch = change.First(x =>
                            EndpointRegistration.Logical.Equals(x, exists));
                        await ApplyActivationFilterAsync(result.DiscoveryConfig?.ActivationFilter,
                            patch);
                        if (exists != patch) {
                            await _iothub.CreateOrUpdateAsync(EndpointRegistration.Patch(
                                exists, patch));
                            updated++;
                            continue;
                        }
                    }
                    unchanged++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error(ex, "Exception during update.");
                }
            }

            // Add endpoint
            foreach (var item in add) {
                try {
                    await ApplyActivationFilterAsync(result.DiscoveryConfig?.ActivationFilter,
                        item);
                    await _iothub.CreateOrUpdateAsync(EndpointRegistration.Patch(null, item));
                    added++;
                }
                catch (Exception ex) {
                    unchanged++;
                    _logger.Error(ex, "Exception during discovery addition.");
                }
            }

            if (added != 0 || removed != 0) {
                _logger.Information("processed endpoint results: {added} endpoints added, {updated} " +
                    "updated, {removed} removed or disabled, and {unchanged} unchanged.",
                    added, updated, removed, unchanged);
            }
        }

        /// <summary>
        /// Disable application and all related endpoints
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        private async Task DisableApplicationAsync(ApplicationRegistration application) {
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(EndpointRegistration.ApplicationId)} = " +
                    $"'{application.ApplicationId}' AND " +
                $"tags.{nameof(BaseRegistration.DeviceType)} = 'Endpoint'";
            string continuation = null;
            do {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, continuation);
                foreach (var twin in devices.Items) {
                    var endpoint = EndpointRegistration.FromTwin(twin, true);
                    if (endpoint.IsDisabled ?? false) {
                        continue;
                    }
                    try {
                        if (endpoint.Activated ?? false) {
                            if (!string.IsNullOrEmpty(endpoint.SupervisorId)) {
                                await SetSupervisorTwinSecretAsync(endpoint.SupervisorId,
                                    twin.Id, null);
                            }
                        }
                        await _iothub.CreateOrUpdateAsync(EndpointRegistration.Patch(
                            endpoint, EndpointRegistration.FromServiceModel(
                                endpoint.ToServiceModel(), true))); // Disable
                    }
                    catch (Exception ex) {
                        _logger.Debug(ex, "Failed disabling of twin {twin}", twin.Id);
                    }
                }
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
            if (application.IsDisabled ?? false) {
                return;
            }
            await _iothub.CreateOrUpdateAsync(ApplicationRegistration.Patch(
                application, ApplicationRegistration.FromServiceModel(
                    application.ToServiceModel(), true)));
        }

        /// <summary>
        /// Get all endpoints for application id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="includeDeleted"></param>
        /// <returns></returns>
        private async Task<IEnumerable<EndpointRegistration>> GetEndpointsAsync(
            string applicationId, bool includeDeleted) {
            // Find all devices where endpoint information is configured
            var query = $"SELECT * FROM devices WHERE " +
                $"tags.{nameof(EndpointRegistration.ApplicationId)} = " +
                    $"'{applicationId}' AND " +
                $"tags.{nameof(BaseRegistration.DeviceType)} = 'Endpoint' ";

            if (!includeDeleted) {
                query += $"AND NOT IS_DEFINED(tags.{nameof(BaseRegistration.NotSeenSince)})";
            }

            var result = new List<DeviceTwinModel>();
            string continuation = null;
            do {
                var devices = await _iothub.QueryDeviceTwinsAsync(query, null);
                result.AddRange(devices.Items);
                continuation = devices.ContinuationToken;
            }
            while (continuation != null);
            return result
                .Select(d => EndpointRegistration.FromTwin(d, false))
                .Where(r => r != null);
        }

        /// <summary>
        /// Apply activation filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private async Task<string> ApplyActivationFilterAsync(
            EndpointActivationFilterModel filter, EndpointRegistration endpoint) {
            if (filter == null || endpoint == null) {
                return null;
            }

            // TODO: Get trust list entry and validate endpoint.Certificate

            var mode = endpoint.SecurityMode ?? SecurityMode.None;
            if (!mode.MatchesFilter(filter.SecurityMode ?? SecurityMode.Best)) {
                return null;
            }
            var policy = endpoint.SecurityPolicy;
            if (filter.SecurityPolicies != null) {
                if (!filter.SecurityPolicies.Any(p =>
                    p.EqualsIgnoreCase(endpoint.SecurityPolicy))) {
                    return null;
                }
            }
            try {
                // Get endpoint twin secret
                var secret = await _iothub.GetPrimaryKeyAsync(endpoint.DeviceId);

                // Try activate endpoint - if possible...
                await _activator.ActivateEndpointAsync(
                    endpoint.ToServiceModel().Registration, secret);

                // Mark in supervisor
                await SetSupervisorTwinSecretAsync(endpoint.SupervisorId,
                    endpoint.DeviceId, secret);
                endpoint.Activated = true;
                return secret;
            }
            catch (Exception ex) {
                _logger.Information(ex, "Failed activating {eeviceId} based off " +
                    "filter.  Manual activation required.", endpoint.DeviceId);
                return null;
            }
        }

        /// <summary>
        /// Enable or disable twin on supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="twinId"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        private async Task SetSupervisorTwinSecretAsync(string supervisorId,
            string twinId, string secret) {

            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (string.IsNullOrEmpty(supervisorId)) {
                return; // ok, no supervisor
            }
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId, out var moduleId);
            if (secret == null) {
                // Remove from supervisor - this disconnects the device
                await _iothub.UpdatePropertyAsync(deviceId, moduleId, twinId, null);
                _logger.Information("Twin {twinId} deactivated on {supervisorId}.",
                    twinId, supervisorId);
            }
            else {
                // Update supervisor to start supervising this endpoint
                await _iothub.UpdatePropertyAsync(deviceId, moduleId, twinId, secret);
                _logger.Information("Twin {twinId} activated on {supervisorId}.",
                    twinId, supervisorId);
            }
        }

        /// <summary>
        /// Notify discovery / registration callbacks
        /// </summary>
        /// <param name="result"></param>
        /// <param name="supervisorId"></param>
        /// <param name="siteId"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private async Task CallDiscoveryCallbacksAsync(DiscoveryResultModel result,
            string supervisorId, string siteId, Exception exception) {
            try {
                var callbacks = result.DiscoveryConfig.Callbacks;
                if (callbacks == null || callbacks.Count == 0) {
                    return;
                }
                await _client.CallAsync(JToken.FromObject(
                    new {
                        id = result.Id,
                        supervisorId,
                        siteId = siteId ?? supervisorId,
                        result = new {
                            config = result.DiscoveryConfig,
                            diagnostics = exception != null ?
                                JToken.FromObject(exception) : result.Diagnostics
                        }
                    }),
                    callbacks.ToArray());
            }
            catch (Exception ex) {
                _logger.Debug(ex, "Failed to notify callbacks. Continue...");
                // Continue...
            }
        }

        /// <summary>
        /// Convert device twin registration property to registration model
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="skipInvalid"></param>
        /// <param name="onlyServerState">Only desired should be returned
        /// this means that you will look at stale information.</param>
        /// <returns></returns>
        private static EndpointInfoModel TwinModelToEndpointRegistrationModel(
            DeviceTwinModel twin, bool onlyServerState, bool skipInvalid) {

            // Convert to twin registration
            var registration = BaseRegistration.ToRegistration(twin, onlyServerState)
                as EndpointRegistration;
            if (registration == null) {
                if (skipInvalid) {
                    return null;
                }
                throw new ResourceNotFoundException(
                    $"{twin.Id} is not a registered opc ua endpoint.");
            }
            return registration.ToServiceModel();
        }

        private readonly IActivationServices<EndpointRegistrationModel> _activator;
        private readonly IIoTHubTwinServices _iothub;
        private readonly IHttpClient _client;
        private readonly ILogger _logger;
    }
}
