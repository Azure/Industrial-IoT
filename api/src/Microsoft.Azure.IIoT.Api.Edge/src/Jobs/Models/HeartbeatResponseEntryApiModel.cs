// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Heatbeat response
    /// </summary>
    public class HeartbeatResponseEntryApiModel {
        /// <summary>
        /// WorkerSupervisorId
        /// </summary>
        [JsonProperty(PropertyName = "jobId")]
        public string JobId { get; set; }


        /// <summary>
        /// Instructions
        /// </summary>
        [JsonProperty(PropertyName = "heartbeatInstruction")]
        public HeartbeatInstruction HeartbeatInstruction { get; set; }

        /// <summary>
        /// Last active
        /// </summary>
        [JsonProperty(PropertyName = "lastActiveHeartbeat",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastActiveHeartbeat { get; set; }

        /// <summary>
        /// Job continuation in case of updates
        /// </summary>
        [JsonProperty(PropertyName = "updatedJob",
            NullValueHandling = NullValueHandling.Ignore)]
        public JobProcessingInstructionApiModel UpdatedJob { get; set; }
    }
}