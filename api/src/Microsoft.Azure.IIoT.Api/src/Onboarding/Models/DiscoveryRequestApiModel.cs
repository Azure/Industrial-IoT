// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
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
    /// Discovery request
    /// </summary>
    public class DiscoveryRequestApiModel {

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Discovery mode to use
        /// </summary>
        [JsonProperty(PropertyName = "discovery",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Scan configuration to use
        /// </summary>
        [JsonProperty(PropertyName = "configuration",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiscoveryConfigApiModel Configuration { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [JsonProperty(PropertyName = "context",
            NullValueHandling = NullValueHandling.Ignore)]
        public RegistryOperationApiModel Context { get; set; }
    }
}
