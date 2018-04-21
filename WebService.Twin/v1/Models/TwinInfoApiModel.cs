// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using Newtonsoft.Json;
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
            Id = model.Id;
            Endpoint = new EndpointApiModel(model.Endpoint);
            ApplicationId = model.ApplicationId;
            SecurityLevel = model.SecurityLevel;
            OutOfSync = model.OutOfSync;
            Connected = model.Connected;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public TwinInfoModel ToServiceModel() {
            return new TwinInfoModel {
                Id = Id,
                ApplicationId = ApplicationId,
                Endpoint = Endpoint.ToServiceModel(),
                SecurityLevel = SecurityLevel,
                OutOfSync = OutOfSync,
                Connected = Connected
            };
        }

        /// <summary>
        /// Registered identifier of the twin
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Application id endpoint is registered under.
        /// </summary>
        [JsonProperty(PropertyName = "applicationId")]
        [Required]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Endpoint information of the registration
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        [Required]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Endpoint security level
        /// </summary>
        [JsonProperty(PropertyName = "securityLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null), ReadOnly(true)]
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// Whether the registration is out of sync
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null), ReadOnly(true)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether edge is connected on this registration
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null), ReadOnly(true)]
        public bool? Connected { get; set; }
    }
}
