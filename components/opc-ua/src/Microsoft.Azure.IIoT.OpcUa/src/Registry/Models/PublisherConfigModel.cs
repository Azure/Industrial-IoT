// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Default publisher configuration
    /// </summary>
    public class PublisherConfigModel {

        /// <summary>
        /// Capabilities
        /// </summary>
        public Dictionary<string, string> Capabilities { get; set; }

        /// <summary>
        /// Interval to check job
        /// </summary>
        public TimeSpan? JobCheckInterval { get; set; }

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Max parallel worker jobs
        /// </summary>
        public int? MaxWorkers { get; set; }

        /// <summary>
        /// Job service endpoint url
        /// </summary>
        public string JobOrchestratorUrl { get; set; }
    }
}
