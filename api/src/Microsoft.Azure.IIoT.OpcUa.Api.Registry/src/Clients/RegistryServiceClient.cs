// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
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
        /// <param name="serializer"></param>
        public RegistryServiceClient(IHttpClient httpClient, IRegistryConfig config,
            ISerializer serializer) :
            this(httpClient, config?.OpcUaRegistryServiceUrl, serializer) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public RegistryServiceClient(IHttpClient httpClient, string serviceUri,
            ISerializer serializer = null) {
            if (string.IsNullOrWhiteSpace(serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the registry micro service.");
            }
            _serviceUri = serviceUri.TrimEnd('/');
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/healthz",
                Resource.Platform);
            try {
                var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
                response.Validate();
                return response.GetContentAsString();
            }
            catch (Exception ex) {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        public async Task UpdateDiscovererAsync(string discovererId,
            DiscovererUpdateApiModel content, CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/discovery/{discovererId}",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PatchAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<DiscovererListApiModel> ListDiscoverersAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/discovery");
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<DiscovererListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<DiscovererListApiModel> QueryDiscoverersAsync(
            DiscovererQueryApiModel query, int? pageSize,
            CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/discovery/query");
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<DiscovererListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<DiscovererApiModel> GetDiscovererAsync(
            string discovererId, CancellationToken ct) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/discovery/{discovererId}");
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<DiscovererApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task SetDiscoveryModeAsync(string discovererId,
            DiscoveryMode mode, DiscoveryConfigApiModel config, CancellationToken ct) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/discovery/{discovererId}") {
                Query = $"mode={mode}"
            };
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            _serializer.SerializeToRequest(request, config);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<SupervisorStatusApiModel> GetSupervisorStatusAsync(
            string supervisorId, CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/{supervisorId}/status");
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<SupervisorStatusApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task ResetSupervisorAsync(string supervisorId, CancellationToken ct) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/{supervisorId}/reset");
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
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
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
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
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<SupervisorListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListApiModel> QuerySupervisorsAsync(
            SupervisorQueryApiModel query, bool? onlyServerState, int? pageSize,
            CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/query");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<SupervisorListApiModel>(response);
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
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<SupervisorApiModel>(response);
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
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            if (request.Options.Timeout == null) {
                request.Options.Timeout = TimeSpan.FromMinutes(3);
            }
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestApiModel content, CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/discover",
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            if (request.Options.Timeout == null) {
                request.Options.Timeout = TimeSpan.FromMinutes(3);
            }
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelApiModel content, CancellationToken ct) {
            if (content?.Id == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/applications/discover/${content.Id}", Resource.Platform);
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
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ApplicationRegistrationResponseApiModel>(response);
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
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
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
                Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ApplicationRegistrationApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListApiModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryApiModel query, int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/query",
                Resource.Platform);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ApplicationInfoListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListApiModel> ListApplicationsAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications",
                Resource.Platform);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ApplicationInfoListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListApiModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/sites",
                Resource.Platform);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<ApplicationSiteListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task UnregisterApplicationAsync(string applicationId, CancellationToken ct) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/applications/{applicationId}",
                Resource.Platform);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor,
            CancellationToken ct) {
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/applications?notSeenFor={notSeenFor}", Resource.Platform);
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
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<EndpointInfoListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListApiModel> QueryEndpointsAsync(
            EndpointRegistrationQueryApiModel query, bool? onlyServerState, int? pageSize,
            CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints/query");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<EndpointInfoListApiModel>(response);
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
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<EndpointInfoApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainApiModel> GetEndpointCertificateAsync(
            string endpointId, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints/{endpointId}/certificate");
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<X509CertificateChainApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task ActivateEndpointAsync(string endpointId, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/endpoints/{endpointId}/activate", Resource.Platform);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task DeactivateEndpointAsync(string endpointId, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/endpoints/{endpointId}/deactivate", Resource.Platform);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<PublisherListApiModel> ListPublishersAsync(
            string continuation, bool? onlyServerState, int? pageSize, CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/publishers");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<PublisherListApiModel>(response);
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
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
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
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<PublisherListApiModel>(response);
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
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<PublisherApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<GatewayListApiModel> ListGatewaysAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/gateways");
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<GatewayListApiModel>(response);
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
                Resource.Platform);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PatchAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<GatewayListApiModel> QueryGatewaysAsync(
            GatewayQueryApiModel query, int? pageSize, CancellationToken ct) {
            var uri = new UriBuilder($"{_serviceUri}/v2/gateways/query");
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<GatewayListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<GatewayInfoApiModel> GetGatewayAsync(string gatewayId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(gatewayId)) {
                throw new ArgumentNullException(nameof(gatewayId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/gateways/{gatewayId}");
            var request = _httpClient.NewRequest(uri.Uri, Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<GatewayInfoApiModel>(response);
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly ISerializer _serializer;
    }
}
