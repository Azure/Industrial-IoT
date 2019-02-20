// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Registry services adapter to run dependent services outside of cloud.
    /// </summary>
    public sealed class RegistryAdapter : IEndpointRegistry, ISupervisorRegistry,
        IApplicationRegistry, IDiscoveryServices, ISupervisorDiagnostics {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public RegistryAdapter(IRegistryServiceApi client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoModel> GetEndpointAsync(string id,
            bool onlyServerState) {
            var result = await _client.GetEndpointAsync(id,
                onlyServerState);
            return Map<EndpointInfoModel>(result);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(
            string continuation, bool onlyServerState, int? pageSize) {
            var result = await _client.ListEndpointsAsync(continuation,
                onlyServerState, pageSize);
            return Map<EndpointInfoListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query, bool onlyServerState,
            int? pageSize) {
            var result = await _client.QueryEndpointsAsync(
                Map<EndpointRegistrationQueryApiModel>(query),
                onlyServerState, pageSize);
            return Map<EndpointInfoListModel>(result);
        }

        /// <inheritdoc/>
        public Task ActivateEndpointAsync(string id) {
            return _client.ActivateEndpointAsync(id);
        }

        /// <inheritdoc/>
        public Task UpdateEndpointAsync(string endpointId,
            EndpointRegistrationUpdateModel request) {
            return _client.UpdateEndpointAsync(endpointId,
                Map<EndpointRegistrationUpdateApiModel>(request));
        }

        /// <inheritdoc/>
        public Task DeactivateEndpointAsync(string id) {
            return _client.DeactivateEndpointAsync(id);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string continuation, bool onlyServerState, int? pageSize) {
            var result = await _client.ListSupervisorsAsync(continuation,
                onlyServerState, pageSize);
            return Map<SupervisorListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool onlyServerState, int? pageSize ) {
            var result = await _client.QuerySupervisorsAsync(
                Map<SupervisorQueryApiModel>(query),
                onlyServerState, pageSize);
            return Map<SupervisorListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string id,
            bool onlyServerState) {
            var result = await _client.GetSupervisorAsync(id,
                onlyServerState);
            return Map<SupervisorModel>(result);
        }

        /// <inheritdoc/>
        public async Task<SupervisorStatusModel> GetSupervisorStatusAsync(string id) {
            var result = await _client.GetSupervisorStatusAsync(id);
            return Map<SupervisorStatusModel>(result);
        }

        /// <inheritdoc/>
        public Task ResetSupervisorAsync(string id) {
            return _client.ResetSupervisorAsync(id);
        }

        /// <inheritdoc/>
        public Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request) {
            return _client.UpdateSupervisorAsync(supervisorId,
                Map<SupervisorUpdateApiModel>(request));
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResultModel> RegisterAsync(
            ApplicationRegistrationRequestModel request) {
            var result = await _client.RegisterAsync(
                Map<ApplicationRegistrationRequestApiModel>(request));
            return Map<ApplicationRegistrationResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveTwins) {
            var result = await _client.GetApplicationAsync(applicationId
                /* TODO ,filterInactiveTwins */ );
            return Map<ApplicationRegistrationModel>(result);
        }

        /// <inheritdoc/>
        public Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request) {
            return _client.UpdateApplicationAsync(applicationId,
                Map<ApplicationRegistrationUpdateApiModel>(request));
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize) {
            var result = await _client.ListSitesAsync(continuation, pageSize);
            return Map<ApplicationSiteListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize) {
            var result = await _client.ListApplicationsAsync(continuation, pageSize);
            return Map<ApplicationInfoListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize) {
            var result = await _client.QueryApplicationsAsync(
                Map<ApplicationRegistrationQueryApiModel>(query), pageSize);
            return Map<ApplicationInfoListModel>(result);
        }

        /// <inheritdoc/>
        public Task UnregisterApplicationAsync(string applicationId) {
            return _client.UnregisterApplicationAsync(applicationId);
        }

        /// <inheritdoc/>
        public Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor) {
            return _client.PurgeDisabledApplicationsAsync(notSeenFor);
        }

        /// <inheritdoc/>
        public Task DiscoverAsync(DiscoveryRequestModel request) {
            return _client.DiscoverAsync(Map<DiscoveryRequestApiModel>(request));
        }

        /// <summary>
        /// Convert from to
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        private static T Map<T>(object model) {
            return JsonConvertEx.DeserializeObject<T>(
                JsonConvertEx.SerializeObject(model));
        }

        private readonly IRegistryServiceApi _client;
        private readonly ILogger _logger;
    }
}
