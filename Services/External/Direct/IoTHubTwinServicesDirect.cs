// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External.Direct {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Http;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Runtime;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Utils;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin services, talking to iot hub directly.  This is
    /// intended for stand alone testing, not for production.  See samples for
    /// details.
    /// </summary>
    public class IoTHubTwinServicesDirect : IIoTHubTwinServices {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTHubTwinServicesDirect(IHttpClient httpClient,
            IOpcUaServicesConfig config, ILogger logger) {
            _httpClient = httpClient;
            _logger = logger;
            if (string.IsNullOrEmpty(config.IoTHubConnString)) {
                throw new ArgumentException(nameof(config));
            }
            _hubConnectionString = ConnectionString.Parse(config.IoTHubConnString);
        }

        /// <summary>
        /// Create or update a device
        /// </summary>
        /// <param name="twin">Device information</param>
        /// <returns>Device information</returns>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel twin) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                // First try create
                try {
                    var request = NewRequest($"/devices/{twin.Id}");
                    request.SetContent(new {
                        deviceId = twin.Id
                    });
                    var response = await _httpClient.PutAsync(request);
                    response.Validate();
                }
                catch (ConflictingResourceException) {
                    // Expected for update
                }
                catch (Exception e) {
                    _logger.Debug("Create failed in CreateOrUpdate", () => e);
                }
                // Then update twin assuming it now exists.  If fails, retry...
                {
                    var request = NewRequest($"/twins/{twin.Id}");
                    request.SetContent(new {
                        tags = twin.Tags
                    });
                    request.Headers.Add("If-Match", 
                        string.IsNullOrEmpty(twin.Etag) ? @"""*""" : twin.Etag);
                    var response = await _httpClient.PatchAsync(request);
                    response.Validate();
                    dynamic result = JToken.Parse(response.Content);
                    twin = new DeviceTwinModel {
                        Etag = result.etag,
                        Id = result.deviceId,
                        Tags = ((JObject)result.tags).Children().ToDictionary(
                            p => ((JProperty)p).Name, p => ((JProperty)p).Value)
                    };
                    return twin;
                }
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
                var request = NewRequest($"/twins/{twinId}/methods");

                // TODO: Add timeouts...
                request.SetContent(new {
                    methodName = parameters.Name,
                    // responseTimeoutInSeconds = ...
                    payload = JToken.Parse(parameters.JsonPayload)
                });
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                dynamic result = JToken.Parse(response.Content);
                return new MethodResultModel {
                    JsonPayload = ((JToken)result.payload).ToString(),
                    Status = result.status
                };
            });
        }

        /// <summary>
        /// Returns device twin object
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns>Device information</returns>
        public Task<DeviceTwinModel> GetAsync(string twinId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"/twins/{twinId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                dynamic twin = JToken.Parse(response.Content);
                return new DeviceTwinModel {
                    Etag = twin.etag,
                    Id = twin.deviceId,
                    Tags = ((JObject)twin.tags).Children().ToDictionary(
                        p => ((JProperty)p).Name, p => ((JProperty)p).Value)
                };
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
                var request = NewRequest("/devices/query");
                if (continuation != null) {
                    request.Headers.Add(kContinuationKey, continuation);
                }
                request.SetContent(new {
                    query
                });
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                if (response.Headers.TryGetValues(kContinuationKey, out var values)) {
                    continuation = values.First();
                }
                var result = (JArray)JToken.Parse(response.Content);
                return new DeviceTwinListModel {
                    ContinuationToken = continuation,
                    Items = result.Select(twin => (dynamic)twin).Select(twin =>
                    new DeviceTwinModel {
                        Etag = twin.etag,
                        Id = twin.deviceId,
                        Tags = ((JObject)twin.tags).Children().ToDictionary(
                            p => ((JProperty)p).Name, p => ((JProperty)p).Value)
                    }).ToList()
                };
            });
        }

        /// <summary>
        /// Delete device twin
        /// </summary>
        /// <param name="twinId"></param>
        public Task DeleteAsync(string twinId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"/devices/{twinId}");
                request.Headers.Add("If-Match", @"""*""");
                var response = await _httpClient.DeleteAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Helper to create new request
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private HttpRequest NewRequest(string path) {
            var request = new HttpRequest();
            request.SetUriFromString(new UriBuilder {
                Scheme = "https",
                Host = _hubConnectionString.HostName,
                Path = path,
                Query = "api-version=" + kApiVersion
            }.ToString());
            request.Headers.Add(HttpRequestHeader.Authorization.ToString(),
                CreateSasToken(_hubConnectionString, 3600));
            request.Headers.Add(HttpRequestHeader.UserAgent.ToString(), kClientId);
            return request;
        }

        /// <summary>
        /// Create a token for iothub from connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="validityPeriodInSeconds"></param>
        /// <returns></returns>
        private static string CreateSasToken(ConnectionString connectionString,
            int validityPeriodInSeconds) {
            // http://msdn.microsoft.com/en-us/library/azure/dn170477.aspx
            // signature is computed from joined encoded request Uri string and expiry string
            var expiryTime = DateTime.UtcNow + TimeSpan.FromSeconds(validityPeriodInSeconds);
            var expiry = ((long)(expiryTime -
                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            var encodedScope = Uri.EscapeDataString(connectionString.HostName);
            // the connection string signature is base64 encoded
            var key = Convert.FromBase64String(connectionString.SharedAccessKey);
            using (var hmac = new HMACSHA256(key)) {
                var sig = Convert.ToBase64String(hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(encodedScope + "\n" + expiry)));
                return $"SharedAccessSignature sr={encodedScope}" +
                    $"&sig={Uri.EscapeDataString(sig)}&se={Uri.EscapeDataString(expiry)}" +
                    $"&skn={Uri.EscapeDataString(connectionString.SharedAccessKeyName)}";
            }
        }

        const string kApiVersion = "2016-11-14";
        const string kClientId = "OpcUaExplorer";
        const string kContinuationKey = "x-ms-continuation";

        private readonly ConnectionString _hubConnectionString;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
    }
}
