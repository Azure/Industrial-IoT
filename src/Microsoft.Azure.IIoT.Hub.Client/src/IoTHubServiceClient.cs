// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Microsoft.Azure.Devices.Shared;
    using Serilog;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin and job services using service sdk.
    /// </summary>
    public sealed class IoTHubServiceClient : IIoTHubTwinServices, IIoTHubJobServices,
        IIoTHubConfigurationServices {

        /// <summary>
        /// The host name the client is talking to
        /// </summary>
        public string HostName { get; }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTHubServiceClient(IIoTHubConfig config, ILogger logger) {
            if (string.IsNullOrEmpty(config.IoTHubConnString)) {
                throw new ArgumentException(nameof(config));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _client = ServiceClient.CreateFromConnectionString(config.IoTHubConnString);
            _registry = RegistryManager.CreateFromConnectionString(config.IoTHubConnString);
            _jobs = JobClient.CreateFromConnectionString(config.IoTHubConnString);

            Task.WaitAll(_client.OpenAsync(), _registry.OpenAsync(), _jobs.OpenAsync());

            HostName = ConnectionString.Parse(config.IoTHubConnString).HostName;
        }

        /// <inheritdoc/>
        public async Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel twin,
            bool forceUpdate) {

            if (string.IsNullOrEmpty(twin.Etag)) {

                // First try create device
                try {
                    var device = await _registry.AddDeviceAsync(twin.ToDevice());
                }
                catch (DeviceAlreadyExistsException) {
                    // Expected for update
                }
                catch (Exception e) {
                    _logger.Debug(e, "Create device failed in CreateOrUpdate");
                }
            }

            try {

                Twin update;
                // Then update twin assuming it now exists. If fails, retry...
                var etag = string.IsNullOrEmpty(twin.Etag) || forceUpdate ? "*" : twin.Etag;
                if (!string.IsNullOrEmpty(twin.ModuleId)) {

                    if (string.IsNullOrEmpty(twin.Etag)) {
                        // Try create module
                        try {
                            var module = await _registry.AddModuleAsync(twin.ToModule());
                        }
                        catch (DeviceAlreadyExistsException) {
                            // Expected for update
                        }
                        catch (Exception e) {
                            _logger.Debug(e, "Create module failed in CreateOrUpdate");
                        }
                    }

                    update = await _registry.UpdateTwinAsync(twin.Id, twin.ModuleId,
                        twin.ToTwin(true), etag);
                }
                else {
                    // Patch device
                    update = await _registry.UpdateTwinAsync(twin.Id,
                        twin.ToTwin(true), etag);
                }
                return update.ToModel();
            }
            catch (Exception e) {
                _logger.Debug(e, "Create or update failed ");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<MethodResultModel> CallMethodAsync(string deviceId, string moduleId,
            MethodParameterModel parameters) {
            try {
                var methodInfo = new CloudToDeviceMethod(parameters.Name);
                methodInfo.SetPayloadJson(parameters.JsonPayload);
                var result = await (string.IsNullOrEmpty(moduleId) ?
                     _client.InvokeDeviceMethodAsync(deviceId, methodInfo) :
                     _client.InvokeDeviceMethodAsync(deviceId, moduleId, methodInfo));
                return new MethodResultModel {
                    JsonPayload = result.GetPayloadAsJson(),
                    Status = result.Status
                };
            }
            catch (Exception e) {
                _logger.Debug(e, "Call method failed ");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, JToken> properties, string etag) {
            try {
                var result = await (string.IsNullOrEmpty(moduleId) ?
                    _registry.UpdateTwinAsync(deviceId, properties.ToTwin(), etag) :
                    _registry.UpdateTwinAsync(deviceId, moduleId, properties.ToTwin(), etag));
            }
            catch (Exception e) {
                _logger.Debug(e, "Update properties failed ");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task ApplyConfigurationAsync(string deviceId,
            ConfigurationContentModel configuration) {
            try {
                await _registry.ApplyConfigurationContentOnDeviceAsync(deviceId,
                    configuration.ToContent());
            }
            catch (Exception e) {
                _logger.Debug(e, "Apply configuration failed ");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<DeviceTwinModel> GetAsync(string deviceId, string moduleId) {
            try {
                if (string.IsNullOrEmpty(moduleId)) {
                    var device = await _registry.GetTwinAsync(deviceId);
                    return device.ToModel();
                }
                var module = await _registry.GetTwinAsync(deviceId, moduleId);
                return module.ToModel();
            }
            catch (Exception e) {
                _logger.Debug(e, "Get twin failed ");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<DeviceModel> GetRegistrationAsync(string deviceId, string moduleId) {
            try {
                if (string.IsNullOrEmpty(moduleId)) {
                    var device = await _registry.GetDeviceAsync(deviceId);
                    return device.ToModel();
                }
                var module = await _registry.GetModuleAsync(deviceId, moduleId);
                return module.ToModel();
            }
            catch (Exception e) {
                _logger.Debug(e, "Get registration failed ");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<QueryResultModel> QueryAsync(string query, string continuation,
            int? pageSize) {
            try {
                var statement = _registry.CreateQuery(query, pageSize);
                var options = new QueryOptions { ContinuationToken = continuation };
                var result = await statement.GetNextAsJsonAsync(options);
                return new QueryResultModel {
                    ContinuationToken = result.ContinuationToken,
                    Result = new JArray(result.Select(JToken.Parse))
                };
            }
            catch (Exception e) {
                _logger.Debug(e, "Query failed ");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string deviceId, string moduleId, string etag) {
            try {
                await (string.IsNullOrEmpty(moduleId) ?
                    _registry.RemoveDeviceAsync(new Device(deviceId) {
                        ETag = etag ?? "*"
                    }) :
                    _registry.RemoveModuleAsync(new Module(deviceId, moduleId) {
                        ETag = etag ?? "*"
                    }));
            }
            catch (Exception e) {
                _logger.Debug(e, "Delete failed ");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<ConfigurationModel> CreateOrUpdateConfigurationAsync(
            ConfigurationModel configuration, bool forceUpdate) {

            if (string.IsNullOrEmpty(configuration.Etag)) {
                // First try create configuration
                try {
                    var result = await _registry.AddConfigurationAsync(
                        configuration.ToConfiguration());
                    return result.ToModel();
                }
                catch (DeviceAlreadyExistsException) { // TODO
                    // Expected for update
                }
                catch (Exception e) {
                    _logger.Debug(e,
                        "Create configuration failed in CreateOrUpdate");
                    // Try patch
                }
            }
            try {
                // Try update configuration
                var result = await _registry.UpdateConfigurationAsync(
                    configuration.ToConfiguration());
                return result.ToModel();
            }
            catch (Exception e) {
                _logger.Debug(e,
                    "Update configuration failed in CreateOrUpdate");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<ConfigurationModel> GetConfigurationAsync(
            string configurationId) {
            try {
                var configuration = await _registry.GetConfigurationAsync(
                    configurationId);
                return configuration.ToModel();
            }
            catch (Exception e) {
                _logger.Debug(e, "Get configuration failed");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationModel>> ListConfigurationsAsync(
            int? maxCount) {
            try {
                var configurations = await _registry.GetConfigurationsAsync(
                    maxCount ?? int.MaxValue);
                return configurations.Select(c => c.ToModel());
            }
            catch (Exception e) {
                _logger.Debug(e, "List configurations failed");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteConfigurationAsync(string configurationId,
            string etag) {
            try {
                if (string.IsNullOrEmpty(etag)) {
                    await _registry.RemoveConfigurationAsync(configurationId);
                }
                else {
                    await _registry.RemoveConfigurationAsync(
                        new Configuration(configurationId) { ETag = etag });
                }
            }
            catch (Exception e) {
                _logger.Debug(e, "Delete configuration failed");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<JobModel> CreateAsync(JobModel job) {
            JobResponse response;
            switch (job.Type) {
                case Models.JobType.ScheduleUpdateTwin:
                    response = await _jobs.ScheduleTwinUpdateAsync(job.JobId, job.QueryCondition,
                        job.UpdateTwin?.ToTwin(),
                        job.StartTimeUtc ?? DateTime.MinValue,
                        job.MaxExecutionTimeInSeconds ?? long.MaxValue);
                    break;
                case Models.JobType.ScheduleDeviceMethod:
                    response = await _jobs.ScheduleDeviceMethodAsync(job.JobId, job.QueryCondition,
                        job.MethodParameter?.ToCloudToDeviceMethod(),
                        job.StartTimeUtc ?? DateTime.MinValue,
                        job.MaxExecutionTimeInSeconds ?? long.MaxValue);
                    break;
                default:
                    throw new ArgumentException(nameof(job.Type));
            }
            // Get device infos
            return await QueryDevicesInfoAsync(response.ToModel());
        }

        /// <inheritdoc/>
        public async Task<JobModel> RefreshAsync(string jobId) {
            var response = await _jobs.GetJobAsync(jobId);
            // Get device infos
            return await QueryDevicesInfoAsync(response.ToModel());
        }

        /// <inheritdoc/>
        public Task CancelAsync(string jobId) => _jobs.CancelJobAsync(jobId);

        /// <summary>
        /// Fill in individual device responses
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private async Task<JobModel> QueryDevicesInfoAsync(JobModel job) {
            var query = $"SELECT * FROM devices.jobs WHERE devices.jobs.jobId = '{job.JobId}'";
            var statement = _registry.CreateQuery(query);
            string continuation = null;
            var devices = new List<DeviceJobModel>();
            do {
                var response = await statement.GetNextAsDeviceJobAsync(new QueryOptions {
                    ContinuationToken = continuation
                });
                devices.AddRange(response.Select(j => j.ToModel()));
                continuation = response.ContinuationToken;
            }
            while (!string.IsNullOrEmpty(continuation));
            job.Devices = devices;
            return job;
        }

        private readonly ServiceClient _client;
        private readonly RegistryManager _registry;
        private readonly JobClient _jobs;
        private readonly ILogger _logger;
    }
}
