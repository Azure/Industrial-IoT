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
        [JsonProperty(PropertyName = "JobId")]
        public string JobId { get; set; }

        /// <summary>
        /// Query
        /// </summary>
        [JsonProperty(PropertyName = "QueryCondition",
            NullValueHandling = NullValueHandling.Ignore)]
        public string QueryCondition { get; set; }

        /// <summary>
        /// Method parameters if type is device method
        /// </summary>
        [JsonProperty(PropertyName = "MethodParameter",
            NullValueHandling = NullValueHandling.Ignore)]
        public MethodParameterModel MethodParameter { get; set; }

        /// <summary>
        /// Twin if type is twin update
        /// </summary>
        [JsonProperty(PropertyName = "UpdateTwin",
            NullValueHandling = NullValueHandling.Ignore)]
        public DeviceTwinModel UpdateTwin { get; set; }

        /// <summary>
        /// Scheduled start time
        /// </summary>
        [JsonProperty(PropertyName = "EndTimeUtc",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTimeUtc { get; set; }

        /// <summary>
        /// Scheduled end time
        /// </summary>
        [JsonProperty(PropertyName = "EndTimeUtc",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTimeUtc { get; set; }

        /// <summary>
        /// Or max execution time
        /// </summary>
        [JsonProperty(PropertyName = "MaxExecutionTimeInSeconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxExecutionTimeInSeconds { get; set; }

        /// <summary>
        /// Type of the job
        /// </summary>
        [JsonProperty(PropertyName = "Type")]
        public JobType Type { get; set; }

        /// <summary>
        /// Status of the job
        /// </summary>
        [JsonProperty(PropertyName = "Status")]
        public JobStatus Status { get; set; }

        /// <summary>
        /// Failure reason if status is error
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string FailureReason { get; set; }

        /// <summary>
        /// Status message
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Individual job results
        /// </summary>
        [JsonProperty(PropertyName = "Devices",
            NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<DeviceJobModel> Devices { get; set; }
    }
}
