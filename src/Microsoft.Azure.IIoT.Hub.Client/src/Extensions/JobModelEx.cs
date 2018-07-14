// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.Devices;

    public static class JobModelEx {

        /// <summary>
        /// Convert response to model
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static JobModel ToModel(this JobResponse response) {
            return new JobModel {
                JobId = response.JobId,
                QueryCondition = response.QueryCondition,
                MethodParameter = response.CloudToDeviceMethod.ToModel(),
                UpdateTwin = response.UpdateTwin.ToModel(),
                EndTimeUtc = response.EndTimeUtc,
                FailureReason = response.FailureReason,
                MaxExecutionTimeInSeconds = response.MaxExecutionTimeInSeconds,
                Status = response.Status.ToModel(),
                StatusMessage = response.StatusMessage,
                StartTimeUtc = response.StartTimeUtc,
                Type = response.Type.ToModel()
            };
        }

        /// <summary>
        /// Convert type to model type
        /// </summary>
        /// <param name="jobType"></param>
        /// <returns></returns>
        public static JobType ToModel(this Devices.JobType jobType) {
            switch (jobType) {
                case Devices.JobType.ScheduleDeviceMethod:
                    return JobType.ScheduleDeviceMethod;
                case Devices.JobType.ScheduleUpdateTwin:
                    return JobType.ScheduleUpdateTwin;
                default:
                    return JobType.Unknown;
            }
        }

        /// <summary>
        /// Convert status to model status
        /// </summary>
        /// <param name="jobStatus"></param>
        /// <returns></returns>
        public static JobStatus ToModel(this Devices.JobStatus jobStatus) {
            switch (jobStatus) {
                case Devices.JobStatus.Cancelled:
                    return JobStatus.Cancelled;
                case Devices.JobStatus.Completed:
                    return JobStatus.Completed;
                case Devices.JobStatus.Enqueued:
                    return JobStatus.Enqueued;
                case Devices.JobStatus.Failed:
                    return JobStatus.Failed;
                case Devices.JobStatus.Queued:
                    return JobStatus.Queued;
                case Devices.JobStatus.Running:
                    return JobStatus.Running;
                case Devices.JobStatus.Scheduled:
                    return JobStatus.Scheduled;
                default:
                    return JobStatus.Unknown;
            }
        }
    }
}
