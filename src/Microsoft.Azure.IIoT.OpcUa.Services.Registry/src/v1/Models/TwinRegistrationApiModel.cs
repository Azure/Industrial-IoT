// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Twin registration model
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
            Endpoint = model.Endpoint == null ? null :
                new EndpointApiModel(model.Endpoint);
            AuthenticationMethods = model.AuthenticationMethods?
                .Select(p => new AuthenticationMethodApiModel(p)).ToList();
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
                AuthenticationMethods = AuthenticationMethods?
                    .Select(p => p.ToServiceModel()).ToList(),
                Endpoint = Endpoint?.ToServiceModel(),
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

        /// <summary>
        /// Supported authentication methods for the endpoint.
        /// </summary>
        [JsonProperty(PropertyName = "authenticationMethods",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<AuthenticationMethodApiModel> AuthenticationMethods { get; set; }
    }
}
