// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Manager {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Runtime;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.IoTSolutions.Common.Http;
    using Microsoft.Azure.IoTSolutions.Common.Utils;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Implementation of v1 service adapter.
    /// </summary>
    public class IoTHubManagerServiceClient : IIoTHubTwinServices, IIoTHubJobServices {

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
        /// <param name="twin">Device information</param>
        /// <returns>Device information</returns>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel twin) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/devices/{twin.Id}");
                request.SetContent(twin);
                var response = await _httpClient.PutAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<DeviceTwinModel>(response.Content);
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
                var request = NewRequest($"{_serviceUri}/devices/{twinId}/methods");
                request.SetContent(parameters);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<MethodResultModel>(response.Content);
            });
        }

        /// <summary>
        /// Update device properties through twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public Task UpdatePropertiesAsync(string twinId,
            Dictionary<string, JToken> properties) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/devices/{twinId}");
                request.SetContent(new DeviceTwinModel {
                    Id = twinId,
                    Properties = new TwinPropertiesModel {
                        Desired = properties
                    }
                });
                var response = await _httpClient.PutAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Returns device twin object
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns>Device information</returns>
        public Task<DeviceTwinModel> GetAsync(string twinId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/devices/{twinId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<DeviceTwinModel>(response.Content);
            });
        }

        /// <summary>
        /// Returns registration info
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns>Registration info</returns>
        public Task<DeviceModel> GetRegistrationAsync(string twinId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/devices/{twinId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<DeviceModel>(response.Content);
            });
        }

        /// <summary>
        /// Delete device twin
        /// </summary>
        /// <param name="twinId"></param>
        public Task DeleteAsync(string twinId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/devices/{twinId}");
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
                var request = NewRequest($"{_serviceUri}/devices/query");
                request.SetContent(query);
                if (continuation != null) {
                    request.Headers.Add("x-ms-continuation", continuation);
                }
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<DeviceTwinListModel>(
                    response.Content);
            });
        }

        /// <summary>
        /// Return raw query response
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public Task<Tuple<string, string>> QueryRawAsync(
            string query, string continuation) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/devices/query");
                request.SetContent(query);
                if (continuation != null) {
                    request.Headers.Add("x-ms-continuation", continuation);
                }
                var response = await _httpClient.PostAsync(request);
                response.Validate();

                // TODO: Manager does not support yet
                if (response.Content != null) throw new NotSupportedException();
                // TODO: Manager does not support yet

                return Tuple.Create((string)null, response.Content);
            });
        }

        /// <summary>
        /// Create job
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public Task<JobModel> CreateAsync(JobModel job) {
            if (job.UpdateTwin != null) {
                job.UpdateTwin.Id = null;
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/jobs/{job.JobId}");
                request.SetContent(job);
                var response = await _httpClient.PutAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<JobModel>(response.Content);
            });
        }

        /// <summary>
        /// Refresh job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public Task<JobModel> RefreshAsync(string jobId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/jobs/{jobId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return JsonConvertEx.DeserializeObject<JobModel>(response.Content);
            });
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public Task CancelAsync(string jobId) {

            // TODO: Log bug to pcs team, this is not yet implemented

            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"{_serviceUri}/jobs/{jobId}");
                var response = await _httpClient.DeleteAsync(request);
                response.Validate();
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
            if (uri.ToLowerInvariant().StartsWith("https:", StringComparison.Ordinal)) {
                request.Options.AllowInsecureSSLServer = true;
            }
            return request;
        }

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _serviceUri;
    }
}
