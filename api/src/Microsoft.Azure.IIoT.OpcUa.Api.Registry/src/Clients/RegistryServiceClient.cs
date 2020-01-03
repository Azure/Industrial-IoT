// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Implementation of registry service api.
    /// </summary>
    public sealed class RegistryServiceClient : IRegistryServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        public RegistryServiceClient(IHttpClient httpClient, IRegistryConfig config) :
            this(httpClient, config.OpcUaRegistryServiceUrl,
                config.OpcUaRegistryServiceResourceId) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        public RegistryServiceClient(IHttpClient httpClient, string serviceUri,
            string resourceId) {
            if (string.IsNullOrEmpty(serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the registry micro service.");
            }
            _serviceUri = serviceUri;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _resourceId = resourceId;
        }

        /// <inheritdoc/>
        public async Task<StatusResponseApiModel> GetServiceStatusAsync(CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/status", _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<StatusResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task SubscribeDiscoveryProgressBySupervisorsIdAsync(string supervisorId,
            string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/{supervisorId}/events", _resourceId);
            request.SetContent<string>(userId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task SubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/requests/{requestId}/events", _resourceId);
            request.SetContent<string>(userId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task SetDiscoveryModeAsync(string supervisorId,
            DiscoveryMode mode, DiscoveryConfigApiModel config, CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/discovery/{supervisorId}") {
                Query = $"mode={mode}"
            };
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            request.SetContent(config);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribeDiscoveryProgressBySupervisorsIdAsync(string supervisorId,
            string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/{supervisorId}/events/{userId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/requests/{requestId}/events/{userId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<SupervisorStatusApiModel> GetSupervisorStatusAsync(
            string supervisorId, CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/{supervisorId}/status");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<SupervisorStatusApiModel>();
        }

        /// <inheritdoc/>
        public async Task ResetSupervisorAsync(string supervisorId, CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/{supervisorId}/reset");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateApiModel content, CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/supervisors/{supervisorId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PatchAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<SupervisorListApiModel> ListSupervisorsAsync(
            string continuation, bool? onlyServerState, int? pageSize, CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<SupervisorListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<SupervisorListApiModel> QuerySupervisorsAsync(
            SupervisorQueryApiModel query, bool? onlyServerState, int? pageSize,
            CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/query");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<SupervisorListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<SupervisorApiModel> GetSupervisorAsync(
            string supervisorId, bool? onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/{supervisorId}");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<SupervisorApiModel>();
        }

        /// <inheritdoc/>
        public async Task SubscribeSupervisorEventsAsync(string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/supervisors/events", _resourceId);
            request.SetContent<string>(userId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribeSupervisorEventsAsync(string userId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/supervisors/events/{userId}", _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestApiModel content,
            CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.DiscoveryUrl == null) {
                throw new ArgumentNullException(nameof(content.DiscoveryUrl));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications",
                _resourceId);
            request.SetContent(content);
            request.Options.Timeout = 60000;
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestApiModel content, CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/discover",
                _resourceId);
            request.SetContent(content);
            request.Options.Timeout = 60000;
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelApiModel content, CancellationToken ct) {
            if (content?.Id == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/applications/discover/${content.Id}", _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResponseApiModel> RegisterAsync(
            ApplicationRegistrationRequestApiModel content, CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.ApplicationUri)) {
                throw new ArgumentNullException(nameof(content.ApplicationUri));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ApplicationRegistrationResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task EnableApplicationAsync(string applicationId, CancellationToken ct) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = $"{_serviceUri}/v2/applications/{applicationId}/enable";
            var request = _httpClient.NewRequest(uri);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task DisableApplicationAsync(string applicationId, CancellationToken ct) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = $"{_serviceUri}/v2/applications/{applicationId}/disable";
            var request = _httpClient.NewRequest(uri);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateApiModel content, CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/{applicationId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PatchAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationApiModel> GetApplicationAsync(
            string applicationId, CancellationToken ct) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/{applicationId}",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ApplicationRegistrationApiModel>();
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListApiModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryApiModel query, int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/query",
                _resourceId);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ApplicationInfoListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListApiModel> ListApplicationsAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications",
                _resourceId);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ApplicationInfoListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListApiModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/sites",
                _resourceId);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<ApplicationSiteListApiModel>();
        }

        /// <inheritdoc/>
        public async Task UnregisterApplicationAsync(string applicationId, CancellationToken ct) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/{applicationId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor,
            CancellationToken ct) {
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/applications?notSeenFor={notSeenFor}", _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task SubscribeApplicationEventsAsync(string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/applications/events", _resourceId);
            request.SetContent<string>(userId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribeApplicationEventsAsync(string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/applications/events/{userId}", _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListApiModel> ListEndpointsAsync(string continuation,
            bool? onlyServerState, int? pageSize, CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<EndpointInfoListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListApiModel> QueryEndpointsAsync(
            EndpointRegistrationQueryApiModel query, bool? onlyServerState, int? pageSize,
            CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints/query");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<EndpointInfoListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoApiModel> GetEndpointAsync(string endpointId,
            bool? onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints/{endpointId}");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<EndpointInfoApiModel>();
        }

        /// <inheritdoc/>
        public async Task ActivateEndpointAsync(string endpointId, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/endpoints/{endpointId}/activate", _resourceId);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task DeactivateEndpointAsync(string endpointId, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/endpoints/{endpointId}/deactivate", _resourceId);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task SubscribeEndpointEventsAsync(string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/endpoints/events", _resourceId);
            request.SetContent<string>(userId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribeEndpointEventsAsync(string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/endpoints/events/{userId}", _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<PublisherListApiModel> ListPublishersAsync(
            string continuation, bool? onlyServerState, int? pageSize, CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/publishers");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<PublisherListApiModel>();
        }

        /// <inheritdoc/>
        public async Task UpdatePublisherAsync(string publisherId,
            PublisherUpdateApiModel content, CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(publisherId)) {
                throw new ArgumentNullException(nameof(publisherId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/publishers/{publisherId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PatchAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<PublisherListApiModel> QueryPublishersAsync(
            PublisherQueryApiModel query, bool? onlyServerState, int? pageSize,
            CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/publishers/query");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<PublisherListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<PublisherApiModel> GetPublisherAsync(
            string publisherId, bool? onlyServerState, CancellationToken ct) {
            if (string.IsNullOrEmpty(publisherId)) {
                throw new ArgumentNullException(nameof(publisherId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/publishers/{publisherId}");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<PublisherApiModel>();
        }

        /// <inheritdoc/>
        public async Task SubscribePublisherEventsAsync(string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/publishers/events", _resourceId);
            request.SetContent<string>(userId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribePublisherEventsAsync(string userId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/publishers/events/{userId}", _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<GatewayListApiModel> ListGatewaysAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/gateways");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<GatewayListApiModel>();
        }

        /// <inheritdoc/>
        public async Task UpdateGatewayAsync(string gatewayId,
            GatewayUpdateApiModel content, CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(gatewayId)) {
                throw new ArgumentNullException(nameof(gatewayId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/gateways/{gatewayId}",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PatchAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<GatewayListApiModel> QueryGatewaysAsync(
            GatewayQueryApiModel query, int? pageSize, CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/gateways/query");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<GatewayListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<GatewayApiModel> GetGatewayAsync(string gatewayId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(gatewayId)) {
                throw new ArgumentNullException(nameof(gatewayId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/gateways/{gatewayId}");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<GatewayApiModel>();
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly string _resourceId;
    }
}
