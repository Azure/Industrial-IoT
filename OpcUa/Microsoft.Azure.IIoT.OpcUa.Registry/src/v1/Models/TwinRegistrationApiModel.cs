// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Twin model for webservice api
    /// </summary>
    public class TwinRegistrationApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinRegistrationApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinRegistrationApiModel(TwinRegistrationModel model) {
            Id = model.Id;
            Endpoint = new EndpointApiModel(model.Endpoint);
            Certificate = model.Certificate;
            SiteId = model.SiteId;
            SecurityLevel = model.SecurityLevel;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public TwinRegistrationModel ToServiceModel() {
            return new TwinRegistrationModel {
                Id = Id,
                SiteId = SiteId,
                Endpoint = Endpoint.ToServiceModel(),
                SecurityLevel = SecurityLevel,
                Certificate = Certificate
            };
        }

        /// <summary>
        /// Registered identifier of the twin
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Registered site of the twin
        /// </summary>
        [JsonProperty(PropertyName = "siteId")]
        [DefaultValue(null)]
        public string SiteId { get; set; }

        /// <summary>
        /// Endpoint information of the registration
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        [Required]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Security level of the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "securityLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// Endpoint cert that was registered.
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public byte[] Certificate { get; set; }
    }
}
