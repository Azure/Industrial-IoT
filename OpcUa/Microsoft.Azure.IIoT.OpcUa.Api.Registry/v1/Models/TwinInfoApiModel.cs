// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Twin registration model for webservice api
    /// </summary>
    public class TwinInfoApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinInfoApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinInfoApiModel(TwinInfoModel model) {
            Registration = new TwinRegistrationApiModel(model.Registration);
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
        public TwinInfoModel ToServiceModel() {
            return new TwinInfoModel {
                ApplicationId = ApplicationId,
                NotSeenSince = NotSeenSince,
                Registration = Registration.ToServiceModel(),
                Activated = Activated,
                Connected = Connected,
                OutOfSync = OutOfSync
            };
        }

        /// <summary>
        /// Twin registration
        /// </summary>
        [JsonProperty(PropertyName = "registration")]
        [Required]
        public TwinRegistrationApiModel Registration { get; set; }

        /// <summary>
        /// Application id endpoint is registered under.
        /// </summary>
        [JsonProperty(PropertyName = "applicationId")]
        [Required]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Whether edge is activated on this registration
        /// </summary>
        [JsonProperty(PropertyName = "activated",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Activated { get; set; }

        /// <summary>
        /// Whether edge is connected on this registration
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
        /// Last time twin was seen
        /// </summary>
        [JsonProperty(PropertyName = "notSeenSince",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public DateTime? NotSeenSince { get; set; }
    }
}
