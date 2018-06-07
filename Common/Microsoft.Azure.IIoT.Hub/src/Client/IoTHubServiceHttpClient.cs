// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin and job services, talking to iot hub directly.
    /// This is intended for stand alone testing, not for production.
    /// See samples for details.
    /// </summary>
    public class IoTHubServiceHttpClient : IIoTHubTwinServices, IIoTHubJobServices {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTHubServiceHttpClient(IHttpClient httpClient,
            IIoTHubConfig config, ILogger logger) {
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
        /// <returns>Updated etag</returns>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel twin,
            bool forceUpdate) {
            return Retry.WithExponentialBackoff(_logger, async () => {

                if (string.IsNullOrEmpty(twin.Etag)) {
                    // First try create
                    try {
                        var device = NewRequest($"/devices/{twin.Id}");
                        device.SetContent(new {
                            deviceId = twin.Id
                        });
                        var response = await _httpClient.PutAsync(device);
                        response.Validate();
                    }
                    catch (ConflictingResourceException) {
                        // Expected for update
                    }
                    catch (Exception e) {
                        _logger.Debug("Create device failed in CreateOrUpdate", () => e);
                    }
                }

                // Then update twin assuming it now exists. If fails, retry...
                var patch = NewRequest(
                    $"/twins/{ToResourceId(twin.Id, twin.ModuleId)}");
                patch.Headers.Add("If-Match",
                     $"\"{(string.IsNullOrEmpty(twin.Etag) || forceUpdate ? "*" : twin.Etag)}\"");
                if (!string.IsNullOrEmpty(twin.ModuleId)) {

                    if (string.IsNullOrEmpty(twin.Etag)) {
                        // Try create module
                        try {
                            var module = NewRequest(
                                $"/devices/{twin.Id}/modules/{twin.ModuleId}");
                            module.SetContent(new {
                                deviceId = twin.Id,
                                moduleId = twin.ModuleId
                            });
                            var response = await _httpClient.PutAsync(module);
                            response.Validate();
                        }
                        catch (ConflictingResourceException) {
                            // Expected for update
                        }
                        catch (Exception e) {
                            _logger.Debug("Create module failed in CreateOrUpdate", () => e);
                        }
                    }

                    // Patch module
                    patch.SetContent(new {
                        deviceId = twin.Id,
                        moduleId = twin.ModuleId,
                        tags = twin.Tags ?? new Dictionary<string, JToken>(),
                        properties = new {
                            desired = twin.Properties?.Desired ?? new Dictionary<string, JToken>()
                        }
                    });
                }
                else {
                    // Patch device
                    patch.SetContent(new {
                        deviceId = twin.Id,
                        tags = twin.Tags ?? new Dictionary<string, JToken>(),
                        properties = new {
                            desired = twin.Properties?.Desired ?? new Dictionary<string, JToken>()
                        }
                    });
                }
                {
                    var response = await _httpClient.PatchAsync(patch);
                    response.Validate();
                    var result = DeviceTwinModelEx.ToDeviceTwinModel(response.Content);
                    _logger.Info($"{twin.Id} ({twin.ModuleId ?? ""}) created or updated " +
                        $"({twin.Etag ?? "*"} -> {result.Etag})", () => { });
                    return result;
                }
            });
        }

        /// <summary>
        /// Call method on device
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Task<MethodResultModel> CallMethodAsync(string deviceId, string moduleId,
            MethodParameterModel parameters) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}/methods");

                request.SetContent(new {
                    methodName = parameters.Name,
                    // TODO: Add timeouts...
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
        /// Update device properties through twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, JToken> properties, string etag) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                request.SetContent(new {
                    deviceId,
                    properties = new {
                        desired = properties
                    }
                });
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.PatchAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Returns device twin object
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns>Device information</returns>
        public Task<DeviceTwinModel> GetAsync(string deviceId, string moduleId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return DeviceTwinModelEx.ToDeviceTwinModel(response.Content);
            });
        }

        /// <summary>
        /// Returns device registration object
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns>Device information</returns>
        public Task<DeviceModel> GetRegistrationAsync(string deviceId, string moduleId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return ToDeviceRegistrationModel(JToken.Parse(response.Content));
            });
        }

        /// <summary>
        /// Return raw query response
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public Task<QueryResultModel> QueryAsync(string query, string continuation,
            int? pageSize) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest("/devices/query");
                if (continuation != null) {
                    request.Headers.Add(kContinuationKey, continuation);
                }
                if (pageSize != null) {
                    request.Headers.Add(kPageSizeKey, pageSize.ToString());
                }
                request.SetContent(new {
                    query
                });
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                if (response.Headers.TryGetValues(kContinuationKey, out var values)) {
                    continuation = values.First();
                }
                return new QueryResultModel {
                    ContinuationToken = continuation,
                    Result = JArray.Parse(response.Content)
                };
            });
        }

        /// <summary>
        /// Delete device or module twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public Task DeleteAsync(string deviceId, string moduleId, string etag) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.DeleteAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Create job
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public async Task<JobModel> CreateAsync(JobModel job) {
            var model = await Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"/jobs/v2/{job.JobId}");
                switch (job.Type) {
                    case JobType.ScheduleUpdateTwin:
                        request.SetContent(new {
                            jobId = job.JobId,
                            type = "scheduleUpdateTwin",
                            queryCondition = job.QueryCondition,
                            updateTwin = new {
                                desiredProperties = new {
                                    tags = job.UpdateTwin?.Tags,
                                    properties = job.UpdateTwin?.Properties?.Desired
                                }
                            },
                            startTime = (job.StartTimeUtc ?? DateTime.MinValue).ToIso8601String(),
                            maxExecutionTimeInSeconds = job.MaxExecutionTimeInSeconds ??
                                long.MaxValue
                        });
                        break;
                    case JobType.ScheduleDeviceMethod:
                        request.SetContent(new {
                            jobId = job.JobId,
                            type = "scheduleDeviceMethod",
                            queryCondition = job.QueryCondition,
                            cloudToDeviceMethod = new {
                                methodName = job.MethodParameter.Name,
                                // responseTimeoutInSeconds = ...
                                payload = JToken.Parse(job.MethodParameter.JsonPayload)
                            },
                            startTime = (job.StartTimeUtc ?? DateTime.MinValue).ToIso8601String(),
                            maxExecutionTimeInSeconds = job.MaxExecutionTimeInSeconds ??
                                long.MaxValue
                        });
                        break;
                    default:
                        throw new ArgumentException(nameof(job.Type));
                }
                var response = await _httpClient.PutAsync(request);
                response.Validate();
                return ToJobModel(JToken.Parse(response.Content));
            });
            // Get device infos
            return await QueryDevicesInfoAsync(model);
        }

        /// <summary>
        /// Refresh
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public async Task<JobModel> RefreshAsync(string jobId) {
            var model = await Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"/jobs/v2/{jobId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return ToJobModel(JToken.Parse(response.Content));
            });
            // Get device infos
            return await QueryDevicesInfoAsync(model);
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public Task CancelAsync(string jobId) {
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"/jobs/v2/{jobId}/cancel");
                var response = await _httpClient.PostAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Fill in individual device responses
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private async Task<JobModel> QueryDevicesInfoAsync(JobModel job) {
            var devices = new List<DeviceJobModel>();
            string continuation = null;
            do {
                continuation = await QueryDevicesInfoAsync(job, devices, continuation);
            }
            while (!string.IsNullOrEmpty(continuation));
            job.Devices = devices;
            return job;
        }

        /// <summary>
        /// Query one page of responses
        /// </summary>
        /// <param name="job"></param>
        /// <param name="devices"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        private async Task<string> QueryDevicesInfoAsync(JobModel job,
            List<DeviceJobModel> devices, string continuation) {
            await Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest("/devices/query");
                if (continuation != null) {
                    request.Headers.Add(kContinuationKey, continuation);
                    continuation = null;
                }
                request.SetContent(new {
                    query = $"SELECT * FROM devices.jobs WHERE devices.jobs.jobId = '{job.JobId}'"
                });
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                if (response.Headers.TryGetValues(kContinuationKey, out var values)) {
                    continuation = values.First();
                }
                var result = (JArray)JToken.Parse(response.Content);
                devices.AddRange(result.Select(ToDeviceJobModel));
            });
            return continuation;
        }

        /// <summary>
        /// Convert json to twin
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static JobModel ToJobModel(dynamic result) {
            Enum.TryParse<JobStatus>((string)result.status, true, out var status);
            Enum.TryParse<JobType>((string)result.type, true, out var type);
            return new JobModel {
                JobId = result.jobId,
                QueryCondition = result.queryCondition,
                MaxExecutionTimeInSeconds = result.maxExecutionTimeInSeconds,
                EndTimeUtc = result.endTime,
                StartTimeUtc = result.startTime,
                FailureReason = result.failureReason,
                StatusMessage = result.statusMessage,
                Status = status,
                Type = type
            };
        }

        /// <summary>
        /// Convert json to twin
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static DeviceJobModel ToDeviceJobModel(dynamic result) {
            Enum.TryParse<JobType>((string)result.jobType, true, out var type);
            Enum.TryParse<DeviceJobStatus>((string)result.status, true, out var status);
            var model = new DeviceJobModel {
                DeviceId = result.deviceId,
                ModuleId = result.moduleId,
                CreatedDateTimeUtc = result.createdDateTimeUtc,
                LastUpdatedDateTimeUtc = result.lastUpdatedDateTimeUtc,
                StartTimeUtc = result.startTimeUtc,
                EndTimeUtc = result.endTimeUtc,
                Status = status
            };
            if (result.Error != null) {
                model.Error = new DeviceJobErrorModel {
                    Code = result.error.code,
                    Description = result.error.description
                };
            }
            else if (status == DeviceJobStatus.Completed && type == JobType.ScheduleDeviceMethod) {
                model.Outcome = new MethodResultModel {
                    JsonPayload = ((JObject)result.outcome.deviceMethodResponse.payload).ToString(),
                    Status = result.outcome.deviceMethodResponse.status
                };
            }
            return model;
        }

        /// <summary>
        /// Convert json to registration
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static DeviceModel ToDeviceRegistrationModel(dynamic result) {
            return new DeviceModel {
                Etag = result.etag,
                Id = result.deviceId,
                ModuleId = result.moduleId,
                Enabled = result.status != "disabled",
                Connected = result.connectionState == "connected",
                Authentication = new DeviceAuthenticationModel {
                    PrimaryKey = result.authentication.symmetricKey.primaryKey,
                    SecondaryKey = result.authentication.symmetricKey.secondaryKey
                }
            };
        }

        /// <summary>
        /// Helper to create new request
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private IHttpRequest NewRequest(string path) {
            var request = _httpClient.NewRequest(new UriBuilder {
                Scheme = "https",
                Host = _hubConnectionString.HostName,
                Path = path,
                Query = "api-version=" + kApiVersion
            }.Uri);
            request.Headers.Add(HttpRequestHeader.Authorization.ToString(),
                CreateSasToken(_hubConnectionString, 3600));
            request.Headers.Add(HttpRequestHeader.UserAgent.ToString(), kClientId);
            return request;
        }

        /// <summary>
        /// Helper to create resource path for device and optional module
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        private static string ToResourceId(string deviceId, string moduleId) =>
            string.IsNullOrEmpty(moduleId) ? deviceId : $"{deviceId}/modules/{moduleId}";

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

        const string kApiVersion = "2018-03-01-preview"; // Configuration preview
        const string kClientId = "OpcTwin";
        const string kContinuationKey = "x-ms-continuation";
        const string kPageSizeKey = "x-ms-max-item-count";

        private readonly ConnectionString _hubConnectionString;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
    }
}
