// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Services {
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.IoTSolutions.Common.Http;
    using Microsoft.Azure.IoTSolutions.Common.Utils;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of v1 service adapter.
    /// </summary>
    public class OpcTwinServiceClient : IOpcTwinService {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public OpcTwinServiceClient(IHttpClient httpClient,
            IOpcTwinConfig config, ILogger logger) {
            _httpClient = httpClient;
            _logger = logger;
            _serviceUri = config.OpcTwinServiceApiUrl;

            if (string.IsNullOrEmpty(_serviceUri)) {
                _serviceUri = "http://localhost:9042/v1";
                _logger.Error(
                    "No opc twin service Uri specified.Using default " +
                    _serviceUri + ". If this is not your intention, or to " +
                    "remove this error, please configure the Url " +
                    "in the appsettings.json file or set the " +
                    "PCS_OPCTWIN_WEBSERVICE_URL environment variable.",
                    () => { });
            }
        }

        /// <summary>
        /// Returns service status
        /// </summary>
        /// <returns></returns>
        public async Task<StatusResponseApiModel> GetServiceStatusAsync() {
            var request = NewRequest($"{_serviceUri}/status");
            var response = await _httpClient.GetAsync(request);
            response.Validate();
            return JsonConvertEx.DeserializeObject<StatusResponseApiModel>(response.Content);
        }

        /// <summary>
        /// List supervisor registrations
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public Task<SupervisorListApiModel> ListSupervisorsAsync(
            string continuation) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/supervisors");
                if (continuation != null) {
                    request.AddHeader(CONTINUATION_TOKEN_NAME, continuation);
                }
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<SupervisorListApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Get supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        public Task<SupervisorApiModel> GetSupervisorAsync(
            string supervisorId) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/supervisors/{supervisorId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<SupervisorApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Update supervisor
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task UpdateSupervisorAsync(SupervisorUpdateApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/supervisors");
                request.SetContent(content);
                var response = await _httpClient.PatchAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Register server using discovery url
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<ApplicationRegistrationResponseApiModel> RegisterAsync(
            ServerRegistrationRequestApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.DiscoveryUrl == null) {
                throw new ArgumentNullException(nameof(content.DiscoveryUrl));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/applications");
                request.SetContent(content);
                request.Options.Timeout = 60000;
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<ApplicationRegistrationResponseApiModel>(
                    response.Content);
            });
        }

        /// <summary>
        /// Register raw application record
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<ApplicationRegistrationResponseApiModel> RegisterAsync(
            ApplicationRegistrationRequestApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.ApplicationUri == null) {
                throw new ArgumentNullException(nameof(content.ApplicationUri));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/applications");
                request.SetContent(content);
                var response = await _httpClient.PutAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<ApplicationRegistrationResponseApiModel>(
                    response.Content);
            });
        }

        /// <summary>
        /// Update application
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task UpdateApplicationAsync(ApplicationRegistrationUpdateApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/applications");
                request.SetContent(content);
                var response = await _httpClient.PatchAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Get application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public Task<ApplicationRegistrationApiModel> GetApplicationAsync(
            string applicationId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/applications/{applicationId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<ApplicationRegistrationApiModel>(
                    response.Content);
            });
        }

        /// <summary>
        /// Returns the application certificate
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public Task<string> GetCertificateAsync(string applicationId) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }

            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Find applications
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public Task<ApplicationInfoListApiModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryApiModel query) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/applications/query");
                request.SetContent(query);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<ApplicationInfoListApiModel>(
                    response.Content);
            });
        }

        /// <summary>
        /// List applications
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public Task<ApplicationInfoListApiModel> ListApplicationsAsync(
            string continuation) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/applications");
                if (continuation != null) {
                    request.AddHeader(CONTINUATION_TOKEN_NAME, continuation);
                }
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<ApplicationInfoListApiModel>(
                    response.Content);
            });
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public Task UnregisterApplicationAsync(string applicationId) {
            if (string.IsNullOrEmpty(applicationId)) {
                throw new ArgumentNullException(nameof(applicationId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/applications/{applicationId}");
                var response = await _httpClient.DeleteAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// List twin registrations
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public Task<TwinInfoListApiModel> ListTwinsAsync(string continuation,
            bool? onlyServerState) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/twins");
                if (continuation != null) {
                    request.AddHeader(CONTINUATION_TOKEN_NAME, continuation);
                }
                if (onlyServerState ?? false) {
                    var uri = new UriBuilder(request.Uri) { Query = "onlyServerState=true" };
                    request.Uri = uri.Uri;
                }
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<TwinInfoListApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Query twin registrations
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public Task<TwinInfoListApiModel> QueryTwinsAsync(
            TwinRegistrationQueryApiModel query, bool? onlyServerState) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/twins/query");
                if (onlyServerState ?? false) {
                    var uri = new UriBuilder(request.Uri) { Query = "onlyServerState=true" };
                    request.Uri = uri.Uri;
                }
                request.SetContent(query);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<TwinInfoListApiModel>(
                    response.Content);
            });
        }

        /// <summary>
        /// Get twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public Task<TwinInfoApiModel> GetTwinAsync(string twinId,
            bool? onlyServerState) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/twins/{twinId}");
                if (onlyServerState ?? false) {
                    var uri = new UriBuilder(request.Uri) { Query = "onlyServerState=true" };
                    request.Uri = uri.Uri;
                }
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<TwinInfoApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Update registration
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task UpdateTwinAsync(TwinRegistrationUpdateApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/twins");
                request.SetContent(content);
                var response = await _httpClient.PatchAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Browse a tree node, returns node properties and all child nodes if not excluded.
        /// </summary>
        /// <param name="twinId">Server twin to talk to</param>
        /// <param name="content">browse node and filters</param>
        /// <returns></returns>
        public Task<BrowseResponseApiModel> NodeBrowseAsync(string twinId,
            BrowseRequestApiModel content) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/browse/{twinId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<BrowseResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Publish node values
        /// </summary>
        /// <param name="twinId">Server twin to talk to</param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<PublishResponseApiModel> NodePublishAsync(string twinId,
            PublishRequestApiModel content) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.NodeId)) {
                throw new ArgumentNullException(nameof(content.NodeId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/publish/{twinId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<PublishResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Get list of published nodes
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="twinId">Server twin to talk to</param>
        /// <returns></returns>
        public Task<PublishedNodeListApiModel> ListPublishedNodesAsync(string continuation,
            string twinId) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/publish/{twinId}/state");
                if (continuation != null) {
                    request.AddHeader(CONTINUATION_TOKEN_NAME, continuation);
                }
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<PublishedNodeListApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Read a variable value
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="content">Read nodes</param>
        /// <returns></returns>
        public Task<ValueReadResponseApiModel> NodeValueReadAsync(string twinId,
            ValueReadRequestApiModel content) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.NodeId)) {
                throw new ArgumentException(nameof(content.NodeId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/read/{twinId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<ValueReadResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Write variable value
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string twinId,
            ValueWriteRequestApiModel content) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Node == null) {
                throw new ArgumentNullException(nameof(content.Node));
            }
            if (content.Value == null) {
                throw new ArgumentNullException(nameof(content.Value));
            }
            if (string.IsNullOrEmpty(content.Node.Id)) {
                throw new ArgumentException(nameof(content.Node.Id));
            }
            if (string.IsNullOrEmpty(content.Node.DataType)) {
                throw new ArgumentException(nameof(content.Node.DataType));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/write/{twinId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<ValueWriteResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string twinId, MethodMetadataRequestApiModel content) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.MethodId)) {
                throw new ArgumentNullException(nameof(content.MethodId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/call/{twinId}/$metadata");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<MethodMetadataResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            string twinId, MethodCallRequestApiModel content) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.MethodId)) {
                throw new ArgumentNullException(nameof(content.MethodId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/call/{twinId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<MethodCallResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Helper to create new request
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static HttpRequest NewRequest(string uri) {
            var request = new HttpRequest();
            request.SetUriFromString(uri);
            if (uri.ToLowerInvariant().StartsWith("https:",
                StringComparison.Ordinal)) {
                request.Options.AllowInsecureSSLServer = true;
            }
            return request;
        }

        private const string CONTINUATION_TOKEN_NAME = "x-ms-continuation";
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _serviceUri;
    }
}
