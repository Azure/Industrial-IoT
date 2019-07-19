// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Invdividual execution of job on a device
    /// </summary>
    public class DeviceJobModel {

        /// <summary>
        /// Device id
        /// </summary>
        [JsonProperty(PropertyName = "deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Module Id
        /// </summary>
        [JsonProperty(PropertyName = "moduleId")]
        public string ModuleId { get; set; }

        /// <summary>
        /// Job status
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public DeviceJobStatus Status { get; set; }

        /// <summary>
        /// Start of job
        /// </summary>
        [JsonProperty(PropertyName = "startTimeUtc")]
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// End of job
        /// </summary>
        [JsonProperty(PropertyName = "endTimeUtc")]
        public DateTime EndTimeUtc { get; set; }

        /// <summary>
        /// Job creation time
        /// </summary>
        [JsonProperty(PropertyName = "createdDateTimeUtc")]
        public DateTime CreatedDateTimeUtc { get; set; }

        /// <summary>
        /// Last updated
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedDateTimeUtc")]
        public DateTime LastUpdatedDateTimeUtc { get; set; }

        /// <summary>
        /// Result
        /// </summary>
        [JsonProperty(PropertyName = "outcome",
            NullValueHandling = NullValueHandling.Ignore)]
        public MethodResultModel Outcome { get; set; }

        /// <summary>
        /// Error details
        /// </summary>
        [JsonProperty(PropertyName = "error",
            NullValueHandling = NullValueHandling.Ignore)]
        public DeviceJobErrorModel Error { get; set; }
    }
}
