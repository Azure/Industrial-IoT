// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Discoverer registration
    /// </summary>
    public class DiscovererModel {

        /// <summary>
        /// Identifier of the discoverer
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Site of the discoverer
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Whether discoverer is in discovery mode
        /// </summary>
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Discoverer config
        /// </summary>
        public DiscoveryConfigModel DiscoveryConfig { get; set; }

        /// <summary>
        /// Requested discovery mode
        /// </summary>
        public DiscoveryMode? RequestedMode { get; set; }

        /// <summary>
        /// Requested configuration
        /// </summary>
        public DiscoveryConfigModel RequestedConfig { get; set; }

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
        /// Whether discoverer is connected
        /// </summary>
        public bool? Connected { get; set; }

        /// <summary>
        /// Version information
        /// </summary>
        public string Version { get; set; }
    }
}
