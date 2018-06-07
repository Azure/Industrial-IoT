// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

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
            Id = model.Id;
            SiteId = model.SiteId;
            Discovery = model.Discovery;
            DiscoveryConfig = model.DiscoveryConfig == null ? null :
                new DiscoveryConfigApiModel(model.DiscoveryConfig);
            DiscoveryCallbacks = model.DiscoveryCallbacks;
            RemoveDiscoveryCallbacks = model.RemoveDiscoveryCallbacks;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public SupervisorUpdateModel ToServiceModel() {
            return new SupervisorUpdateModel {
                Id = Id,
                SiteId = SiteId,
                Discovery = Discovery,
                DiscoveryConfig = DiscoveryConfig?.ToServiceModel(),
                DiscoveryCallbacks = DiscoveryCallbacks,
                RemoveDiscoveryCallbacks = RemoveDiscoveryCallbacks
            };
        }

        /// <summary>
        /// Identifier of the supervisor to patch
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

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
        public List<Uri> DiscoveryCallbacks { get; set; }

        /// <summary>
        /// Whether to add or remove callbacks
        /// </summary>
        [JsonProperty(PropertyName = "removeDiscoveryCallbacks",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? RemoveDiscoveryCallbacks { get; set; }
    }
}
