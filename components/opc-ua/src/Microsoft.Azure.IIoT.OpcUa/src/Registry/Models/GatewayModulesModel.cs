// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Edge Gateway modules
    /// </summary>
    public class GatewayModulesModel {

        /// <summary>
        /// Supervisor identity if deployed
        /// </summary>
        public SupervisorModel Supervisor { get; set; }

        /// <summary>
        /// Publisher identity if deployed
        /// </summary>
        public PublisherModel Publisher { get; set; }

        /// <summary>
        /// Discoverer identity if deployed
        /// </summary>
        public DiscovererModel Discoverer { get; set; }
    }
}
