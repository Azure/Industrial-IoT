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
        [DataMember(Name = "agentId",
            EmitDefaultValue = false)]
        public string AgentId { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        [DataMember(Name = "capabilities",
            EmitDefaultValue = false)]
        public Dictionary<string, string> Capabilities { get; set; }

        /// <summary>
        /// Interval to check job
        /// </summary>
        [DataMember(Name = "jobCheckInterval",
            EmitDefaultValue = false)]
        public TimeSpan? JobCheckInterval { get; set; }

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        [DataMember(Name = "heartBeatInterval",
            EmitDefaultValue = false)]
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Parellel jobs
        /// </summary>
        [DataMember(Name = "parellelJobs",
            EmitDefaultValue = false)]
        public int? ParallelJobs { get; set; }

        /// <summary>
        /// Job service endpoint url
        /// </summary>
        [DataMember(Name = "jobServiceUrl",
            EmitDefaultValue = false)]
        public string JobOrchestratorUrl { get; set; }
    }
}