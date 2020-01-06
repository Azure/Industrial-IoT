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
    /// Gateway info model
    /// </summary>
    public class GatewayInfoApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public GatewayInfoApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public GatewayInfoApiModel(GatewayInfoModel model) {
            Gateway = model.Gateway == null ? null : 
                new GatewayApiModel(model.Gateway);
            Publisher = model.Publisher == null ? null :
                new PublisherApiModel(model.Publisher);
            Supervisor = model.Supervisor == null ? null :
                new SupervisorApiModel(model.Supervisor);
            Discoverer = model.Discoverer == null ? null :
                new DiscovererApiModel(model.Discoverer);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public GatewayInfoModel ToServiceModel() {
            return new GatewayInfoModel {
                Gateway = Gateway?.ToServiceModel(),
                Publisher = Publisher?.ToServiceModel(),
                Supervisor = Supervisor?.ToServiceModel(),
                Discoverer = Discoverer?.ToServiceModel()
            };
        }

        /// <summary>
        /// Gateway
        /// </summary>
        [JsonProperty(PropertyName = "gateway")]
        [Required]
        public GatewayApiModel Gateway { get; set; }

        /// <summary>
        /// Supervisor identity if deployed
        /// </summary>
        [JsonProperty(PropertyName = "supervisor",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public SupervisorApiModel Supervisor { get; set; }

        /// <summary>
        /// Publisher identity if deployed
        /// </summary>
        [JsonProperty(PropertyName = "publisher",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public PublisherApiModel Publisher { get; set; }

        /// <summary>
        /// Discoverer identity if deployed
        /// </summary>
        [JsonProperty(PropertyName = "discoverer",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public DiscovererApiModel Discoverer { get; set; }
    }
}
