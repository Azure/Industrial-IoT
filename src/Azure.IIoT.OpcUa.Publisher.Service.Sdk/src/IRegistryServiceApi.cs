// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry api calls
    /// </summary>
    public interface IRegistryServiceApi
    {
        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> GetServiceStatusAsync(CancellationToken ct = default);

        /// <summary>
        /// Kick off onboarding of new server
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RegisterAsync(ServerRegistrationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Kick off a one time discovery on all supervisors
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Cancel a discovery request with a particular id
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CancelAsync(DiscoveryCancelRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Register new application.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationResponseModel> RegisterAsync(
            ApplicationRegistrationRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Get application for specified unique application id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, CancellationToken ct = default);

        /// <summary>
        /// Update an application' properties.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request,
            CancellationToken ct = default);

        /// <summary>
        /// List all Application sites to visually group applications.
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationSiteListModel> ListSitesAsync(
            string? continuation = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// List all applications or continue a QueryApplications
        /// call.
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> ListApplicationsAsync(
            string? continuation = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Find applications based on specified criteria. Pass
        /// continuation token if any returned to ListApplications to
        /// retrieve remaining items.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Enable the application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task EnableApplicationAsync(string applicationId,
            CancellationToken ct = default);

        /// <summary>
        /// Disable the application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DisableApplicationAsync(string applicationId,
            CancellationToken ct = default);

        /// <summary>
        /// Unregister and delete application and all endpoints.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UnregisterApplicationAsync(string applicationId,
            CancellationToken ct = default);

        /// <summary>
        /// Unregister disabled applications not seen since specified
        /// amount of time.
        /// </summary>
        /// <param name="notSeenSince"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task PurgeDisabledApplicationsAsync(TimeSpan notSeenSince,
            CancellationToken ct = default);

        /// <summary>
        /// Find the endpoint and server application information that
        /// matches the endpoint query and register it in the registry.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns>New Endpoint identifier</returns>
        Task<string> RegisterEndpointAsync(ServerEndpointQueryModel query,
            CancellationToken ct = default);

        /// <summary>
        /// Get endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            bool? onlyServerState = null, CancellationToken ct = default);

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string endpointId, CancellationToken ct = default);

        /// <summary>
        /// Connect the endpoint on the module side.
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ConnectResponseModel> ConnectAsync(string endpointId,
            ConnectRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Test connection by opening a session to the server.
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TestConnectionResponseModel> TestConnectionAsync(string endpointId,
            TestConnectionRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Disconnect the session for the endpoint.
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DisconnectAsync(string endpointId,
            DisconnectRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// List all endpoints
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EndpointInfoListModel> ListEndpointsAsync(
            string? continuation = null, bool? onlyServerState = null,
            int? pageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Find endpoint based on specified criteria. Pass continuation
        /// token if any is returned to ListEndpointsAsync to retrieve
        /// the remaining items
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query,
            bool? onlyServerState = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SupervisorModel> GetSupervisorAsync(
            string supervisorId, bool? onlyServerState = null,
            CancellationToken ct = default);

        /// <summary>
        /// Update supervisor including config updates.
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request,
            CancellationToken ct = default);

        /// <summary>
        /// List all supervisors
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SupervisorListModel> ListSupervisorsAsync(
            string? continuation = null, bool? onlyServerState = null,
            int? pageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Find supervisors based on specified criteria. Pass
        /// continuation token if any returned to ListSupervisors to
        /// retrieve remaining items.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool? onlyServerState = null,
            int? pageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Get discoverer
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DiscovererModel> GetDiscovererAsync(
            string discovererId, CancellationToken ct = default);

        /// <summary>
        /// Update discoverer including config updates.
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateDiscovererAsync(string discovererId,
            DiscovererUpdateModel request,
            CancellationToken ct = default);

        /// <summary>
        /// List all discoverers
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DiscovererListModel> ListDiscoverersAsync(
            string? continuation = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Find discoverers based on specified criteria. Pass
        /// continuation token if any returned to ListDiscoverers to
        /// retrieve remaining items.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DiscovererListModel> QueryDiscoverersAsync(
            DiscovererQueryModel query,
            int? pageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Get gateway
        /// </summary>
        /// <param name="publisherId"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublisherModel> GetPublisherAsync(
            string publisherId, bool? onlyServerState = null,
            CancellationToken ct = default);

        /// <summary>
        /// Set configured endpoints on the publisher
        /// </summary>
        /// <param name="publisherId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SetConfiguredEndpointsAsync(
            string publisherId, SetConfiguredEndpointsRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Get configured endpoints on the publisher
        /// </summary>
        /// <param name="publisherId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<PublishedNodesEntryModel> GetConfiguredEndpointsAsync(
            string publisherId, GetConfiguredEndpointsRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Update Publisher including config updates.
        /// </summary>
        /// <param name="publisherId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdatePublisherAsync(string publisherId,
            PublisherUpdateModel request,
            CancellationToken ct = default);

        /// <summary>
        /// List all gateways
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublisherListModel> ListPublishersAsync(
            string? continuation = null, bool? onlyServerState = null,
            int? pageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Find gateways based on specified criteria. Pass
        /// continuation token if any returned to ListPublishers to
        /// retrieve remaining items.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublisherListModel> QueryPublishersAsync(
            PublisherQueryModel query, bool? onlyServerState = null,
            int? pageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Get gateway
        /// </summary>
        /// <param name="gatewayId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<GatewayInfoModel> GetGatewayAsync(
            string gatewayId, CancellationToken ct = default);

        /// <summary>
        /// Update Gateway including config updates.
        /// </summary>
        /// <param name="gatewayId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateGatewayAsync(string gatewayId,
            GatewayUpdateModel request,
            CancellationToken ct = default);

        /// <summary>
        /// List all gateways
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<GatewayListModel> ListGatewaysAsync(
            string? continuation = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Find gateways based on specified criteria. Pass
        /// continuation token if any returned to ListGateways to
        /// retrieve remaining items.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<GatewayListModel> QueryGatewaysAsync(
            GatewayQueryModel query, int? pageSize = null,
            CancellationToken ct = default);
    }
}
