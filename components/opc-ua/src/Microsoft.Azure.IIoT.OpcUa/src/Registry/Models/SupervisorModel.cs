// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Supervisor registration
    /// </summary>
    public class SupervisorModel {

        /// <summary>
        /// Identifier of the supervisor
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Site of the supervisor
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether supervisor is connected
        /// </summary>
        public bool? Connected { get; set; }
    }
}
