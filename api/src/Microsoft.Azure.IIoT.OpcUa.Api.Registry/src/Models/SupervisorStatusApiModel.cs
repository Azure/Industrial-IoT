// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Supervisor runtime status
    /// </summary>
    public class SupervisorStatusApiModel {

        /// <summary>
        /// Edge device id
        /// </summary>
        [JsonProperty(PropertyName = "deviceId")]
        [Required]
        public string DeviceId { get; set; }

        /// <summary>
        /// Module id
        /// </summary>
        [JsonProperty(PropertyName = "moduleId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ModuleId { get; set; }

        /// <summary>
        /// Site id
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteId { get; set; }

        /// <summary>
        /// Endpoint activation status
        /// </summary>
        [JsonProperty(PropertyName = "endpoints",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<EndpointActivationStatusApiModel> Endpoints { get; set; }
    }
}
