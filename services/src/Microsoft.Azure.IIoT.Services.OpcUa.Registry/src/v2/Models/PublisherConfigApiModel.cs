// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Default publisher agent configuration
    /// </summary>
    public class PublisherConfigApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublisherConfigApiModel() {
        }

        /// <summary>
        /// Create config
        /// </summary>
        /// <param name="model"></param>
        public PublisherConfigApiModel(PublisherConfigModel model) {
            Capabilities = model?.Capabilities?.ToDictionary(k => k.Key, v => v.Value);
            HeartbeatInterval = model?.HeartbeatInterval;
            JobCheckInterval = model?.JobCheckInterval;
            JobOrchestratorUrl = model?.JobOrchestratorUrl;
            MaxWorkers = model?.MaxWorkers;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public PublisherConfigModel ToServiceModel() {
            return new PublisherConfigModel {
                Capabilities = Capabilities?.ToDictionary(k => k.Key, v => v.Value),
                HeartbeatInterval = HeartbeatInterval,
                JobCheckInterval = JobCheckInterval,
                JobOrchestratorUrl = JobOrchestratorUrl,
                MaxWorkers = MaxWorkers
            };
        }

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
        /// Parallel jobs
        /// </summary>
        [JsonProperty(PropertyName = "maxWorkers",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxWorkers { get; set; }

        /// <summary>
        /// Job orchestrator endpoint url
        /// </summary>
        [JsonProperty(PropertyName = "jobOrchestratorUrl",
            NullValueHandling = NullValueHandling.Ignore)]
        public string JobOrchestratorUrl { get; set; }
    }
}