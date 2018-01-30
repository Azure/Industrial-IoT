// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External.Manager {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Http;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Runtime;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Utils;
    using Newtonsoft.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of v1 service adapter.
    /// </summary>
    public class IoTHubManagerServiceClient : IIoTHubTwinServices {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTHubManagerServiceClient(IHttpClient httpClient,
            IOpcUaServicesConfig config, ILogger logger) {
            _httpClient = httpClient;
            _logger = logger;
            _serviceUri = config.IoTHubManagerV1ApiUrl;
        }

        /// <summary>
        /// Create or update a device
        /// </summary>
        /// <param name="device">Device information</param>
        /// <returns>Device information</returns>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel device) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/{device.Id}");
                request.SetContent(device);
                var response = await _httpClient.PutAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<DeviceTwinModel>(response.Content);
            });
        }

        /// <summary>
        /// Call method on device
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Task<MethodResultModel> CallMethodAsync(
            string twinId, MethodParameterModel parameters) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/{twinId}/methods");
                request.SetContent(parameters);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<MethodResultModel>(response.Content);
            });
        }

        /// <summary>
        /// Returns device twin object
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns>Device information</returns>
        public Task<DeviceTwinModel> GetAsync(string twinId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/{twinId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<DeviceTwinModel>(response.Content);
            });
        }

        /// <summary>
        /// Delete device twin
        /// </summary>
        /// <param name="twinId"></param>
        public Task DeleteAsync(string twinId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/{twinId}");
                var response = await _httpClient.DeleteAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Query single page and add results to list
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public Task<DeviceTwinListModel> QueryAsync(string query,
            string continuation) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/query");
                request.SetContent(query);
                if (continuation != null) {
                    request.Headers.Add("x-ms-continuation", continuation);
                }
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvert.DeserializeObject<DeviceTwinListModel>(
                    response.Content);
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
                System.StringComparison.Ordinal)) {
                request.Options.AllowInsecureSSLServer = true;
            }
            return request;
        }

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _serviceUri;
    }
}
