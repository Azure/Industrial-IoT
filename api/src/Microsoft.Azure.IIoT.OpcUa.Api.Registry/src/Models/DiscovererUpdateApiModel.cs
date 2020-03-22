// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Discoverer update request
    /// </summary>
    [DataContract]
    public class DiscovererUpdateApiModel {

        /// <summary>
        /// Site the discoverer is part of
        /// </summary>
        [DataMember(Name = "siteId",
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Discovery mode of discoverer
        /// </summary>
        [DataMember(Name = "discovery",
            EmitDefaultValue = false)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Discoverer discovery config
        /// </summary>
        [DataMember(Name = "discoveryConfig",
            EmitDefaultValue = false)]
        public DiscoveryConfigApiModel DiscoveryConfig { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [DataMember(Name = "logLevel",
            EmitDefaultValue = false)]
        public TraceLogLevel? LogLevel { get; set; }
    }
}
