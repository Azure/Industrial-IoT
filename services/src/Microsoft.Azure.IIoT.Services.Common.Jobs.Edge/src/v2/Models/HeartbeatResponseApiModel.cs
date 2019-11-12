// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Heatbeat response
    /// </summary>
    public class HeartbeatResponseApiModel {

        /// <summary>
        /// Create response
        /// </summary>
        /// <param name="model"></param>
        public HeartbeatResponseApiModel(HeartbeatResultModel model) {
            HeartbeatInstruction = model.HeartbeatInstruction;
            LastActiveHeartbeat = model.LastActiveHeartbeat;
            UpdatedJob = model.UpdatedJob == null ? null :
                new JobProcessingInstructionApiModel(model.UpdatedJob);
        }

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