// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Discovery mode to use
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DiscoveryMode {

        /// <summary>
        /// Off
        /// </summary>
        Off,

        /// <summary>
        /// Local
        /// </summary>
        Local,

        /// <summary>
        /// Network
        /// </summary>
        Network,

        /// <summary>
        /// Scan
        /// </summary>
        Scan
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
