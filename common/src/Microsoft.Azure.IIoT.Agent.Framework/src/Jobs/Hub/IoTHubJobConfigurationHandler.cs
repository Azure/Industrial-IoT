// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using System;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// IoT hub based job event handler
    /// </summary>
    public class IoTHubJobConfigurationHandler : IJobEventHandler {

        /// <summary>
        /// Create event handler
        /// </summary>
        /// <param name="ioTHubTwinServices"></param>
        /// <param name="logger"></param>
        public IoTHubJobConfigurationHandler(IIoTHubTwinServices ioTHubTwinServices,
            ILogger logger) {
            _ioTHubTwinServices = ioTHubTwinServices;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task OnJobCreatedAsync(IJobService manager, JobInfoModel job) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task OnJobCreatingAsync(IJobService manager, JobInfoModel job) {
            if (job.JobConfiguration?.IsObject != true) {
                return;
            }
            try {
                var jobDeviceId = GetJobDeviceId(job);
                var deviceTwin = await _ioTHubTwinServices.FindAsync(jobDeviceId);
                if (deviceTwin == null) {
                    deviceTwin = new DeviceTwinModel {
                        Id = jobDeviceId
                    };
                    await _ioTHubTwinServices.CreateOrUpdateAsync(deviceTwin, true);
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Error while creating IoT Device.");
            }
        }

        /// <inheritdoc/>
        public async Task OnJobAssignmentAsync(IJobService manager, JobInfoModel job, string workerId) {
            if (job.JobConfiguration?.IsObject != true) {
                return;
            }
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException("empty WorkerId provided");
            }
            try {
                var edgeDeviceTwin = await _ioTHubTwinServices.FindAsync(workerId.Split("_publisher")[0]);
                if (edgeDeviceTwin == null) {
                    _logger.Error("IoT Edge Device not found.");
                    return;
                }

                var jobDeviceId = GetJobDeviceId(job);
                var deviceTwin = await _ioTHubTwinServices.FindAsync(jobDeviceId);
                if (deviceTwin == null) {
                    deviceTwin = new DeviceTwinModel {
                        Id = jobDeviceId,
                        DeviceScope = edgeDeviceTwin.DeviceScope
                    };
                    await _ioTHubTwinServices.CreateOrUpdateAsync(deviceTwin, true);
                }
                else {
                    if (deviceTwin.DeviceScope != edgeDeviceTwin.DeviceScope) {
                        deviceTwin.DeviceScope = edgeDeviceTwin.DeviceScope;
                        await _ioTHubTwinServices.CreateOrUpdateAsync(deviceTwin, true);
                    }
                }
                var cs = await _ioTHubTwinServices.GetConnectionStringAsync(deviceTwin.Id);
                job.JobConfiguration[TwinProperties.ConnectionString].AssignValue(cs.ToString());
                _logger.Debug("Added connection string to job {id}", jobDeviceId);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Error while assigning the Job's IoT Device.");
            }
        }

        /// <inheritdoc/>
        public Task OnJobDeletingAsync(IJobService manager, JobInfoModel job) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task OnJobDeletedAsync(IJobService manager, JobInfoModel job) {
            var jobDeviceId = GetJobDeviceId(job);
            try {
                await _ioTHubTwinServices.DeleteAsync(jobDeviceId);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to delete device job {id}", jobDeviceId);
            }
        }

        /// <summary>
        /// Create job device identifier
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private static string GetJobDeviceId(JobInfoModel job) {
            return $"{job.JobConfigurationType}_{job.Id}";
        }

        private readonly IIoTHubTwinServices _ioTHubTwinServices;
        private readonly ILogger _logger;
    }
}