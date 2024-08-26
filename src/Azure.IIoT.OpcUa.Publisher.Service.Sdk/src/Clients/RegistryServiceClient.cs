// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Http;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
        /// <param name="options"></param>
        /// <param name="serializers"></param>
        public RegistryServiceClient(IHttpClientFactory httpClient,
            IOptions<ServiceSdkOptions> options, IEnumerable<ISerializer> serializers) :
            this(httpClient, options.Value.ServiceUrl!, options.Value.TokenProvider,
                serializers.Resolve(options.Value))
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="authorization"></param>
        /// <param name="serializer"></param>
        public RegistryServiceClient(IHttpClientFactory httpClient, string serviceUri,
            Func<Task<string?>>? authorization, ISerializer? serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the registry micro service.");
            }
            _serviceUri = serviceUri.TrimEnd('/') + "/registry";
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authorization = authorization;
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serializer"></param>
        public RegistryServiceClient(HttpClient httpClient, ISerializer? serializer = null) :
            this(httpClient.ToHttpClientFactory(), httpClient.BaseAddress?.ToString()!,
                null, serializer)
        {
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct)
        {
            using var httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri($"{_serviceUri}/healthz")
            };
            try
            {
                using var response = await _httpClient.GetAsync(httpRequest,
                    authorization: _authorization, ct: ct).ConfigureAwait(false);
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
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            var uri = new Uri($"{_serviceUri}/v2/discovery/{Uri.EscapeDataString(discovererId)}");
            await _httpClient.PatchAsync(uri, request, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<DiscovererListModel> ListDiscoverersAsync(
            string? continuation, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/discovery");
            return await _httpClient.GetAsync<DiscovererListModel>(uri,
                _serializer, request =>
            {
                if (continuation != null)
                {
                    request.AddHeader(HttpHeader.ContinuationToken, continuation);
                }
                if (pageSize.HasValue)
                {
                    request.AddHeader(HttpHeader.MaxItemCount,
                        pageSize.Value.ToString(CultureInfo.InvariantCulture));
                }
            }, authorization: _authorization, ct: ct).ConfigureAwait(false);
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
                    if (pageSize.HasValue)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<DiscovererModel> GetDiscovererAsync(string discovererId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            var uri = new Uri($"{_serviceUri}/v2/discovery/{Uri.EscapeDataString(discovererId)}");
            return await _httpClient.GetAsync<DiscovererModel>(uri, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(supervisorId))
            {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new Uri($"{_serviceUri}/v2/supervisors/{Uri.EscapeDataString(supervisorId)}");
            await _httpClient.PatchAsync(uri, request, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<SupervisorListModel> ListSupervisorsAsync(
            string? continuation, bool? onlyServerState, int? pageSize, CancellationToken ct)
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
                if (pageSize.HasValue)
                {
                    request.AddHeader(HttpHeader.MaxItemCount,
                        pageSize.Value.ToString(CultureInfo.InvariantCulture));
                }
            }, authorization: _authorization, ct: ct).ConfigureAwait(false);
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
                    if (pageSize.HasValue)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<SupervisorModel> GetSupervisorAsync(
            string supervisorId, bool? onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(supervisorId))
            {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/supervisors/{Uri.EscapeDataString(supervisorId)}");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.GetAsync<SupervisorModel>(uri.Uri,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request,
            string? discovererId, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.DiscoveryUrl == null)
            {
                throw new ArgumentException("Discovery Url missing.", nameof(request));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/applications");
            if (discovererId != null)
            {
                uri.Query = "discovererId=" + discovererId;
            }
            await _httpClient.PostAsync(uri.Uri, request, _serializer,
                request => request.SetTimeout(TimeSpan.FromMinutes(3)),
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request,
            string? discovererId, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            var uri = new UriBuilder($"{_serviceUri}/v2/applications/discover");
            if (discovererId != null)
            {
                uri.Query = "discovererId=" + discovererId;
            }
            await _httpClient.PostAsync(uri.Uri, request, _serializer,
                request => request.SetTimeout(TimeSpan.FromMinutes(3)),
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelRequestModel request,
            string? discovererId, CancellationToken ct)
        {
            if (request?.Id == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var uri = new UriBuilder(
                $"{_serviceUri}/v2/applications/discover/${Uri.EscapeDataString(request.Id)}");
            if (discovererId != null)
            {
                uri.Query = "discovererId=" + discovererId;
            }
            await _httpClient.DeleteAsync(uri.Uri,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationResponseModel> RegisterAsync(
            ApplicationRegistrationRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ApplicationUri))
            {
                throw new ArgumentException("Application Uri missing", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications");
            return await _httpClient.PutAsync<ApplicationRegistrationResponseModel>(uri,
                request, _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task EnableApplicationAsync(string applicationId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{Uri.EscapeDataString(applicationId)}/enable");
            await _httpClient.PostAsync(uri, string.Empty, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DisableApplicationAsync(string applicationId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{Uri.EscapeDataString(applicationId)}/disable");
            await _httpClient.PostAsync(uri, string.Empty, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdateApplicationAsync(string applicationId,
            ApplicationRegistrationUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{Uri.EscapeDataString(applicationId)}");
            await _httpClient.PatchAsync(uri, request, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationRegistrationModel> GetApplicationAsync(
            string applicationId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{Uri.EscapeDataString(applicationId)}");
            return await _httpClient.GetAsync<ApplicationRegistrationModel>(uri,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryModel query, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/applications/query");
            return await _httpClient.PostAsync<ApplicationInfoListModel>(uri, query,
                _serializer, httpRequest =>
                {
                    if (pageSize.HasValue)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListApplicationsAsync(
            string? continuation, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/applications");
            return await _httpClient.GetAsync<ApplicationInfoListModel>(uri,
                _serializer, request =>
                {
                    if (continuation != null)
                    {
                        request.AddHeader(HttpHeader.ContinuationToken, continuation);
                    }
                    if (pageSize.HasValue)
                    {
                        request.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string? continuation, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/applications/sites");
            return await _httpClient.GetAsync<ApplicationSiteListModel>(uri,
                _serializer, request =>
                {
                    if (continuation != null)
                    {
                        request.AddHeader(HttpHeader.ContinuationToken, continuation);
                    }
                    if (pageSize.HasValue)
                    {
                        request.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UnregisterApplicationAsync(string applicationId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var uri = new Uri($"{_serviceUri}/v2/applications/{Uri.EscapeDataString(applicationId)}");
            await _httpClient.DeleteAsync(uri, authorization: _authorization,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenSince,
            CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/applications?notSeenFor={notSeenSince}");
            await _httpClient.DeleteAsync(uri, authorization: _authorization,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string> RegisterEndpointAsync(ServerEndpointQueryModel query,
            string? discovererId, CancellationToken ct)
        {
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints");
            if (discovererId != null)
            {
                uri.Query = "discovererId=" + discovererId;
            }
            return await _httpClient.PutAsync<string>(uri.Uri, query,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoListModel> ListEndpointsAsync(string? continuation,
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
                    if (pageSize.HasValue)
                    {
                        request.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
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
                    if (pageSize.HasValue)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            bool? onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/endpoints/{Uri.EscapeDataString(endpointId)}");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.GetAsync<EndpointInfoModel>(uri.Uri,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string endpointId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/v2/endpoints/{Uri.EscapeDataString(endpointId)}/certificate");
            return await _httpClient.GetAsync<X509CertificateChainModel>(uri,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TestConnectionResponseModel> TestConnectionAsync(string endpointId,
            TestConnectionRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/v2/endpoints/{Uri.EscapeDataString(endpointId)}/test");
            return await _httpClient.PostAsync<TestConnectionResponseModel>(uri, request,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ConnectResponseModel> ConnectAsync(string endpointId,
            ConnectRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/v2/endpoints/{Uri.EscapeDataString(endpointId)}/connect");
            return await _httpClient.PostAsync<ConnectResponseModel>(uri, request,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(string endpointId,
            DisconnectRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/v2/endpoints/{Uri.EscapeDataString(endpointId)}/disconnect");
            await _httpClient.PostAsync(uri, request, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublisherListModel> ListPublishersAsync(
            string? continuation, bool? onlyServerState, int? pageSize, CancellationToken ct)
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
                    if (pageSize.HasValue)
                    {
                        request.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdatePublisherAsync(string publisherId,
            PublisherUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(publisherId))
            {
                throw new ArgumentNullException(nameof(publisherId));
            }
            var uri = new Uri($"{_serviceUri}/v2/publishers/{Uri.EscapeDataString(publisherId)}");
            await _httpClient.PatchAsync(uri, request, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<PublishedNodesEntryModel> GetConfiguredEndpointsAsync(
            string publisherId, GetConfiguredEndpointsRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(publisherId))
            {
                throw new ArgumentNullException(nameof(publisherId));
            }
            var uri = new UriBuilder(
                $"{_serviceUri}/v2/publishers/{Uri.EscapeDataString(publisherId)}/endpoints");
            if (request?.IncludeNodes ?? false)
            {
                uri.Query = "includeNodes=true";
            }
            return _httpClient.GetStreamAsync<PublishedNodesEntryModel>(uri.Uri, _serializer,
                authorization: _authorization, ct: ct);
        }

        /// <inheritdoc/>
        public async Task SetConfiguredEndpointsAsync(string publisherId,
            SetConfiguredEndpointsRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            var uri = new Uri($"{_serviceUri}/v2/publishers/{Uri.EscapeDataString(publisherId)}/endpoints");
            await _httpClient.PutAsync(uri, request,
                _serializer, authorization: _authorization, ct: ct).ConfigureAwait(false);
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
                    if (pageSize.HasValue)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublisherModel> GetPublisherAsync(
            string publisherId, bool? onlyServerState, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(publisherId))
            {
                throw new ArgumentNullException(nameof(publisherId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/publishers/{Uri.EscapeDataString(publisherId)}");
            if (onlyServerState ?? false)
            {
                uri.Query = "onlyServerState=true";
            }
            return await _httpClient.GetAsync<PublisherModel>(uri.Uri, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<GatewayListModel> ListGatewaysAsync(
            string? continuation, int? pageSize, CancellationToken ct)
        {
            var uri = new Uri($"{_serviceUri}/v2/gateways");
            return await _httpClient.GetAsync<GatewayListModel>(uri, _serializer, request =>
            {
                if (continuation != null)
                {
                    request.AddHeader(HttpHeader.ContinuationToken, continuation);
                }
                if (pageSize.HasValue)
                {
                    request.AddHeader(HttpHeader.MaxItemCount,
                        pageSize.Value.ToString(CultureInfo.InvariantCulture));
                }
            }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdateGatewayAsync(string gatewayId,
            GatewayUpdateModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(gatewayId))
            {
                throw new ArgumentNullException(nameof(gatewayId));
            }
            var uri = new Uri($"{_serviceUri}/v2/gateways/{Uri.EscapeDataString(gatewayId)}");
            await _httpClient.PatchAsync(uri, request, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<GatewayListModel> QueryGatewaysAsync(
            GatewayQueryModel query, int? pageSize, CancellationToken ct)
        {
            var uri = new UriBuilder($"{_serviceUri}/v2/gateways/query");
            return await _httpClient.PostAsync<GatewayListModel>(uri.Uri, query,
                _serializer, httpRequest =>
                {
                    if (pageSize.HasValue)
                    {
                        httpRequest.AddHeader(HttpHeader.MaxItemCount,
                            pageSize.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<GatewayInfoModel> GetGatewayAsync(string gatewayId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(gatewayId))
            {
                throw new ArgumentNullException(nameof(gatewayId));
            }
            var uri = new Uri($"{_serviceUri}/v2/gateways/{Uri.EscapeDataString(gatewayId)}");
            return await _httpClient.GetAsync<GatewayInfoModel>(uri, _serializer,
                authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly Func<Task<string?>>? _authorization;
        private readonly string _serviceUri;
        private readonly ISerializer _serializer;
    }
}
