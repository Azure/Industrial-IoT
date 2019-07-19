// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a scheduled job
    /// </summary>
    public class JobModel {

        /// <summary>
        /// Id of the job
        /// </summary>
        [JsonProperty(PropertyName = "jobId")]
        public string JobId { get; set; }

        /// <summary>
        /// Query
        /// </summary>
        [JsonProperty(PropertyName = "queryCondition",
            NullValueHandling = NullValueHandling.Ignore)]
        public string QueryCondition { get; set; }

        /// <summary>
        /// Method parameters if type is device method
        /// </summary>
        [JsonProperty(PropertyName = "methodParameter",
            NullValueHandling = NullValueHandling.Ignore)]
        public MethodParameterModel MethodParameter { get; set; }

        /// <summary>
        /// Twin if type is twin update
        /// </summary>
        [JsonProperty(PropertyName = "updateTwin",
            NullValueHandling = NullValueHandling.Ignore)]
        public DeviceTwinModel UpdateTwin { get; set; }

        /// <summary>
        /// Scheduled start time
        /// </summary>
        [JsonProperty(PropertyName = "startTimeUtc",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTimeUtc { get; set; }

        /// <summary>
        /// Scheduled end time
        /// </summary>
        [JsonProperty(PropertyName = "endTimeUtc",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTimeUtc { get; set; }

        /// <summary>
        /// Or max execution time
        /// </summary>
        [JsonProperty(PropertyName = "maxExecutionTimeInSeconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxExecutionTimeInSeconds { get; set; }

        /// <summary>
        /// Type of the job
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public JobType Type { get; set; }

        /// <summary>
        /// Status of the job
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public JobStatus Status { get; set; }

        /// <summary>
        /// Failure reason if status is error
        /// </summary>
        [JsonProperty(PropertyName = "failureReason",
            NullValueHandling = NullValueHandling.Ignore)]
        public string FailureReason { get; set; }

        /// <summary>
        /// Status message
        /// </summary>
        [JsonProperty(PropertyName = "statusMessage",
            NullValueHandling = NullValueHandling.Ignore)]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Individual job results
        /// </summary>
        [JsonProperty(PropertyName = "devices",
            NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<DeviceJobModel> Devices { get; set; }
    }
}
