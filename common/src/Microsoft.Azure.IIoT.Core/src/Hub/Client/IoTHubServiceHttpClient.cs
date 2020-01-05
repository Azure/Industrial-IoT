// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Implementation of twin and job services, talking to iot hub
    /// directly. Alternatively, there is a sdk based implementation
    /// in the Hub.Client nuget package that can also be used.
    /// </summary>
    public sealed class IoTHubServiceHttpClient : IoTHubHttpClientBase,
        IIoTHubTwinServices, IIoTHubJobServices, IHealthCheck {

        /// <summary>
        /// The host name the client is talking to
        /// </summary>
        public string HostName => HubConnectionString.HostName;

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTHubServiceHttpClient(IHttpClient httpClient,
            IIoTHubConfig config, ILogger logger) :
            base(httpClient, config, logger) {
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken ct) {
            try {
                await QueryAsync("SELECT * FROM devices", null, 1, ct);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex) {
                return new HealthCheckResult(context.Registration.FailureStatus,
                    exception: ex);
            }
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> CreateAsync(DeviceTwinModel twin, bool force,
            CancellationToken ct) {
            if (twin == null) {
                throw new ArgumentNullException(nameof(twin));
            }
            if (string.IsNullOrEmpty(twin.Id)) {
                throw new ArgumentNullException(nameof(twin.Id));
            }
            // Retry transient errors
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                // First try create device
                try {
                    var device = NewRequest($"/devices/{twin.Id}");
                    device.SetContent(new {
                        deviceId = twin.Id,
                        capabilities = twin.Capabilities
                    });
                    var response = await _httpClient.PutAsync(device, ct);
                    response.Validate();
                }
                catch (ConflictingResourceException)
                    when (!string.IsNullOrEmpty(twin.ModuleId) || force) {
                    // Continue onward
                }
                catch (Exception e) {
                    _logger.Debug(e, "Create device failed in CreateOrUpdate");
                }
                if (!string.IsNullOrEmpty(twin.ModuleId)) {
                    // Try create module
                    try {
                        var module = NewRequest(
                            $"/devices/{twin.Id}/modules/{twin.ModuleId}");
                        module.SetContent(new {
                            deviceId = twin.Id,
                            moduleId = twin.ModuleId
                        });
                        var response = await _httpClient.PutAsync(module, ct);
                        response.Validate();
                    }
                    catch (ConflictingResourceException)
                        when (force) {
                    }
                    catch (Exception e) {
                        _logger.Debug(e, "Create module failed in CreateOrUpdate");
                    }
                }
                return await PatchAsync(twin, true, ct);  // Force update of twin
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> PatchAsync(DeviceTwinModel twin, bool force, CancellationToken ct) {
            if (twin == null) {
                throw new ArgumentNullException(nameof(twin));
            }
            if (string.IsNullOrEmpty(twin.Id)) {
                throw new ArgumentNullException(nameof(twin.Id));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {

                // Then update twin assuming it now exists. If fails, retry...
                var patch = NewRequest(
                    $"/twins/{ToResourceId(twin.Id, twin.ModuleId)}");
                patch.Headers.Add("If-Match",
                     $"\"{(string.IsNullOrEmpty(twin.Etag) || force ? "*" : twin.Etag)}\"");
                if (!string.IsNullOrEmpty(twin.ModuleId)) {

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
                    var response = await _httpClient.PatchAsync(patch, ct);
                    response.Validate();
                    var result = response.GetContent<DeviceTwinModel>();
                    _logger.Information(
                        "{id} ({moduleId}) created or updated ({twinEtag} -> {resultEtag})",
                        twin.Id, twin.ModuleId ?? string.Empty, twin.Etag ?? "*", result.Etag);
                    return result;
                }
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<MethodResultModel> CallMethodAsync(string deviceId, string moduleId,
            MethodParameterModel parameters, CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (parameters == null) {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (string.IsNullOrEmpty(parameters.Name)) {
                throw new ArgumentNullException(nameof(parameters.Name));
            }
            var request = NewRequest(
                $"/twins/{ToResourceId(deviceId, moduleId)}/methods");

            request.SetContent(new {
                methodName = parameters.Name,
                // TODO: Add timeouts...
                // responseTimeoutInSeconds = ...
                payload = JToken.Parse(parameters.JsonPayload)
            });
            var response = await _httpClient.PostAsync(request, ct);
            response.Validate();
            dynamic result = JToken.Parse(response.GetContentAsString());
            return new MethodResultModel {
                JsonPayload = ((JToken)result.payload).ToString(),
                Status = result.status
            };
        }

        /// <inheritdoc/>
        public Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, JToken> properties, string etag, CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
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
                var response = await _httpClient.PatchAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task ApplyConfigurationAsync(string deviceId,
            ConfigurationContentModel configuration, CancellationToken ct) {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, null)}/applyConfigurationContent");
                request.SetContent(configuration);
                var response = await _httpClient.PostAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> GetAsync(string deviceId, string moduleId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {

                try {
                    var request = NewRequest(
                   $"/twins/{ToResourceId(deviceId, moduleId)}");
                    var response = await _httpClient.GetAsync(request, ct);
                    response.Validate();
                    return response.GetContent<DeviceTwinModel>();
                }
                catch (ResourceNotFoundException) {
                    return null;
                }
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceModel> GetRegistrationAsync(string deviceId, string moduleId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request, ct);
                response.Validate();
                return ToDeviceRegistrationModel(JToken.Parse(response.GetContentAsString()));
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<QueryResultModel> QueryAsync(string query, string continuation,
            int? pageSize, CancellationToken ct) {
            if (string.IsNullOrEmpty(query)) {
                throw new ArgumentNullException(nameof(query));
            }
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
            var response = await _httpClient.PostAsync(request, ct);
            response.Validate();
            if (response.Headers.TryGetValues(HttpHeader.ContinuationToken, out var values)) {
                continuation = values.First();
            }
            return new QueryResultModel {
                ContinuationToken = continuation,
                Result = JArray.Parse(response.GetContentAsString())
            };
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string deviceId, string moduleId, string etag,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            etag = null; // TODO : Fix - Currently prevents internal server error
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.DeleteAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<JobModel> CreateAsync(JobModel job, CancellationToken ct) {
            if (job == null) {
                throw new ArgumentNullException(nameof(job));
            }
            if (string.IsNullOrEmpty(job.JobId)) {
                throw new ArgumentNullException(nameof(job.JobId));
            }
            if (string.IsNullOrEmpty(job.QueryCondition)) {
                throw new ArgumentNullException(nameof(job.QueryCondition));
            }
            var model = await Retry.WithExponentialBackoff(_logger, ct, async () => {
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
                            startTime = ToIso8601String(job.StartTimeUtc),
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
                            startTime = ToIso8601String(job.StartTimeUtc),
                            maxExecutionTimeInSeconds = job.MaxExecutionTimeInSeconds ??
                                long.MaxValue
                        });
                        break;
                    default:
                        throw new ArgumentException(nameof(job.Type));
                }
                var response = await _httpClient.PutAsync(request, ct);
                response.Validate();
                return ToJobModel(JToken.Parse(response.GetContentAsString()));
            }, kMaxRetryCount);
            // Get device infos
            return await QueryDevicesInfoAsync(model, ct);
        }

        /// <inheritdoc/>
        public async Task<JobModel> RefreshAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var model = await Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest($"/jobs/v2/{jobId}");
                var response = await _httpClient.GetAsync(request, ct);
                response.Validate();
                return ToJobModel(JToken.Parse(response.GetContentAsString()));
            }, kMaxRetryCount);
            // Get device infos
            return await QueryDevicesInfoAsync(model, ct);
        }

        /// <inheritdoc/>
        public Task CancelAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest($"/jobs/v2/{jobId}/cancel");
                var response = await _httpClient.PostAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <summary>
        /// Fill in individual device responses
        /// </summary>
        /// <param name="job"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<JobModel> QueryDevicesInfoAsync(JobModel job, CancellationToken ct) {
            var devices = new List<DeviceJobModel>();
            string continuation = null;
            do {
                continuation = await QueryDevicesInfoAsync(job, devices, continuation, ct);
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
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<string> QueryDevicesInfoAsync(JobModel job,
            List<DeviceJobModel> devices, string continuation, CancellationToken ct) {
            await Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest("/devices/query");
                if (continuation != null) {
                    request.Headers.Add(HttpHeader.ContinuationToken, continuation);
                    continuation = null;
                }
                request.SetContent(new {
                    query = $"SELECT * FROM devices.jobs WHERE devices.jobs.jobId = '{job.JobId}'"
                });
                var response = await _httpClient.PostAsync(request, ct);
                response.Validate();
                if (response.Headers.TryGetValues(HttpHeader.ContinuationToken, out var values)) {
                    continuation = values.First();
                }
                var result = (JArray)JToken.Parse(response.GetContentAsString());
                devices.AddRange(result.Select(ToDeviceJobModel));
            }, kMaxRetryCount);
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
            else if (status == DeviceJobStatus.Completed &&
                type == JobType.ScheduleDeviceMethod) {
                model.Outcome = new MethodResultModel {
                    JsonPayload = ((JObject)result.outcome.deviceMethodResponse.payload)
                        .ToString(),
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
                Authentication = new DeviceAuthenticationModel {
                    PrimaryKey = result.authentication.symmetricKey.primaryKey,
                    SecondaryKey = result.authentication.symmetricKey.secondaryKey
                }
            };
        }

        /// <summary>
        /// Convert time to iso string
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static string ToIso8601String(DateTime? datetime) {
            return (datetime ?? DateTime.MinValue)
                .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK");
        }
    }
}
