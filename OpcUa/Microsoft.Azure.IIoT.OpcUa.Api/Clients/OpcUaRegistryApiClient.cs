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
    public class OpcUaRegistryApiClient : IOpcUaRegistryApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public OpcUaRegistryApiClient(IHttpClient httpClient,
            IOpcUaRegistryConfig config, ILogger logger) {
            _httpClient = httpClient;
            _logger = logger;
            _serviceUri = config.OpcUaRegistryServiceUrl;

            if (string.IsNullOrEmpty(_serviceUri)) {
                _serviceUri = "http://localhost:9041/v1";
                _logger.Error(
                    "No opc twin service Uri specified.Using default " +
                    _serviceUri + ". If this is not your intention, or to " +
                    "remove this error, please configure the Url " +
                    "in the appsettings.json file or set the " +
                    "PCS_OPCTWIN_WEBSERVICE_URL environment variable.",
                    () => {});
            }
        }

        /// <summary>
        /// Returns service status
        /// </summary>
        /// <returns></returns>
        public async Task<StatusResponseApiModel> GetServiceStatusAsync() {
            var request = NewRequest($"{_serviceUri}/status");
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<StatusResponseApiModel>(response.Content);
        }

        /// <summary>
        /// List supervisor registrations
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<SupervisorListApiModel> ListSupervisorsAsync(string continuation,
            int? pageSize) {
            var request = NewRequest($"{_serviceUri}/supervisors");
            if (continuation != null) {
                request.AddHeader(kContinuationTokenHeaderKey, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<SupervisorListApiModel>(response.Content);
        }

        /// <summary>
        /// Query supervisors
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<SupervisorListApiModel> QuerySupervisorsAsync(
            SupervisorQueryApiModel query, int? pageSize) {
            var request = NewRequest($"{_serviceUri}/supervisors/query");
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<SupervisorListApiModel>(
                response.Content);
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
            var request = NewRequest($"{_serviceUri}/supervisors/{supervisorId}");
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<SupervisorApiModel>(response.Content);
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
            var request = NewRequest($"{_serviceUri}/supervisors");
            request.SetContent(content);
            var response = await _httpClient.PatchAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        /// <summary>
        /// Register server using discovery url
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationResponseApiModel> RegisterAsync(
            ServerRegistrationRequestApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.DiscoveryUrl == null) {
                throw new ArgumentNullException(nameof(content.DiscoveryUrl));
            }
            var request = NewRequest($"{_serviceUri}/applications");
            request.SetContent(content);
            request.Options.Timeout = 60000;
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationRegistrationResponseApiModel>(
                response.Content);
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
            var request = NewRequest($"{_serviceUri}/applications");
            request.SetContent(content);
            var response = await _httpClient.PutAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationRegistrationResponseApiModel>(
                response.Content);
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
            var request = NewRequest($"{_serviceUri}/applications");
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
            var request = NewRequest($"{_serviceUri}/applications/{applicationId}");
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationRegistrationApiModel>(
                response.Content);
        }

        /// <summary>
        /// Query applications
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<ApplicationInfoListApiModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryApiModel query, int? pageSize) {
            var request = NewRequest($"{_serviceUri}/applications/query");
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationInfoListApiModel>(
                response.Content);
        }

        /// <summary>
        /// List applications
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<ApplicationInfoListApiModel> ListApplicationsAsync(string continuation,
            int? pageSize) {
            var request = NewRequest($"{_serviceUri}/applications");
            if (continuation != null) {
                request.AddHeader(kContinuationTokenHeaderKey, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<ApplicationInfoListApiModel>(
                response.Content);
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
            var request = NewRequest($"{_serviceUri}/applications/{applicationId}");
            var response = await _httpClient.DeleteAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        /// <summary>
        /// Unregister applications disabled specified amount of time ago.
        /// </summary>
        /// <param name="notSeenFor"></param>
        /// <returns></returns>
        public async Task PurgeDisabledApplicationsAsync(TimeSpan notSeenFor) {
            var request = NewRequest($"{_serviceUri}/applications?notSeenFor={notSeenFor}");
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
            var request = NewRequest($"{_serviceUri}/twins");
            if (continuation != null) {
                request.AddHeader(kContinuationTokenHeaderKey, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            if (onlyServerState ?? false) {
                var uri = new UriBuilder(request.Uri) { Query = "onlyServerState=true" };
                request.Uri = uri.Uri;
            }
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<TwinInfoListApiModel>(response.Content);
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
            var request = NewRequest($"{_serviceUri}/twins/query");
            if (pageSize != null) {
                request.AddHeader(kPageSizeHeaderKey, pageSize.ToString());
            }
            if (onlyServerState ?? false) {
                var uri = new UriBuilder(request.Uri) { Query = "onlyServerState=true" };
                request.Uri = uri.Uri;
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<TwinInfoListApiModel>(
                response.Content);
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
            var request = NewRequest($"{_serviceUri}/twins/{twinId}");
            if (onlyServerState ?? false) {
                var uri = new UriBuilder(request.Uri) { Query = "onlyServerState=true" };
                request.Uri = uri.Uri;
            }
            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            response.Validate();
            return JsonConvertEx.DeserializeObject<TwinInfoApiModel>(response.Content);
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
            var request = NewRequest($"{_serviceUri}/twins");
            request.SetContent(content);
            var response = await _httpClient.PatchAsync(request).ConfigureAwait(false);
            response.Validate();
        }

        /// <summary>
        /// Helper to create new request
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static HttpRequest NewRequest(string uri) {
            var request = new HttpRequest();
            request.SetUriFromString(uri);
            if (uri.ToLowerInvariant().StartsWith("https:", StringComparison.Ordinal)) {
                request.Options.AllowInsecureSSLServer = true;
            }
            return request;
        }

        private const string kContinuationTokenHeaderKey = "x-ms-continuation";
        private const string kPageSizeHeaderKey = "x-ms-max-item-count";
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _serviceUri;
    }
}
