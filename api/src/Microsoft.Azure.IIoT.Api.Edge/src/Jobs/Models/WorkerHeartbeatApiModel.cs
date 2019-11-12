// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Heartbeat information
    /// </summary>
    public class WorkerHeartbeatApiModel {

        /// <summary>
        /// Worker id
        /// </summary>
        [JsonProperty(PropertyName = "workerId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string WorkerId { get; set; }

        /// <summary>
        /// Agent id
        /// </summary>
        [JsonProperty(PropertyName = "agentId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AgentId { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public WorkerStatus Status { get; set; }
    }
}