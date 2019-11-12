// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Heartbeat information
    /// </summary>
    public class WorkerHeartbeatApiModel {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public WorkerHeartbeatModel ToServiceModel() {
            return new WorkerHeartbeatModel {
                WorkerId = WorkerId,
                AgentId = AgentId,
                Status = Status
            };
        }

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