// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Security.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint security
    /// </summary>
    public class SecurityAgentMessageModel {

        /// <summary>
        /// Agent Version
        /// </summary>
        public string AgentVersion { get; set; } = "0.0.1";

        /// <summary>
        /// Agent Id
        /// </summary>
        public string AgentId { get; set; } = "e00dc5f5-feac-4c3e-87e2-93c16f010c00";

        /// <summary>
        /// Message Schema Version
        /// </summary>
        public string MessageSchemaVersion { get; set; } = "1.0";

        /// <summary>
        /// Events
        /// </summary>
        public List<SecurityEventModel> Events { get; set; }
    }
}
