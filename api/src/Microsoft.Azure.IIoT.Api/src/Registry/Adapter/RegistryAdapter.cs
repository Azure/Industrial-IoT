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
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Registry services adapter to run dependent services outside of cloud.
    /// </summary>
    public sealed class RegistryAdapter : IEndpointRegistry, ISupervisorRegistry,
        IApplicationRegistry, IPublisherRegistry, IDiscoveryServices, ISupervisorDiagnostics {

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
            return result.Map<EndpointInfoModel>();
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var result = await _client.ListEndpointsAsync(continuation,
                onlyServerState, pageSize, ct);
            return result.Map<EndpointInfoListModel>();
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query, bool onlyServerState,
            int? pageSize, CancellationToken ct) {
            var result = await _client.QueryEndpointsAsync(
                query.Map<EndpointRegistrationQueryApiModel>(),
                onlyServerState, pageSize, ct);
            return result.Map<EndpointInfoListModel>();
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string id, CancellationToken ct) {
            var result = await _client.GetEndpointCertificateAsync(id, ct);
            return result.Map<X509CertificateChainModel>();
        }

        /// <inheritdoc/>
        public Task ActivateEndpointAsync(string id, RegistryOperationContextModel context,
            CancellationToken ct) {
            return _client.ActivateEndpointAsync(id, ct);
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
            return result.Map<SupervisorListModel>();
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool onlyServerState, int? pageSize,
            CancellationToken ct) {
            var result = await _client.QuerySupervisorsAsync(
                query.Map<SupervisorQueryApiModel>(),
                onlyServerState, pageSize, ct);
            return result.Map<SupervisorListModel>();
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            var result = await _client.GetSupervisorAsync(id,
                onlyServerState, ct);
            return result.Map<SupervisorModel>();
        }

        /// <inheritdoc/>
        public async Task<SupervisorStatusModel> GetSupervisorStatusAsync(string id,
            CancellationToken ct) {
            var result = await _client.GetSupervisorStatusAsync(id, ct);
            return result.Map<SupervisorStatusModel>();
        }

        /// <inheritdoc/>
        public Task ResetSupervisorAsync(string id, CancellationToken ct) {
            return _client.ResetSupervisorAsync(id, ct);
        }

        /// <inheritdoc/>
        public Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request, CancellationToken ct) {
            return _client.UpdateSupervisorAsync(supervisorId,
                request.Map<SupervisorUpdateApiModel>(), ct);
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> ListPublishersAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var result = await _client.ListPublishersAsync(continuation,
                onlyServerState, pageSize, ct);
            return result.Map<PublisherListModel>();
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> QueryPublishersAsync(
            PublisherQueryModel query, bool onlyServerState, int? pageSize,
            CancellationToken ct) {
            var result = await _client.QueryPublishersAsync(
                query.Map<PublisherQueryApiModel>(),
                onlyServerState, pageSize, ct);
            return result.Map<PublisherListModel>();
        }

        /// <inheritdoc/>
        public async Task<PublisherModel> GetPublisherAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            var result = await _client.GetPublisherAsync(id,
                onlyServerState, ct);
            return result.Map<PublisherModel>();
        }

        /// <inheritdoc/>
        public async Task UpdatePublisherAsync(string id, PublisherUpdateModel request,
            CancellationToken ct) {
            await _client.UpdatePublisherAsync(id,
                request.Map<PublisherUpdateApiModel>(), ct);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResultModel> RegisterApplicationAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct) {
            var result = await _client.RegisterAsync(
                request.Map<ApplicationRegistrationRequestApiModel>(), ct);
            return result.Map<ApplicationRegistrationResultModel>();
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveTwins, CancellationToken ct) {
            var result = await _client.GetApplicationAsync(applicationId
                /* TODO ,filterInactiveTwins */, ct);
            return result.Map<ApplicationRegistrationModel>();
        }

        /// <inheritdoc/>
        public Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct) {
            return _client.UpdateApplicationAsync(applicationId,
                request.Map<ApplicationRegistrationUpdateApiModel>(), ct);
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var result = await _client.ListSitesAsync(continuation, pageSize, ct);
            return result.Map<ApplicationSiteListModel>();
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var result = await _client.ListApplicationsAsync(continuation, pageSize, ct);
            return result.Map<ApplicationInfoListModel>();
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize, CancellationToken ct) {
            var result = await _client.QueryApplicationsAsync(
                query.Map<ApplicationRegistrationQueryApiModel>(), pageSize, ct);
            return result.Map<ApplicationInfoListModel>();
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
            return _client.DiscoverAsync(request.Map<DiscoveryRequestApiModel>(), ct);
        }

        /// <inheritdoc/>
        public Task CancelAsync(DiscoveryCancelModel request, CancellationToken ct) {
            return _client.CancelAsync(request.Map<DiscoveryCancelApiModel>(), ct);
        }

        private readonly IRegistryServiceApi _client;
    }
}
