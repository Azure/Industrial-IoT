// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.Devices;

    /// <summary>
    /// Device job model extensions
    /// </summary>
    public static class DeviceJobModelEx {

        /// <summary>
        /// Convert device job to model
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public static DeviceJobModel ToModel(this DeviceJob job) {
            return new DeviceJobModel {
                CreatedDateTimeUtc = job.CreatedDateTimeUtc,
                DeviceId = job.DeviceId,
                EndTimeUtc = job.EndTimeUtc,
                Error = job.Error.ToModel(),
                LastUpdatedDateTimeUtc = job.LastUpdatedDateTimeUtc,
            //    ModuleId = job.ModuleId,  // TODO:
                Outcome = job.Outcome.ToModel(),
                StartTimeUtc = job.StartTimeUtc,
                Status = job.Status.ToModel()
            };
        }

        /// <summary>
        /// Convert status to model status
        /// </summary>
        /// <param name="jobStatus"></param>
        /// <returns></returns>
        public static DeviceJobStatus ToModel(this Devices.DeviceJobStatus jobStatus) {
            switch (jobStatus) {
                case Devices.DeviceJobStatus.Canceled:
                    return DeviceJobStatus.Cancelled;
                case Devices.DeviceJobStatus.Completed:
                    return DeviceJobStatus.Completed;
                case Devices.DeviceJobStatus.Failed:
                    return DeviceJobStatus.Failed;
                case Devices.DeviceJobStatus.Running:
                    return DeviceJobStatus.Running;
                case Devices.DeviceJobStatus.Scheduled:
                    return DeviceJobStatus.Scheduled;
                default:
                    return DeviceJobStatus.Pending;
            }
        }

        /// <summary>
        /// Convert error to model error
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static DeviceJobErrorModel ToModel(this DeviceJobError error) {
            return new DeviceJobErrorModel {
                Code = error.Code,
                Description = error.Description
            };
        }
    }
}
