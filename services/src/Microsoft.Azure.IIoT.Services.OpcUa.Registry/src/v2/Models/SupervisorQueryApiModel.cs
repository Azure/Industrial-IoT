// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Supervisor registration query
    /// </summary>
    public class SupervisorQueryApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public SupervisorQueryApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public SupervisorQueryApiModel(SupervisorQueryModel model) {
            SiteId = model.SiteId;
            Discovery = model.Discovery;
            Connected = model.Connected;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public SupervisorQueryModel ToServiceModel() {
            return new SupervisorQueryModel {
                SiteId = SiteId,
                Connected = Connected,
                Discovery = Discovery
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
        /// Discovery mode of supervisor
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
