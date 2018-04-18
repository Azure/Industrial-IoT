// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models {
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Invdividual execution of job on a device
    /// </summary>
    public class DeviceJobModel {

        /// <summary>
        /// Device id
        /// </summary>
        [JsonProperty(PropertyName = "DeviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Module Id
        /// </summary>
        [JsonProperty(PropertyName = "ModuleId")]
        public string ModuleId { get; set; }

        /// <summary>
        /// Job status
        /// </summary>
        [JsonProperty(PropertyName = "Status")]
        public DeviceJobStatus Status { get; set; }

        /// <summary>
        /// Start of job
        /// </summary>
        [JsonProperty(PropertyName = "StartTimeUtc")]
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// End of job
        /// </summary>
        [JsonProperty(PropertyName = "EndTimeUtc")]
        public DateTime EndTimeUtc { get; set; }

        /// <summary>
        /// Job creation time
        /// </summary>
        [JsonProperty(PropertyName = "CreatedDateTimeUtc")]
        public DateTime CreatedDateTimeUtc { get; set; }

        /// <summary>
        /// Last updated
        /// </summary>
        [JsonProperty(PropertyName = "LastUpdatedDateTimeUtc")]
        public DateTime LastUpdatedDateTimeUtc { get; set; }

        /// <summary>
        /// Result
        /// </summary>
        [JsonProperty(PropertyName = "Outcome",
            NullValueHandling = NullValueHandling.Ignore)]
        public MethodResultModel Outcome { get; set; }

        /// <summary>
        /// Error details
        /// </summary>
        [JsonProperty(PropertyName = "Error",
            NullValueHandling = NullValueHandling.Ignore)]
        public DeviceJobErrorModel Error { get; set; }
    }
}
