// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Controller {
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Default publisher configuration
    /// </summary>
    [DataContract]
    public class PublisherConfigApiModel {

        /// <summary>
        /// Agent identifier
        /// </summary>
        [DataMember(Name = "agentId", Order = 0,
            EmitDefaultValue = false)]
        public string AgentId { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        [DataMember(Name = "capabilities", Order = 1,
            EmitDefaultValue = false)]
        public Dictionary<string, string> Capabilities { get; set; }

        /// <summary>
        /// Interval to check job
        /// </summary>
        [DataMember(Name = "jobCheckInterval", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? JobCheckInterval { get; set; }

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        [DataMember(Name = "heartBeatInterval", Order = 3,
            EmitDefaultValue = false)]
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Parellel jobs
        /// </summary>
        [DataMember(Name = "parellelJobs", Order = 4,
            EmitDefaultValue = false)]
        public int? ParallelJobs { get; set; }

        /// <summary>
        /// Job service endpoint url
        /// </summary>
        [DataMember(Name = "jobServiceUrl", Order = 5,
            EmitDefaultValue = false)]
        public string JobOrchestratorUrl { get; set; }
    }
}