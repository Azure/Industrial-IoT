// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// processing status
    /// </summary>
    public class ProcessingStatusApiModel {

        /// <summary>
        /// Default
        /// </summary>
        public ProcessingStatusApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public ProcessingStatusApiModel(ProcessingStatusModel model) {
            LastKnownHeartbeat = model.LastKnownHeartbeat;
            LastKnownState = model.LastKnownState?.DeepClone();
            ProcessMode = model.ProcessMode;
        }

        /// <summary>
        /// Last known heartbeat
        /// </summary>
        [JsonProperty(PropertyName = "lastKnownHeartbeat",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastKnownHeartbeat { get; set; }

        /// <summary>
        /// Last known state
        /// </summary>
        [JsonProperty(PropertyName = "lastKnownState",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken LastKnownState { get; set; }

        /// <summary>
        /// Processing mode
        /// </summary>
        [JsonProperty(PropertyName = "processMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public ProcessMode? ProcessMode { get; set; }
    }
}