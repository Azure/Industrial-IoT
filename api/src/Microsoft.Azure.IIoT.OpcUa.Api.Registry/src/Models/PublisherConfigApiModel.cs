// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Default publisher agent configuration
    /// </summary>
    public class PublisherConfigApiModel {

        /// <summary>
        /// Capabilities
        /// </summary>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Capabilities { get; set; }

        /// <summary>
        /// Interval to check job
        /// </summary>
        [JsonProperty(PropertyName = "jobCheckInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? JobCheckInterval { get; set; }

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        [JsonProperty(PropertyName = "heartbeatInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Max workers
        /// </summary>
        [JsonProperty(PropertyName = "maxWorkers",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxWorkers { get; set; }

        /// <summary>
        /// Job service endpoint url
        /// </summary>
        [JsonProperty(PropertyName = "jobOrchestratorUrl",
            NullValueHandling = NullValueHandling.Ignore)]
        public string JobOrchestratorUrl { get; set; }
    }
}
