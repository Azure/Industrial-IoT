// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Registry services adapter to run dependent services outside of cloud.
    /// </summary>
    public sealed class RegistryServicesApiAdapter : IEndpointRegistry, ISupervisorRegistry,
        IApplicationRegistry, IPublisherRegistry, IDiscoveryServices, ISupervisorDiagnostics {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        public RegistryServicesApiAdapter(IRegistryServiceApi client, ISerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoModel> GetEndpointAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            var result = await _client.GetEndpointAsync(id,
                onlyServerState, ct);
            return _serializer.Map<EndpointInfoModel>(result);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var result = await _client.ListEndpointsAsync(continuation,
                onlyServerState, pageSize, ct);
            return _serializer.Map<EndpointInfoListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query, bool onlyServerState,
            int? pageSize, CancellationToken ct) {
            var result = await _client.QueryEndpointsAsync(
                _serializer.Map<EndpointRegistrationQueryApiModel>(query),
                onlyServerState, pageSize, ct);
            return _serializer.Map<EndpointInfoListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string id, CancellationToken ct) {
            var result = await _client.GetEndpointCertificateAsync(id, ct);
            return _serializer.Map<X509CertificateChainModel>(result);
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
            return _serializer.Map<SupervisorListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool onlyServerState, int? pageSize,
            CancellationToken ct) {
            var result = await _client.QuerySupervisorsAsync(
                _serializer.Map<SupervisorQueryApiModel>(query),
                onlyServerState, pageSize, ct);
            return _serializer.Map<SupervisorListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            var result = await _client.GetSupervisorAsync(id,
                onlyServerState, ct);
            return _serializer.Map<SupervisorModel>(result);
        }

        /// <inheritdoc/>
        public async Task<SupervisorStatusModel> GetSupervisorStatusAsync(string id,
            CancellationToken ct) {
            var result = await _client.GetSupervisorStatusAsync(id, ct);
            return _serializer.Map<SupervisorStatusModel>(result);
        }

        /// <inheritdoc/>
        public Task ResetSupervisorAsync(string id, CancellationToken ct) {
            return _client.ResetSupervisorAsync(id, ct);
        }

        /// <inheritdoc/>
        public Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request, CancellationToken ct) {
            return _client.UpdateSupervisorAsync(supervisorId,
                _serializer.Map<SupervisorUpdateApiModel>(request), ct);
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> ListPublishersAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct) {
            var result = await _client.ListPublishersAsync(continuation,
                onlyServerState, pageSize, ct);
            return _serializer.Map<PublisherListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> QueryPublishersAsync(
            PublisherQueryModel query, bool onlyServerState, int? pageSize,
            CancellationToken ct) {
            var result = await _client.QueryPublishersAsync(
                _serializer.Map<PublisherQueryApiModel>(query),
                onlyServerState, pageSize, ct);
            return _serializer.Map<PublisherListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<PublisherModel> GetPublisherAsync(string id,
            bool onlyServerState, CancellationToken ct) {
            var result = await _client.GetPublisherAsync(id,
                onlyServerState, ct);
            return _serializer.Map<PublisherModel>(result);
        }

        /// <inheritdoc/>
        public async Task UpdatePublisherAsync(string id, PublisherUpdateModel request,
            CancellationToken ct) {
            await _client.UpdatePublisherAsync(id,
                _serializer.Map<PublisherUpdateApiModel>(request), ct);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResultModel> RegisterApplicationAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct) {
            var result = await _client.RegisterAsync(
                _serializer.Map<ApplicationRegistrationRequestApiModel>(request), ct);
            return _serializer.Map<ApplicationRegistrationResultModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveTwins, CancellationToken ct) {
            var result = await _client.GetApplicationAsync(applicationId
                /* TODO ,filterInactiveTwins */, ct);
            return _serializer.Map<ApplicationRegistrationModel>(result);
        }

        /// <inheritdoc/>
        public Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct) {
            return _client.UpdateApplicationAsync(applicationId,
                _serializer.Map<ApplicationRegistrationUpdateApiModel>(request), ct);
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var result = await _client.ListSitesAsync(continuation, pageSize, ct);
            return _serializer.Map<ApplicationSiteListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var result = await _client.ListApplicationsAsync(continuation, pageSize, ct);
            return _serializer.Map<ApplicationInfoListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize, CancellationToken ct) {
            var result = await _client.QueryApplicationsAsync(
                _serializer.Map<ApplicationRegistrationQueryApiModel>(query), pageSize, ct);
            return _serializer.Map<ApplicationInfoListModel>(result);
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
            return _client.DiscoverAsync(
                _serializer.Map<DiscoveryRequestApiModel>(request), ct);
        }

        /// <inheritdoc/>
        public Task CancelAsync(DiscoveryCancelModel request, CancellationToken ct) {
            return _client.CancelAsync(
                _serializer.Map<DiscoveryCancelApiModel>(request), ct);
        }

        private readonly ISerializer _serializer;
        private readonly IRegistryServiceApi _client;
    }
}
