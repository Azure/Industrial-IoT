// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Supervisor registration query request
    /// </summary>
    public class SupervisorQueryModel {

        /// <summary>
        /// Site for the supervisors
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Managing provided endpoint twin
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Included connected or disconnected
        /// </summary>
        public bool? Connected { get; set; }
    }
}
