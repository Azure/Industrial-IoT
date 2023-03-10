// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Http;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of registry service api.
    /// </summary>
    public sealed class RegistryServiceClient : IRegistryServiceApi
    {
        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public RegistryServiceClient(IHttpClientFactory httpClient, IServiceApiConfig config,
            ISerializer serializer) :
            this(httpClient, config?.ServiceUrl, serializer)
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public RegistryServiceClient(IHttpClientFactory httpClient, string serviceUri,
            ISerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the registry micro service.");
            }
            _serviceUri = serviceUri.TrimEnd('/') + "/registry";
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct)
        {
            var httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri($"{_serviceUri}/healthz")
            };
            try
            {
                using var response = await _httpClient.GetAsync(httpRequest,
                    ct).ConfigureAwait(false);
                response.ValidateResponse();
                return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        public async Task UpdateDiscovererAsync(string discovererId,
            DiscovererUpdateModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            var uri = new Uri($"{_serviceUri}/v2/discovery/{discovererId}");
            await _httpClient.PatchAsync(uri, request, _serializer,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<DiscovererListModel> ListDiscoverersAsync(
            string continuation, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/discovery");
            return await _httpClient.GetAsync<DiscovererListModel>(uri,
                _serializer, request =>
            {
                if (continuation != null)
                {
                    request.AddHeader(HttpHeader.ContinuationToken, continuation);
                }
                if (pageSize != null)
                {
                    request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                }
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<DiscovererListModel> QueryDiscoverersAsync(
            DiscovererQueryModel query, int? pageSize,
            CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/discovery/query");
            return await _httpClient.PostAsync<DiscovererListModel>(uri, query,
                _serializer, httpRequest =>
                {
                    if (pageSize != null)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<DiscovererModel> GetDiscovererAsync(string discovererId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            var uri = new Uri($"{_serviceUri}/v2/discovery/{discovererId}");
            return await _httpClient.GetAsync<DiscovererModel>(uri, _serializer,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task SetDiscoveryModeAsync(string discovererId,
            DiscoveryMode mode, DiscoveryConfigModel config, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/discovery/{discovererId}")
            {
                Query = $"mode={mode}"
            };
            await _httpClient.PostAsync(uri.Uri, config, _serializer,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(supervisorId))
            {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new Uri($"{_serviceUri}/v2/supervisors/{supervisorId}");
            await _httpClient.PatchAsync(uri, request, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string continuation, bool? onlyServerState, int? pageSize, CancellationToken ct)
        {
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.GetAsync<SupervisorListModel>(uri.Uri,
                _serializer, request =>
            {
                if (continuation != null)
                {
                    request.AddHeader(HttpHeader.ContinuationToken, continuation);
                }
                if (pageSize != null)
                {
                    request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                }
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool? onlyServerState, int? pageSize,
            CancellationToken ct)
        {
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/query");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.PostAsync<SupervisorListModel>(uri.Uri, query,
                _serializer, httpRequest =>
                {
                    if (pageSize != null)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(
            string supervisorId, bool? onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(supervisorId))
            {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/{supervisorId}");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.GetAsync<SupervisorModel>(uri.Uri,
                _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.DiscoveryUrl == null)
            {
                throw new ArgumentException("Discovery Url missing.", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications");
            await _httpClient.PostAsync(uri, request, _serializer, request =>
            {
                // if (request.Options.Timeout == null)
                {
                    //     request.Options.Timeout = TimeSpan.FromMinutes(3);
                }
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/discover");
            await _httpClient.PostAsync(uri, request, _serializer, request =>
            {
                // if (request.Options.Timeout == null)
                {
                    //     request.Options.Timeout = TimeSpan.FromMinutes(3);
                }
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelRequestModel request, CancellationToken ct)
        {
            if (request?.Id == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/discover/${request.Id}");
            await _httpClient.DeleteAsync(uri, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResponseModel> RegisterAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ApplicationUri))
            {
                throw new ArgumentException("Application Uri missing", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications");
            return await _httpClient.PutAsync<ApplicationRegistrationResponseModel>(
                uri, request, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task EnableApplicationAsync(string applicationId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{applicationId}/enable");
            await _httpClient.PostAsync(uri, null, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DisableApplicationAsync(string applicationId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{applicationId}/disable");
            await _httpClient.PostAsync(uri, null, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{applicationId}");
            await _httpClient.PatchAsync(uri, request, _serializer,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{applicationId}");
            return await _httpClient.GetAsync<ApplicationRegistrationModel>(
                uri, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/applications/query");
            return await _httpClient.PostAsync<ApplicationInfoListModel>(uri, query,
                _serializer, httpRequest =>
                {
                    if (pageSize != null)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string continuation, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/applications");
            return await _httpClient.GetAsync<ApplicationInfoListModel>(uri,
                _serializer, request =>
                {
                    if (continuation != null)
                    {
                        request.AddHeader(HttpHeader.ContinuationToken, continuation);
                    }
                    if (pageSize != null)
                    {
                        request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/applications/sites");
            return await _httpClient.GetAsync<ApplicationSiteListModel>(uri,
                _serializer, request =>
                {
                    if (continuation != null)
                    {
                        request.AddHeader(HttpHeader.ContinuationToken, continuation);
                    }
                    if (pageSize != null)
                    {
                        request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UnregisterApplicationAsync(string applicationId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{applicationId}");
            await _httpClient.DeleteAsync(uri, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenSince,
            CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/applications?notSeenFor={notSeenSince}");
            await _httpClient.DeleteAsync(uri, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string> RegisterEndpointAsync(ServerEndpointQueryModel query,
            CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/endpoints");
            return await _httpClient.PutAsync<string>(uri, query,
                _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(string continuation,
            bool? onlyServerState, int? pageSize, CancellationToken ct)
        {
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.GetAsync<EndpointInfoListModel>(uri.Uri,
                _serializer, request =>
                {
                    if (continuation != null)
                    {
                        request.AddHeader(HttpHeader.ContinuationToken, continuation);
                    }
                    if (pageSize != null)
                    {
                        request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query, bool? onlyServerState, int? pageSize,
            CancellationToken ct)
        {
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints/query");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.PostAsync<EndpointInfoListModel>(uri.Uri, query,
                _serializer, httpRequest =>
                {
                    if (pageSize != null)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            bool? onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints/{endpointId}");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.GetAsync<EndpointInfoModel>(uri.Uri,
                _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string endpointId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/v2/endpoints/{endpointId}/certificate");
            return await _httpClient.GetAsync<X509CertificateChainModel>(uri,
                _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task ConnectAsync(string endpointId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/v2/endpoints/{endpointId}/activate");
            await _httpClient.PostAsync(uri, null, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(string endpointId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/v2/endpoints/{endpointId}/deactivate");
            await _httpClient.PostAsync(uri, null, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> ListPublishersAsync(
            string continuation, bool? onlyServerState, int? pageSize, CancellationToken ct)
        {
            var uri = new UriBuilder($"{_serviceUri}/v2/publishers");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.GetAsync<PublisherListModel>(uri.Uri,
                _serializer, request =>
                {
                    if (continuation != null)
                    {
                        request.AddHeader(HttpHeader.ContinuationToken, continuation);
                    }
                    if (pageSize != null)
                    {
                        request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdatePublisherAsync(string publisherId,
            PublisherUpdateModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(publisherId))
            {
                throw new ArgumentNullException(nameof(publisherId));
            }
            var uri = new Uri($"{_serviceUri}/v2/publishers/{publisherId}");
            await _httpClient.PatchAsync(uri, request, _serializer,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> QueryPublishersAsync(
            PublisherQueryModel query, bool? onlyServerState, int? pageSize,
            CancellationToken ct)
        {
            var uri = new UriBuilder($"{_serviceUri}/v2/publishers/query");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.PostAsync<PublisherListModel>(uri.Uri, query,
                _serializer, httpRequest =>
                {
                    if (pageSize != null)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublisherModel> GetPublisherAsync(
            string publisherId, bool? onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(publisherId))
            {
                throw new ArgumentNullException(nameof(publisherId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/publishers/{publisherId}");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.GetAsync<PublisherModel>(uri.Uri, _serializer,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<GatewayListModel> ListGatewaysAsync(
            string continuation, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/gateways");
            return await _httpClient.GetAsync<GatewayListModel>(uri, _serializer, request =>
            {
                if (continuation != null)
                {
                    request.AddHeader(HttpHeader.ContinuationToken, continuation);
                }
                if (pageSize != null)
                {
                    request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                }
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdateGatewayAsync(string gatewayId,
            GatewayUpdateModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(gatewayId))
            {
                throw new ArgumentNullException(nameof(gatewayId));
            }
            var uri = new Uri($"{_serviceUri}/v2/gateways/{gatewayId}");
            await _httpClient.PatchAsync(uri, request, _serializer,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<GatewayListModel> QueryGatewaysAsync(
            GatewayQueryModel query, int? pageSize, CancellationToken ct)
        {
            var uri = new UriBuilder($"{_serviceUri}/v2/gateways/query");
            return await _httpClient.PostAsync<GatewayListModel>(uri.Uri, query,
                _serializer, httpRequest =>
                {
                    if (pageSize != null)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
                    }
                }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<GatewayInfoModel> GetGatewayAsync(string gatewayId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(gatewayId))
            {
                throw new ArgumentNullException(nameof(gatewayId));
            }
            var uri = new Uri($"{_serviceUri}/v2/gateways/{gatewayId}");
            return await _httpClient.GetAsync<GatewayInfoModel>(
                uri, _serializer, ct: ct).ConfigureAwait(false);
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly string _serviceUri;
        private readonly ISerializer _serializer;
    }
}
