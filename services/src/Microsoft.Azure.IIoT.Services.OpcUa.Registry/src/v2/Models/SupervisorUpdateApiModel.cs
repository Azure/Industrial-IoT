// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Supervisor registration update request
    /// </summary>
    public class SupervisorUpdateApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public SupervisorUpdateApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public SupervisorUpdateApiModel(SupervisorUpdateModel model) {
            SiteId = model.SiteId;
            Discovery = model.Discovery;
            LogLevel = model.LogLevel;
            DiscoveryConfig = model.DiscoveryConfig == null ? null :
                new DiscoveryConfigApiModel(model.DiscoveryConfig);
            DiscoveryCallbacks = model.DiscoveryCallbacks?
                .Select(c => new CallbackApiModel(c)).ToList();
            RemoveDiscoveryCallbacks = model.RemoveDiscoveryCallbacks;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public SupervisorUpdateModel ToServiceModel() {
            return new SupervisorUpdateModel {
                SiteId = SiteId,
                LogLevel = LogLevel,
                Discovery = Discovery,
                DiscoveryConfig = DiscoveryConfig?.ToServiceModel(),
                DiscoveryCallbacks = DiscoveryCallbacks?
                    .Select(c => c.ToServiceModel()).ToList(),
                RemoveDiscoveryCallbacks = RemoveDiscoveryCallbacks
            };
        }

        /// <summary>
        /// Site of the supervisor
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteId { get; set; }

        /// <summary>
        /// Whether the supervisor is in discovery mode.
        /// If null, does not change.
        /// </summary>
        [JsonProperty(PropertyName = "discovery",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(DiscoveryMode.Off)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Supervisor discovery configuration
        /// </summary>
        [JsonProperty(PropertyName = "discoveryConfig",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public DiscoveryConfigApiModel DiscoveryConfig { get; set; }

        /// <summary>
        /// Callbacks to add or remove (see below)
        /// </summary>
        [JsonProperty(PropertyName = "discoveryCallbacks",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<CallbackApiModel> DiscoveryCallbacks { get; set; }

        /// <summary>
        /// Whether to add or remove callbacks
        /// </summary>
        [JsonProperty(PropertyName = "removeDiscoveryCallbacks",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? RemoveDiscoveryCallbacks { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [JsonProperty(PropertyName = "logLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public SupervisorLogLevel? LogLevel { get; set; }
    }
}
