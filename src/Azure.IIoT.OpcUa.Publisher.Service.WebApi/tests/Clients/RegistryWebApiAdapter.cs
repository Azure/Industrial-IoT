// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Service;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Google.Api;

    /// <summary>
    /// Registry services adapter to run dependent services outside of cloud.
    /// </summary>
    public sealed class RegistryWebApiAdapter : IEndpointRegistry, ISupervisorRegistry,
        IApplicationRegistry, IPublisherRegistry, INetworkDiscovery<string>, IEndpointManager<string>
    {
        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="client"></param>
        public RegistryWebApiAdapter(IRegistryServiceApi client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<string> RegisterEndpointAsync(ServerEndpointQueryModel query,
            string context, CancellationToken ct)
        {
            return await _client.RegisterEndpointAsync(query, context,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            bool onlyServerState, CancellationToken ct)
        {
            return await _client.GetEndpointAsync(endpointId, onlyServerState,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            return await _client.ListEndpointsAsync(continuation,
                onlyServerState, pageSize, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query, bool onlyServerState,
            int? pageSize, CancellationToken ct)
        {
            return await _client.QueryEndpointsAsync(query,
                onlyServerState, pageSize, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            return await _client.ListSupervisorsAsync(continuation,
                onlyServerState, pageSize, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool onlyServerState, int? pageSize,
            CancellationToken ct)
        {
            return await _client.QuerySupervisorsAsync(query,
                onlyServerState, pageSize, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(string supervisorId,
            bool onlyServerState, CancellationToken ct)
        {
            return await _client.GetSupervisorAsync(supervisorId, onlyServerState,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request, CancellationToken ct)
        {
            return _client.UpdateSupervisorAsync(supervisorId, request, ct);
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> ListPublishersAsync(
            string continuation, bool onlyServerState, int? pageSize, CancellationToken ct)
        {
            return await _client.ListPublishersAsync(continuation,
                onlyServerState, pageSize, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> QueryPublishersAsync(
            PublisherQueryModel query, bool onlyServerState, int? pageSize,
            CancellationToken ct)
        {
            return await _client.QueryPublishersAsync(query,
                onlyServerState, pageSize, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublisherModel> GetPublisherAsync(string publisherId,
            bool onlyServerState, CancellationToken ct)
        {
            return await _client.GetPublisherAsync(publisherId, onlyServerState,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdatePublisherAsync(string publisherId, PublisherUpdateModel request,
            CancellationToken ct)
        {
            await _client.UpdatePublisherAsync(publisherId, request,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResponseModel> RegisterApplicationAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct)
        {
            return await _client.RegisterAsync(request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, bool filterInactiveEndpoints, CancellationToken ct)
        {
            return await _client.GetApplicationAsync(applicationId
                /* TODO ,filterInactiveTwins */, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct)
        {
            return _client.UpdateApplicationAsync(applicationId, request, ct);
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct)
        {
            return await _client.ListSitesAsync(continuation, pageSize,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize, CancellationToken ct)
        {
            return await _client.ListApplicationsAsync(continuation, pageSize,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize, CancellationToken ct)
        {
            return await _client.QueryApplicationsAsync(query, pageSize,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task DisableApplicationAsync(string applicationId,
            OperationContextModel context, CancellationToken ct)
        {
            return _client.DisableApplicationAsync(applicationId, ct);
        }

        /// <inheritdoc/>
        public Task EnableApplicationAsync(string applicationId,
            OperationContextModel context, CancellationToken ct)
        {
            return _client.EnableApplicationAsync(applicationId, ct);
        }

        /// <inheritdoc/>
        public Task UnregisterApplicationAsync(string applicationId,
            OperationContextModel context, CancellationToken ct)
        {
            return _client.UnregisterApplicationAsync(applicationId, ct);
        }

        /// <inheritdoc/>
        public Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor,
            OperationContextModel context, CancellationToken ct)
        {
            return _client.PurgeDisabledApplicationsAsync(notSeenFor, ct);
        }

        /// <inheritdoc/>
        public Task DiscoverAsync(DiscoveryRequestModel request,
            string context, CancellationToken ct)
        {
            return _client.DiscoverAsync(request, context, ct);
        }

        /// <inheritdoc/>
        public Task CancelAsync(DiscoveryCancelRequestModel request,
            string context, CancellationToken ct)
        {
            return _client.CancelAsync(request, context, ct);
        }

        /// <inheritdoc/>
        public Task RegisterAsync(ServerRegistrationRequestModel request,
            string context, CancellationToken ct = default)
        {
            return _client.RegisterAsync(request, context, ct);
        }

        private readonly IRegistryServiceApi _client;
    }
}
