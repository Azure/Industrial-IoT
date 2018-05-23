// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Models {

    /// <summary>
    /// Supervisor registration query request
    /// </summary>
    public class SupervisorQueryModel {

        /// <summary>
        /// Site for the supervisors
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Supervisor discovery mode
        /// </summary>
        public DiscoveryMode? Discovery { get; set; }
    }
}
