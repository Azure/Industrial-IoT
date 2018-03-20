// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Supervisor registration
    /// </summary>
    public class SupervisorModel {

        /// <summary>
        /// Identifier of the supervisor
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Whether supervisor is in discovery mode
        /// </summary>
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Supervisor configuration
        /// </summary>
        public SupervisorConfigModel Configuration { get; set; }

        /// <summary>
        /// Domain of supervisor
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (edge) and server (service) (default: false).
        /// </summary>
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether supervisor is connected
        /// </summary>
        public bool? Connected { get; set; }
    }
}
