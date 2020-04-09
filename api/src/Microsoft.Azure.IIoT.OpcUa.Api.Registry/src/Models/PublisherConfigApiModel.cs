// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Default publisher agent configuration
    /// </summary>
    [DataContract]
    public class PublisherConfigApiModel {

        /// <summary>
        /// Capabilities
        /// </summary>
        [DataMember(Name = "capabilities", Order = 0,
            EmitDefaultValue = false)]
        public Dictionary<string, string> Capabilities { get; set; }

        /// <summary>
        /// Interval to check job
        /// </summary>
        [DataMember(Name = "jobCheckInterval", Order = 1,
            EmitDefaultValue = false)]
        public TimeSpan? JobCheckInterval { get; set; }

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        [DataMember(Name = "heartbeatInterval", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Parallel jobs
        /// </summary>
        [DataMember(Name = "maxWorkers", Order = 3,
            EmitDefaultValue = false)]
        public int? MaxWorkers { get; set; }

        /// <summary>
        /// Job orchestrator endpoint url
        /// </summary>
        [DataMember(Name = "jobOrchestratorUrl", Order = 4,
            EmitDefaultValue = false)]
        public string JobOrchestratorUrl { get; set; }
    }
}
