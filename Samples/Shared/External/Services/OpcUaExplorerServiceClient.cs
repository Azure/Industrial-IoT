// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.External.Services {
    using Microsoft.Azure.IoTSolutions.Shared.External.Models;
    using Microsoft.Azure.IoTSolutions.Shared.Diagnostics;
    using Microsoft.Azure.IoTSolutions.Shared.Http;
    using Microsoft.Azure.IoTSolutions.Shared.Utils;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of v1 service adapter.
    /// </summary>
    public class OpcUaExplorerServiceClient : IOpcUaExplorerService {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public OpcUaExplorerServiceClient(IHttpClient httpClient,
            IOpcUaExplorerConfig config, ILogger logger) {
            _httpClient = httpClient;
            _logger = logger;
            _serviceUri = config.OpcUaExplorerV1ApiUrl;

            if (string.IsNullOrEmpty(_serviceUri)) {
                _serviceUri = "http://localhost:9042/v1";
                _logger.Error(
                    "No opc ua explorer service Uri specified.Using default " +
                    _serviceUri + ". If this is not your intention, or to " +
                    "remove this error, please configure the Url " +
                    "in the appsettings.json file or set the " +
                    "PCS_OPCUAEXPLORER_WEBSERVICE_URL environment variable.",
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
            return JsonConvert.DeserializeObject<StatusResponseApiModel>(response.Content);
        }

        /// <summary>
        /// Register endpoint
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<ServerRegistrationResponseApiModel> RegisterEndpointAsync(
            ServerRegistrationRequestApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (content.Endpoint == null) {
                throw new ArgumentNullException(nameof(content.Endpoint));
            }
            if (content.Endpoint.Url == null) {
                throw new ArgumentNullException(nameof(content.Endpoint.Url));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/endpoints");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<ServerRegistrationResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// List endpoints
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public Task<ServerRegistrationListApiModel> ListEndpointsAsync(string continuation) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/endpoints");
                if (continuation != null) {
                    request.AddHeader(CONTINUATION_TOKEN_NAME, continuation);
                }
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<ServerRegistrationListApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Get endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public Task<ServerEndpointApiModel> GetEndpointAsync(string endpointId) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/endpoints/{endpointId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<ServerEndpointApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Update registration
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task UpdateEndpointAsync(ServerRegistrationApiModel content) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/endpoints");
                request.SetContent(content);
                var response = await _httpClient.PatchAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Delete endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public Task DeleteEndpointAsync(string endpointId) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/endpoints/{endpointId}");
                var response = await _httpClient.DeleteAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Browse a tree node, returns node properties and all child nodes if not excluded.
        /// </summary>
        /// <param name="endpointId">Endpoint url of the server to talk to</param>
        /// <param name="content">browse node and filters</param>
        /// <returns></returns>
        public Task<BrowseResponseApiModel> NodeBrowseAsync(string endpointId,
            BrowseRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/browse/{endpointId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<BrowseResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Publish node values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<PublishResponseApiModel> NodePublishAsync(string endpointId,
            PublishRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.NodeId)) {
                throw new ArgumentNullException(nameof(content.NodeId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/publish/{endpointId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<PublishResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Read a variable value
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content">Read nodes</param>
        /// <returns></returns>
        public Task<ValueReadResponseApiModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.NodeId)) {
                throw new ArgumentException(nameof(content.NodeId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/read/{endpointId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<ValueReadResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Write variable value
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
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
                var request = NewRequest($"{_serviceUri}/write/{endpointId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<ValueWriteResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.MethodId)) {
                throw new ArgumentNullException(nameof(content.MethodId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/call/{endpointId}/$metadata");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<MethodMetadataResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            string endpointId, MethodCallRequestApiModel content) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrEmpty(content.MethodId)) {
                throw new ArgumentNullException(nameof(content.MethodId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/call/{endpointId}");
                request.SetContent(content);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<MethodCallResponseApiModel>(response.Content);
            });
        }

        /// <summary>
        /// Returns the client certificate
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public Task<string> GetClientCertificateAsync(string endpointId) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }

            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the server certificate
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public Task<string> GetServerCertificateAsync(string endpointId) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }

            // TODO
            throw new NotImplementedException();
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
