// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Supervisor update request
    /// </summary>
    public class SupervisorUpdateApiModel {

        /// <summary>
        /// Site the supervisor is part of
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SiteId { get; set; }

        /// <summary>
        /// Discovery mode of supervisor
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
        /// Callbacks to add or remove (see below)
        /// </summary>
        [JsonProperty(PropertyName = "discoveryCallbacks",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<CallbackApiModel> DiscoveryCallbacks { get; set; }

        /// <summary>
        /// Whether to add or remove callbacks
        /// </summary>
        [JsonProperty(PropertyName = "removeDiscoveryCallbacks",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? RemoveDiscoveryCallbacks { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [JsonProperty(PropertyName = "logLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        public SupervisorLogLevel? LogLevel { get; set; }
    }
}
