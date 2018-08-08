// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of v1 service adapter.
    /// </summary>
    public class RegistryServiceClient : IRegistryServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public RegistryServiceClient(IHttpClient httpClient, IRegistryConfig config,
            ILogger logger) :
            this (httpClient, config.OpcUaRegistryServiceUrl,
                config.OpcUaRegistryServiceResourceId, logger){
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        /// <param name="logger"></param>
        public RegistryServiceClient(IHttpClient httpClient, string serviceUri,
            string resourceId, ILogger logger) {
            _httpClient = httpClient;
            _logger = logger;
            _serviceUri = serviceUri;
            _resourceId = resourceId;

            if (string.IsNullOrEmpty(_serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the registry micro service.");
            }
        }

        /// <summary>
        /// Returns service status
        /// </summary>
        /// <returns></returns>
        public async Task<StatusResponseApiModel> GetServiceStatusAsync() {
            var request = _httpClient.NewRequest($"{_serviceUri}/status", _resourceId);
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<StatusResponseApiModel>(response.GetContentAsString());
        }

        /// <summary>
        /// List supervisor registrations
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<SupervisorListApiModel> ListSupervisorsAsync(string continuation,
            int? pageSize) {
            var request = _httpClient.NewRequest($"{_serviceUri}/supervisors", _resourceId);
            if (continuation != null) {
                request.AddHeader(kContinuationTokenHeaderKey, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<SupervisorListApiModel>(response.GetContentAsString());
        }

        /// <summary>
        /// Query supervisors
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<SupervisorListApiModel> QuerySupervisorsAsync(
            SupervisorQueryApiModel query, int? pageSize) {
            var request = _httpClient.NewRequest($"{_serviceUri}/supervisors/query",
                _resourceId);
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<SupervisorListApiModel>(
                response.GetContentAsString());
        }

        /// <summary>
        /// Get supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        public async Task<SupervisorApiModel> GetSupervisorAsync(
            string supervisorId) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/supervisors/{supervisorId}",
                _resourceId);
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<SupervisorApiModel>(response.GetContentAsString());
        }

        /// <summary>
        /// Update supervisor
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task UpdateSupervisorAsync(SupervisorUpdateApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/supervisors",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PatchAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        /// <summary>
        /// Register server using discovery url
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task RegisterAsync(ServerRegistrationRequestApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.DiscoveryUrl == null) {
                throw new ArgumentNullException(nameof(content.DiscoveryUrl));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/applications",
                _resourceId);
            request.SetContent(content);
            request.Options.Timeout = 60000;
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        /// <summary>
        /// Kick off a one time discovery on all supervisors
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task DiscoverAsync(DiscoveryRequestApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/applications/discover",
                _resourceId);
            request.SetContent(content);
            request.Options.Timeout = 60000;
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        /// <summary>
        /// Register raw application record
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationResponseApiModel> RegisterAsync(
            ApplicationRegistrationRequestApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.ApplicationUri == null) {
                throw new ArgumentNullException(nameof(content.ApplicationUri));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/applications",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PutAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationRegistrationResponseApiModel>(
                response.GetContentAsString());
        }

        /// <summary>
        /// Update application
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task UpdateApplicationAsync(ApplicationRegistrationUpdateApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/applications",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PatchAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        /// <summary>
        /// Get application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationApiModel> GetApplicationAsync(
            string applicationId) {
            var request = _httpClient.NewRequest($"{_serviceUri}/applications/{applicationId}",
                _resourceId);
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationRegistrationApiModel>(
                response.GetContentAsString());
        }

        /// <summary>
        /// Query applications
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<ApplicationInfoListApiModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryApiModel query, int? pageSize) {
            var request = _httpClient.NewRequest($"{_serviceUri}/applications/query",
                _resourceId);
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationInfoListApiModel>(
                response.GetContentAsString());
        }

        /// <summary>
        /// List applications
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<ApplicationInfoListApiModel> ListApplicationsAsync(
            string continuation, int? pageSize) {
            var request = _httpClient.NewRequest($"{_serviceUri}/applications",
                _resourceId);
            if (continuation != null) {
                request.AddHeader(kContinuationTokenHeaderKey, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationInfoListApiModel>(
                response.GetContentAsString());
        }

        /// <summary>
        /// List sites
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<ApplicationSiteListApiModel> ListSitesAsync(
            string continuation, int? pageSize) {
            var request = _httpClient.NewRequest($"{_serviceUri}/applications/sites",
                _resourceId);
            if (continuation != null) {
                request.AddHeader(kContinuationTokenHeaderKey, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationSiteListApiModel>(
                response.GetContentAsString());
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public async Task UnregisterApplicationAsync(string applicationId) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/applications/{applicationId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        /// <summary>
        /// Unregister applications disabled specified amount of time ago.
        /// </summary>
        /// <param name="notSeenFor"></param>
        /// <returns></returns>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor) {
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/applications?notSeenFor={notSeenFor}", _resourceId);
            var response = await _httpClient.DeleteAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        /// <summary>
        /// List twin registrations
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<TwinInfoListApiModel> ListTwinsAsync(string continuation,
            bool? onlyServerState, int? pageSize) {
            var uri = new UriBuilder($"{_serviceUri}/twins");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (continuation != null) {
                request.AddHeader(kContinuationTokenHeaderKey, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<TwinInfoListApiModel>(response.GetContentAsString());
        }

        /// <summary>
        /// Query twin registrations
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<TwinInfoListApiModel> QueryTwinsAsync(TwinRegistrationQueryApiModel query,
            bool? onlyServerState, int? pageSize) {
            var uri = new UriBuilder($"{_serviceUri}/twins/query");
            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<TwinInfoListApiModel>(
                response.GetContentAsString());
        }

        /// <summary>
        /// Get twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public async Task<TwinInfoApiModel> GetTwinAsync(string twinId,
            bool? onlyServerState) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            var uri = new UriBuilder($"{_serviceUri}/twins/{twinId}");

            if (onlyServerState ?? false) {
                uri.Query = "onlyServerState=true";
            }
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<TwinInfoApiModel>(response.GetContentAsString());
        }

        /// <summary>
        /// Update registration
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task UpdateTwinAsync(TwinRegistrationUpdateApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/twins",
                _resourceId);
            request.SetContent(content);
            var response = await _httpClient.PatchAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        private const string kContinuationTokenHeaderKey = "x-ms-continuation";
        private const string kPageSizeHeaderKey = "x-ms-max-item-count";
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _serviceUri;
        private readonly string _resourceId;
    }
}
