// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.Common;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint services using the IoT Hub twin services for endpoint
    /// identity registration/retrieval.
    /// </summary>
    public class OpcUaRegistryServices : IOpcUaTwinRegistry, IOpcUaSupervisorRegistry,
        IOpcUaServerRegistry, IOpcUaRegistryMaintenance {

        /// <summary>
        /// Create using iot hub twin registry service client
        /// </summary>
        /// <param name="registry"></param>
        public OpcUaRegistryServices(IIoTHubTwinServices registry,
            IOpcUaEndpointValidator validator, ILogger logger) {

            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var device = await _registry.GetAsync(id);
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
            var twin = await _registry.GetAsync(request.Id);
            if (twin.Id != request.Id) {
                throw new ArgumentException("Id must be same as twin to patch",
                    nameof(request.Id));
            }

            // Convert to supervisor registration
            var registration = OpcUaSupervisorRegistration.FromTwin(twin, true,
                out var tmp);

            // Update registration from update request
            var patched = registration.ToServiceModel();
            if (request.Discovering != null) {
                patched.Discovering = (bool)request.Discovering;
            }
            if (request.Domain != null) {
                patched.Domain = string.IsNullOrEmpty(
                    request.Domain) ? null : request.Domain;
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
            string continuation) {
            var query = $"SELECT * FROM devices WHERE " +
                $"properties.reported.type = 'supervisor' OR tags.type = 'supervisor'";
            var devices = await _registry.QueryAsync(query, continuation);
            return new SupervisorListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(OpcUaSupervisorRegistration.FromTwin)
                    .Select(s => s.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Read specific twin registration by twin id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public async Task<TwinRegistrationModel> GetTwinAsync(string id,
            bool onlyServerState) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var device = await _registry.GetAsync(id);
            return TwinModelToTwinRegistrationModel(device, onlyServerState);
        }

        /// <summary>
        /// List all twin registrations
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public async Task<TwinRegistrationListModel> ListTwinsAsync(string continuation,
            bool onlyServerState) {
            // Find all devices where endpoint information is configured
            var query = $"SELECT * FROM devices WHERE " +
                $"IS_OBJECT(properties.desired.{k_endpointProperty})";
            var devices = await _registry.QueryAsync(query, continuation);
            return new TwinRegistrationListModel {
                ContinuationToken = devices.ContinuationToken,
                Items = devices.Items
                    .Select(d => TwinModelToTwinRegistrationModel(d, onlyServerState))
                    .ToList()
            };
        }

        /// <summary>
        /// Register opc ua endpoint in device twin registry and with edge
        /// controllers out there.  If id is provided, it must not be used,
        /// however, if the provided endpoint info is the same as the one
        /// registered under the id, we do not throw, but suceeed without
        /// doing anything.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<TwinRegistrationResultModel> RegisterTwinAsync(
            TwinRegistrationRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Endpoint == null) {
                throw new ArgumentNullException(nameof(request.Endpoint));
            }
            if (string.IsNullOrEmpty(request.Endpoint.Url)) {
                throw new ArgumentException(nameof(request.Endpoint.Url));
            }

            //
            // If id was passed, look up id to see if we already have a
            // registration of this endpoint.  If the endpoint exists, but is
            // not the same, throw exception.  User should rather call patch,
            // or first delete.
            //
            if (!string.IsNullOrEmpty(request.Id)) {
                try {
                    var existing = await _registry.GetAsync(request.Id);
                    if (OpcUaTwinRegistration.FromTwin(existing, true, out var tmp).Matches(
                        request.Endpoint)) {
                        _logger.Info($"Endpoint already registered as {existing.Id}!",
                            () => existing);
                        return new TwinRegistrationResultModel {
                            Id = existing.Id
                        };
                    }
                    throw new ConflictingResourceException(
                        $"Endpoint {nameof(request.Id)} must be updated.");
                }
                catch (ResourceNotFoundException) {
                    // Expected, now create new
                }
            }

            //
            // Validate the endpoint at the edge which will fill in missing
            // information.
            //
            var validationResult = await _validator.ValidateAsync(request.Endpoint);

            //
            // If no id was provided, we look up all entries with the same
            // endpoint url. If there is one that matches, we return that
            // one instead.  This is not atomic, so multple registration could
            // end up with the same endpoint registered, however, this avoids
            // having too many duplicates.
            //
            if (string.IsNullOrEmpty(request.Id)) {
                var results = await _registry.QueryAsync("SELECT * FROM devices WHERE " +
                    $"IS_OBJECT(properties.desired.{ k_endpointProperty}) AND " +
                    $"tags.EndpointId = " +
                        $"'{validationResult.Endpoint.Url.ToLowerInvariant()}' AND " +
                    $"tags.ApplicationId = " +
                        $"'{validationResult.Server.ApplicationUri.ToLowerInvariant()}'");
                foreach (var candidate in results) {
                    if (OpcUaTwinRegistration.FromTwin(candidate, false, out var tmp)?.Matches(
                        validationResult.Endpoint) ?? false) {
                        _logger.Info(
                            $"Endpoint already registered under device {candidate.Id}",
                                () => candidate);
                        return new TwinRegistrationResultModel {
                            Id = candidate.Id
                        };
                    }
                }
            }

            var twin = new OpcUaTwinRegistration { DeviceId = request.Id }
                .Patch(validationResult);
            _logger.Debug($"Register new server endpoint twin", () => twin);
            twin = await _registry.CreateOrUpdateAsync(twin);

            if (validationResult.Endpoint.IsTrusted ?? false) {
                try {
                    // Enable twin
                    await EnableTwinAsync(validationResult.Endpoint.SupervisorId,
                        twin.Id, false);
                }
                catch (Exception e) {
                    // ouch - try to unroll registration and throw.
                    _logger.Error(
                        "Error during supervisor registration, delete device.", () => e);
                    await _registry.DeleteAsync(twin.Id);
                    throw e;
                }
            }
            return new TwinRegistrationResultModel {
                Id = twin.Id
            };
        }

        /// <summary>
        /// Process discovery sweep results from supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="servers"></param>
        /// <returns></returns>
        public async Task ProcessSupervisorDiscoveryAsync(string supervisorId,
            IEnumerable<ServerEndpointModel> servers) {

            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            if (servers == null) {
                throw new ArgumentNullException(nameof(servers));
            }

            // Get all endpoints for the supervisor
            var results = await _registry.QueryAsync("SELECT * FROM devices WHERE " +
                $"IS_OBJECT(properties.desired.{k_endpointProperty}) AND " +
                    $"tags.SupervisorId = '{supervisorId}'");
            var remove = new HashSet<OpcUaTwinRegistration>(
                results.Select(t => OpcUaTwinRegistration.FromTwin(t)));
            var add = new HashSet<OpcUaTwinRegistration>(
                servers.Select(s => OpcUaTwinRegistration.FromServiceModel(s)));

            // Calculate unchanged items
            var unchanged = 0;
            var added = 0;
            var removed = 0;

            // Remove items
            foreach(var item in remove) {
                if (add.Contains(item)) {
                    unchanged++;
                    continue;
                }
                try {
                    await DeleteTwinAsync(item.DeviceId); // TODO: Soft delete here...
                    removed++;
                }
                catch (Exception ex) {
                    _logger.Error("Exception during discovery removal.", () => ex);
                }
            }

            // Add items
            foreach (var item in add) {
                if (remove.Contains(item)) {
                    unchanged++;
                    continue;
                }

                // TODO: Check if same server owned by someone already...

                var twin = new OpcUaTwinRegistration().Patch(item.ToServiceModel());
                try {
                    await _registry.CreateOrUpdateAsync(twin);
                    added++;
                }
                catch (Exception ex) {
                    _logger.Error("Exception during discovery addition.", () => ex);
                }
            }

            if (add.Count != 0 || remove.Count != 0) {
                _logger.Info($"processed {supervisorId} discovery results: {added} " +
                    $"endpoints added, {removed} removed, and {unchanged} " +
                    "unchanged.", () => { });
            }
        }

        /// <summary>
        /// Find registration for the endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<TwinRegistrationModel> FindTwinAsync(EndpointModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            var results = await _registry.QueryAsync("SELECT * FROM devices WHERE " +
                $"IS_OBJECT(properties.desired.{k_endpointProperty}) AND " +
                    $"tags.EndpointId = '{endpoint.Url.ToLowerInvariant()}'");
            foreach (var candidate in results) {
                if (OpcUaTwinRegistration.FromTwin(candidate, false, out var tmp)
                    .Matches(endpoint)) {
                    return TwinModelToTwinRegistrationModel(candidate, false);
                }
            }
            return null;
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
            var registration = OpcUaTwinRegistration.FromTwin(twin, true,
                out var tmp);

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
            if (request.ApplicationName != null) {
                patched.Server.ApplicationName = string.IsNullOrEmpty(
                    request.ApplicationName) ? null : request.ApplicationName;
            }
            var isEnabled = (patched.Endpoint.IsTrusted ?? false);
            var enable = (request.IsTrusted ?? isEnabled);
            patched.Endpoint.IsTrusted = request.IsTrusted;

            // Patch
            await _registry.CreateOrUpdateAsync(registration.Patch(patched));

            // Enable/disable twin if needed
            if (isEnabled != enable) {
                await EnableTwinAsync(registration.SupervisorId, twin.Id, !enable);
            }
        }

        /// <summary>
        /// Delete twin
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DeleteTwinAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            var twin = await _registry.GetAsync(id);
            // Now we need to update any supervisor registration
            var existingEndpoint = OpcUaTwinRegistration.FromTwin(twin, false,
                out var tmp);
            if (!string.IsNullOrEmpty(existingEndpoint.SupervisorId)) {
                await _registry.UpdatePropertyAsync(existingEndpoint.SupervisorId,
                    id, null);
            }
            await _registry.DeleteAsync(id);
        }

        /// <summary>
        /// List all servers with endpoints == twins
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public async Task<ServerInfoListModel> ListServerInfosAsync(string continuation) {
            var tags = "tags.ApplicationId, tags.SupervisorId, tags.ServerCertificate";
            var query = $"SELECT {tags}, COUNT() FROM devices GROUP BY {tags}";
            var result = await _registry.QueryRawAsync(query, continuation);
            var items = JsonConvertEx.DeserializeObject<List<OpcUaTwinRegistration>>(
                result.Item2);
            return new ServerInfoListModel {
                ContinuationToken = result.Item1,
                Items = items
                    .Where(s => s.ApplicationId != null)
                    .Select(s => s.ToServiceModel().Server)
                    .ToList()
            };
        }

        /// <summary>
        /// Get full server model for specified server
        /// </summary>
        /// <param name="serverId"></param>
        /// <returns></returns>
        public async Task<ServerModel> GetServerAsync(string serverId) {
            if (string.IsNullOrEmpty(serverId)) {
                throw new ArgumentNullException(nameof(serverId));
            }
            var query = $"SELECT * FROM devices WHERE tags.ServerId = '{serverId}'";
            var result = await _registry.QueryAsync(query);
            var endpoints = result
                .Select(t => OpcUaTwinRegistration.FromTwin(t))
                .Select(s => s.ToServiceModel());
            if (!endpoints.Any()) {
                throw new ResourceNotFoundException("Server not found");
            }
            return new ServerModel {
                Server = endpoints.First().Server,
                Endpoints = endpoints.Select(s => s.Endpoint).ToList()
            };
        }

        /// <summary>
        /// Read full server model for specified server
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ServerModel> FindServerAsync(ServerInfoModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            if (string.IsNullOrEmpty(model.ApplicationUri)) {
                throw new ArgumentNullException(nameof(model.ApplicationUri));
            }
            var query = "SELECT * FROM devices WHERE " +
                $"tags.ApplicationId = '{model.ApplicationUri.ToLowerInvariant()}' ";

            if (model.ApplicationName != null) {
                // If application name provided, include it in search
                query += $"AND tags.ApplicationName = '{model.ApplicationName}' ";
            }
            if (model.ServerCertificate != null) {
                // If server cerfificate provided, include it in search
                query += $"AND tags.Thumbprint = '{model.ServerCertificate.ToSha1Hash()}' ";
            }
            if (!string.IsNullOrEmpty(model.SupervisorId)) {
                // If supervisor id provided, include it in search
                query += $"AND tags.SupervisorId = '{model.SupervisorId}' ";
            }
            var result = await _registry.QueryAsync(query);
            var endpoints = result
                .Select(t => OpcUaTwinRegistration.FromTwin(t))
                .Select(s => s.ToServiceModel());
            if (!endpoints.Any()) {
                throw new ResourceNotFoundException("Server not found");
            }
            return new ServerModel {
                Server = endpoints.First().Server,
                Endpoints = endpoints.Select(s => s.Endpoint).ToList()
            };
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
            // Remove from supervisor - this disconnects the device
            if (disable) {
                await _registry.UpdatePropertyAsync(supervisorId, twinId, null);
            }
            // Enable
            else {
                var device = await _registry.GetRegistrationAsync(twinId);
                // Update supervisor to start supervising this endpoint
                await _registry.UpdatePropertyAsync(supervisorId, device.Id,
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
        private static TwinRegistrationModel TwinModelToTwinRegistrationModel(
            TwinModel twin, bool onlyServerState) {
            var registration = OpcUaTwinRegistration.FromTwin(twin, onlyServerState,
                out var connected);
            if (registration == null) {
                throw new ResourceNotFoundException(
                    $"{twin.Id} is not a registered opc ua twin");
            }
            var model = registration.ToServiceModel();
            return new TwinRegistrationModel {
                Endpoint = model.Endpoint,
                Server = model.Server,
                Id = twin.Id,
                OutOfSync = connected && !registration.IsInSync() ? true : (bool?)null,
                Connected = connected
            };
        }

        private const string k_endpointProperty = "endpoint";
        private readonly IIoTHubTwinServices _registry;
        private readonly IOpcUaEndpointValidator _validator;
        private readonly ILogger _logger;
    }
}
