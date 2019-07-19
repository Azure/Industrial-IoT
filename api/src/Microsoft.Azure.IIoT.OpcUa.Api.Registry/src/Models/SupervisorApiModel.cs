// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Discovery mode to use
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DiscoveryMode {

        /// <summary>
        /// No discovery
        /// </summary>
        Off,

        /// <summary>
        /// Find and use local discovery server on edge device
        /// </summary>
        Local,

        /// <summary>
        /// Find and use all LDS in all connected networks
        /// </summary>
        Network,

        /// <summary>
        /// Fast network scan of */24 and known list of ports
        /// </summary>
        Fast,

        /// <summary>
        /// Perform a deep scan of all networks.
        /// </summary>
        Scan
    }

    /// <summary>
    /// Log level for supervisor
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SupervisorLogLevel {

        /// <summary>
        /// Error only
        /// </summary>
        Error = 0,

        /// <summary>
        /// Default
        /// </summary>
        Information = 1,

        /// <summary>
        /// Debug log
        /// </summary>
        Debug = 2,

        /// <summary>
        /// Verbose
        /// </summary>
        Verbose = 3
    }

    /// <summary>
    /// Supervisor registration model
    /// </summary>
    public class SupervisorApiModel {

        /// <summary>
        /// Supervisor id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Site of the application
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SiteId { get; set; }

        /// <summary>
        /// Whether the supervisor is in discovery mode
        /// </summary>
        [JsonProperty(PropertyName = "discovery",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Supervisor discovery config
        /// </summary>
        [JsonProperty(PropertyName = "discoveryConfig",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiscoveryConfigApiModel DiscoveryConfig { get; set; }

        /// <summary>
        /// Supervisor public client cert
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [JsonProperty(PropertyName = "logLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        public SupervisorLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether supervisor is connected on this registration
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Connected { get; set; }
    }
}
