// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Endpoint registration model
    /// </summary>
    public class EndpointInfoApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointInfoApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointInfoApiModel(EndpointInfoModel model) {
            Registration = new EndpointRegistrationApiModel(model.Registration);
            ApplicationId = model.ApplicationId;
            NotSeenSince = model.NotSeenSince;
            Activated = model.Activated;
            Connected = model.Connected;
            OutOfSync = model.OutOfSync;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public EndpointInfoModel ToServiceModel() {
            return new EndpointInfoModel {
                ApplicationId = ApplicationId,
                NotSeenSince = NotSeenSince,
                Registration = Registration.ToServiceModel(),
                Activated = Activated,
                Connected = Connected,
                OutOfSync = OutOfSync
            };
        }

        /// <summary>
        /// Endpoint registration
        /// </summary>
        [JsonProperty(PropertyName = "registration")]
        [Required]
        public EndpointRegistrationApiModel Registration { get; set; }

        /// <summary>
        /// Application id endpoint is registered under.
        /// </summary>
        [JsonProperty(PropertyName = "applicationId")]
        [Required]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Whether endpoint is activated on this registration
        /// </summary>
        [JsonProperty(PropertyName = "activated",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Activated { get; set; }

        /// <summary>
        /// Whether endpoint is connected on this registration
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Connected { get; set; }

        /// <summary>
        /// Whether the registration is out of sync
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null), ReadOnly(true)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Last time endpoint was seen
        /// </summary>
        [JsonProperty(PropertyName = "notSeenSince",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public DateTime? NotSeenSince { get; set; }
    }
}
