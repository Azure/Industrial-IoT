// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Discoverer registration model
    /// </summary>
    public class DiscovererApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiscovererApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DiscovererApiModel(DiscovererModel model) {
            Id = model.Id;
            SiteId = model.SiteId;
            Discovery = model.Discovery;
            LogLevel = model.LogLevel;
            DiscoveryConfig = model.DiscoveryConfig == null ? null :
                new DiscoveryConfigApiModel(model.DiscoveryConfig);
            OutOfSync = model.OutOfSync;
            Connected = model.Connected;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public DiscovererModel ToServiceModel() {
            return new DiscovererModel {
                Id = Id,
                SiteId = SiteId,
                LogLevel = LogLevel,
                Discovery = Discovery,
                DiscoveryConfig = DiscoveryConfig?.ToServiceModel(),
                OutOfSync = OutOfSync,
                Connected = Connected
            };
        }

        /// <summary>
        /// Discoverer id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Site of the discoverer
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteId { get; set; }

        /// <summary>
        /// Whether the discoverer is in discovery mode
        /// </summary>
        [JsonProperty(PropertyName = "discovery",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(DiscoveryMode.Off)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Discoverer configuration
        /// </summary>
        [JsonProperty(PropertyName = "discoveryConfig",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public DiscoveryConfigApiModel DiscoveryConfig { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [JsonProperty(PropertyName = "logLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(TraceLogLevel.Information)]
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether discoverer is connected on this registration
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Connected { get; set; }
    }
}
