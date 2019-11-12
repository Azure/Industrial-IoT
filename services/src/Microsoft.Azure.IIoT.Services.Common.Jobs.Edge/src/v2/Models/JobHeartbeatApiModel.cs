// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Job heartbeat
    /// </summary>
    public class JobHeartbeatApiModel {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public JobHeartbeatModel ToServiceModel() {
            return new JobHeartbeatModel {
                State = State?.DeepClone(),
                ProcessMode = ProcessMode,
                JobHash = JobHash,
                JobId = JobId,
                Status = Status
            };
        }

        /// <summary>
        /// Job id
        /// </summary>
        [JsonProperty(PropertyName = "jobId")]
        [Required]
        public string JobId { get; set; }

        /// <summary>
        /// Hash
        /// </summary>
        [JsonProperty(PropertyName = "jobHash",
            NullValueHandling = NullValueHandling.Ignore)]
        public string JobHash { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public JobStatus Status { get; set; }

        /// <summary>
        /// Process mode
        /// </summary>
        [JsonProperty(PropertyName = "processMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public ProcessMode ProcessMode { get; set; }

        /// <summary>
        /// Job state
        /// </summary>
        [JsonProperty(PropertyName = "state",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken State { get; set; }
    }
}