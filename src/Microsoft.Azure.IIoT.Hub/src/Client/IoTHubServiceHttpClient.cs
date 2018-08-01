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
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin and job services, talking to iot hub
    /// directly. Alternatively, there is a sdk based implementation
    /// in the Hub.Client nuget package that can also be used.
    /// </summary>
    public class IoTHubServiceHttpClient : IoTHubHttpClientBase,
        IIoTHubTwinServices, IIoTHubJobServices {

        /// <summary>
        /// The host name the client is talking to
        /// </summary>
        public string HostName => _hubConnectionString.HostName;

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTHubServiceHttpClient(IHttpClient httpClient,
            IIoTHubConfig config, ILogger logger) :
            base (httpClient, config, logger) {
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel twin,
            bool forceUpdate) {
            if (twin == null) {
                throw new ArgumentNullException(nameof(twin));
            }
            if (string.IsNullOrEmpty(twin.Id)) {
                throw new ArgumentNullException(nameof(twin.Id));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {

                if (string.IsNullOrEmpty(twin.Etag)) {
                    // First try create
                    try {
                        var device = NewRequest($"/devices/{twin.Id}");
                        device.SetContent(new {
                            deviceId = twin.Id,
                            capabilities = twin.Capabilities
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
                    var result = DeviceTwinModelEx.ToDeviceTwinModel(response.GetContentAsString());
                    _logger.Info($"{twin.Id} ({twin.ModuleId ?? ""}) created or updated " +
                        $"({twin.Etag ?? "*"} -> {result.Etag})", () => { });
                    return result;
                }
            });
        }

        /// <inheritdoc/>
        public Task<MethodResultModel> CallMethodAsync(string deviceId, string moduleId,
            MethodParameterModel parameters) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (parameters == null) {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (string.IsNullOrEmpty(parameters.Name)) {
                throw new ArgumentNullException(nameof(parameters.Name));
            }
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
                dynamic result = JToken.Parse(response.GetContentAsString());
                return new MethodResultModel {
                    JsonPayload = ((JToken)result.payload).ToString(),
                    Status = result.status
                };
            });
        }

        /// <inheritdoc/>
        public Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, JToken> properties, string etag) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                request.SetContent(new {
                    deviceId,
                    properties = new {
                        desired = properties ?? new Dictionary<string, JToken>()
                    }
                });
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.PatchAsync(request);
                response.Validate();
            });
        }

        /// <inheritdoc/>
        public Task ApplyConfigurationAsync(string deviceId,
            ConfigurationContentModel configuration) {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, null)}/applyConfigurationContent");
                request.SetContent(configuration);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
            });
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> GetAsync(string deviceId, string moduleId) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return DeviceTwinModelEx.ToDeviceTwinModel(response.GetContentAsString());
            });
        }

        /// <inheritdoc/>
        public Task<DeviceModel> GetRegistrationAsync(string deviceId, string moduleId) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return ToDeviceRegistrationModel(JToken.Parse(response.GetContentAsString()));
            });
        }

        /// <inheritdoc/>
        public Task<QueryResultModel> QueryAsync(string query, string continuation,
            int? pageSize) {
            if (string.IsNullOrEmpty(query)) {
                throw new ArgumentNullException(nameof(query));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest("/devices/query");
                if (continuation != null) {
                    request.Headers.Add(HttpHeader.ContinuationToken, continuation);
                }
                if (pageSize != null) {
                    request.Headers.Add(HttpHeader.MaxItemCount, pageSize.ToString());
                }
                request.SetContent(new {
                    query
                });
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                if (response.Headers.TryGetValues(HttpHeader.ContinuationToken, out var values)) {
                    continuation = values.First();
                }
                return new QueryResultModel {
                    ContinuationToken = continuation,
                    Result = JArray.Parse(response.GetContentAsString())
                };
            });
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string deviceId, string moduleId, string etag) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.DeleteAsync(request);
                response.Validate();
            });
        }

        /// <inheritdoc/>
        public async Task<JobModel> CreateAsync(JobModel job) {
            if (job == null) {
                throw new ArgumentNullException(nameof(job));
            }
            if (string.IsNullOrEmpty(job.JobId)) {
                throw new ArgumentNullException(nameof(job.JobId));
            }
            if (string.IsNullOrEmpty(job.QueryCondition)) {
                throw new ArgumentNullException(nameof(job.QueryCondition));
            }
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
                return ToJobModel(JToken.Parse(response.GetContentAsString()));
            });
            // Get device infos
            return await QueryDevicesInfoAsync(model);
        }

        /// <inheritdoc/>
        public async Task<JobModel> RefreshAsync(string jobId) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var model = await Retry.WithExponentialBackoff(_logger, async () => {
                var request = NewRequest($"/jobs/v2/{jobId}");
                var response = await _httpClient.GetAsync(request);
                response.Validate();
                return ToJobModel(JToken.Parse(response.GetContentAsString()));
            });
            // Get device infos
            return await QueryDevicesInfoAsync(model);
        }

        /// <inheritdoc/>
        public Task CancelAsync(string jobId) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
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
                    request.Headers.Add(HttpHeader.ContinuationToken, continuation);
                    continuation = null;
                }
                request.SetContent(new {
                    query = $"SELECT * FROM devices.jobs WHERE devices.jobs.jobId = '{job.JobId}'"
                });
                var response = await _httpClient.PostAsync(request);
                response.Validate();
                if (response.Headers.TryGetValues(HttpHeader.ContinuationToken, out var values)) {
                    continuation = values.First();
                }
                var result = (JArray)JToken.Parse(response.GetContentAsString());
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
    }
}
