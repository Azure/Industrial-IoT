// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
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
        public RegistryAdapter(IRegistryServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoModel> GetEndpointAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            var result = await _client.GetEndpointAsync(id,
                onlyServerState, ct);
            return Map<EndpointInfoModel>(result);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var result = await _client.ListEndpointsAsync(continuation,
                onlyServerState, pageSize, ct);
            return Map<EndpointInfoListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query, bool onlyServerState,
            int? pageSize, CancellationToken ct) {
            var result = await _client.QueryEndpointsAsync(
                Map<EndpointRegistrationQueryApiModel>(query),
                onlyServerState, pageSize, ct);
            return Map<EndpointInfoListModel>(result);
        }

        /// <inheritdoc/>
        public Task ActivateEndpointAsync(string id, RegistryOperationContextModel context,
            CancellationToken ct) {
            return _client.ActivateEndpointAsync(id, ct);
        }

        /// <inheritdoc/>
        public Task UpdateEndpointAsync(string endpointId,
            EndpointRegistrationUpdateModel request, CancellationToken ct) {
            return _client.UpdateEndpointAsync(endpointId,
                Map<EndpointRegistrationUpdateApiModel>(request), ct);
        }

        /// <inheritdoc/>
        public Task DeactivateEndpointAsync(string id,
            RegistryOperationContextModel context, CancellationToken ct) {
            return _client.DeactivateEndpointAsync(id, ct);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var result = await _client.ListSupervisorsAsync(continuation,
                onlyServerState, pageSize, ct);
            return Map<SupervisorListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool onlyServerState, int? pageSize,
            CancellationToken ct) {
            var result = await _client.QuerySupervisorsAsync(
                Map<SupervisorQueryApiModel>(query),
                onlyServerState, pageSize, ct);
            return Map<SupervisorListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            var result = await _client.GetSupervisorAsync(id,
                onlyServerState, ct);
            return Map<SupervisorModel>(result);
        }

        /// <inheritdoc/>
        public async Task<SupervisorStatusModel> GetSupervisorStatusAsync(string id,
            CancellationToken ct) {
            var result = await _client.GetSupervisorStatusAsync(id, ct);
            return Map<SupervisorStatusModel>(result);
        }

        /// <inheritdoc/>
        public Task ResetSupervisorAsync(string id, CancellationToken ct) {
            return _client.ResetSupervisorAsync(id, ct);
        }

        /// <inheritdoc/>
        public Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request, CancellationToken ct) {
            return _client.UpdateSupervisorAsync(supervisorId,
                Map<SupervisorUpdateApiModel>(request), ct);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResultModel> RegisterApplicationAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct) {
            var result = await _client.RegisterAsync(
                Map<ApplicationRegistrationRequestApiModel>(request), ct);
            return Map<ApplicationRegistrationResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveTwins, CancellationToken ct) {
            var result = await _client.GetApplicationAsync(applicationId
                /* TODO ,filterInactiveTwins */, ct);
            return Map<ApplicationRegistrationModel>(result);
        }

        /// <inheritdoc/>
        public Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct) {
            return _client.UpdateApplicationAsync(applicationId,
                Map<ApplicationRegistrationUpdateApiModel>(request), ct);
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var result = await _client.ListSitesAsync(continuation, pageSize, ct);
            return Map<ApplicationSiteListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var result = await _client.ListApplicationsAsync(continuation, pageSize, ct);
            return Map<ApplicationInfoListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize, CancellationToken ct) {
            var result = await _client.QueryApplicationsAsync(
                Map<ApplicationRegistrationQueryApiModel>(query), pageSize, ct);
            return Map<ApplicationInfoListModel>(result);
        }

        /// <inheritdoc/>
        public Task DisableApplicationAsync(string applicationId,
            RegistryOperationContextModel context, CancellationToken ct) {
            return _client.DisableApplicationAsync(applicationId, ct);
        }

        /// <inheritdoc/>
        public Task EnableApplicationAsync(string applicationId,
            RegistryOperationContextModel context, CancellationToken ct) {
            return _client.EnableApplicationAsync(applicationId, ct);
        }

        /// <inheritdoc/>
        public Task UnregisterApplicationAsync(string applicationId,
            RegistryOperationContextModel context, CancellationToken ct) {
            return _client.UnregisterApplicationAsync(applicationId, ct);
        }

        /// <inheritdoc/>
        public Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor,
            RegistryOperationContextModel context, CancellationToken ct) {
            return _client.PurgeDisabledApplicationsAsync(notSeenFor, ct);
        }

        /// <inheritdoc/>
        public Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct) {
            return _client.DiscoverAsync(Map<DiscoveryRequestApiModel>(request), ct);
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
    }
}
