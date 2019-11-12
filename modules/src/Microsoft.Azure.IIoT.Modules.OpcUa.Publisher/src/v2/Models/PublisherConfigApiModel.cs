// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Controller {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Default publisher configuration
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
        public PublisherConfigApiModel(AgentConfigModel model) {
            AgentId = model?.AgentId;
            Capabilities = model?.Capabilities?.ToDictionary(k => k.Key, v => v.Value);
            HeartbeatInterval = model?.HeartbeatInterval;
            JobCheckInterval = model?.JobCheckInterval;
            JobOrchestratorUrl = model?.JobOrchestratorUrl;
            ParallelJobs = model?.MaxWorkers;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public AgentConfigModel ToServiceModel() {
            return new AgentConfigModel {
                AgentId = AgentId,
                Capabilities = Capabilities?.ToDictionary(k => k.Key, v => v.Value),
                HeartbeatInterval = HeartbeatInterval,
                JobCheckInterval = JobCheckInterval,
                JobOrchestratorUrl = JobOrchestratorUrl,
                MaxWorkers = ParallelJobs
            };
        }

        /// <summary>
        /// Agent identifier
        /// </summary>
        [JsonProperty(PropertyName = "agentId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AgentId { get; set; }

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
        [JsonProperty(PropertyName = "heartBeatInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Parellel jobs
        /// </summary>
        [JsonProperty(PropertyName = "parellelJobs",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? ParallelJobs { get; set; }

        /// <summary>
        /// Job service endpoint url
        /// </summary>
        [JsonProperty(PropertyName = "jobServiceUrl",
            NullValueHandling = NullValueHandling.Ignore)]
        public string JobOrchestratorUrl { get; set; }
    }
}