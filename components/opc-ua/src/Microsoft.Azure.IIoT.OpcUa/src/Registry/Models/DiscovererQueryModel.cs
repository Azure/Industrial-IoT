// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Discoverer registration query request
    /// </summary>
    public class DiscovererQueryModel {

        /// <summary>
        /// Site for the supervisors
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Discovery mode
        /// </summary>
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Included connected or disconnected
        /// </summary>
        public bool? Connected { get; set; }
    }
}
