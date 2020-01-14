// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Gateway info model
    /// </summary>
    public class GatewayInfoApiModel {

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
