// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Discoverer registration query
    /// </summary>
    public class DiscovererQueryApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiscovererQueryApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DiscovererQueryApiModel(DiscovererQueryModel model) {
            SiteId = model.SiteId;
            Discovery = model.Discovery;
            Connected = model.Connected;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public DiscovererQueryModel ToServiceModel() {
            return new DiscovererQueryModel {
                SiteId = SiteId,
                Connected = Connected,
                Discovery = Discovery
            };
        }

        /// <summary>
        /// Site of the discoverer
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteId { get; set; }

        /// <summary>
        /// Discovery mode of discoverer
        /// </summary>
        [JsonProperty(PropertyName = "discovery",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Included connected or disconnected
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Connected { get; set; }
    }
}
