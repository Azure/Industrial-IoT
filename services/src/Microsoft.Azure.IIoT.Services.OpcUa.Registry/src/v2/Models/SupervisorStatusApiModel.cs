// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Supervisor runtime status
    /// </summary>
    public class SupervisorStatusApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public SupervisorStatusApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public SupervisorStatusApiModel(SupervisorStatusModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            DeviceId = model.DeviceId;
            ModuleId = model.ModuleId;
            SiteId = model.SiteId;
            Endpoints = model.Endpoints?
                .Select(e => e == null ? null : new EndpointActivationStatusApiModel(e))
                .ToList();
        }

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
