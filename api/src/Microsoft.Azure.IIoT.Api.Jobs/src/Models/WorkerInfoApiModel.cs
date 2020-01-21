// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Worker info
    /// </summary>
    public class WorkerInfoApiModel {

        /// <summary>
        /// Identifier of the worker instance
        /// </summary>
        [JsonProperty(PropertyName = "workerId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string WorkerId { get; set; }

        /// <summary>
        /// Identifier of the agent
        /// </summary>
        [JsonProperty(PropertyName = "agentId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AgentId { get; set; }

        /// <summary>
        /// Worker status
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public WorkerStatus Status { get; set; }

        /// <summary>
        /// Last seen
        /// </summary>
        [JsonProperty(PropertyName = "lastSeen",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime LastSeen { get; set; }
    }
}