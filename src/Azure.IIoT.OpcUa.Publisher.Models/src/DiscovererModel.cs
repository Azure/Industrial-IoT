// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Discoverer registration
    /// </summary>
    [DataContract]
    public sealed record class DiscovererModel
    {
        /// <summary>
        /// Discoverer id
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public required string Id { get; set; }

        /// <summary>
        /// Site of the discoverer
        /// </summary>
        [DataMember(Name = "siteId", Order = 1,
            EmitDefaultValue = false)]
        public string? SiteId { get; set; }

        /// <summary>
        /// Whether the discoverer is in discovery mode
        /// </summary>
        [DataMember(Name = "discovery", Order = 2,
            EmitDefaultValue = false)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Discoverer configuration
        /// </summary>
        [DataMember(Name = "discoveryConfig", Order = 3,
            EmitDefaultValue = false)]
        public DiscoveryConfigModel? DiscoveryConfig { get; set; }

        /// <summary>
        /// Requested discovery mode
        /// </summary>
        [DataMember(Name = "requestedMode", Order = 4,
            EmitDefaultValue = false)]
        public DiscoveryMode? RequestedMode { get; set; }

        /// <summary>
        /// Requested discoverer configuration
        /// </summary>
        [DataMember(Name = "requestedConfig", Order = 5,
            EmitDefaultValue = false)]
        public DiscoveryConfigModel? RequestedConfig { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        [DataMember(Name = "outOfSync", Order = 7,
            EmitDefaultValue = false)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether discoverer is connected on this registration
        /// </summary>
        [DataMember(Name = "connected", Order = 8,
            EmitDefaultValue = false)]
        public bool? Connected { get; set; }

        /// <summary>
        /// The reported version of the discovery module
        /// </summary>
        [DataMember(Name = "version", Order = 9,
            EmitDefaultValue = false)]
        public string? Version { get; set; }

        /// <summary>
        /// Current api key
        /// </summary>
        [DataMember(Name = "apiKey", Order = 10,
            EmitDefaultValue = false)]
        public string? ApiKey { get; set; }
    }
}
