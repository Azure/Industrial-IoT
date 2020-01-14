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
    /// Endpoint registration model
    /// </summary>
    public class EndpointRegistrationApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointRegistrationApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointRegistrationApiModel(EndpointRegistrationModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Id = model.Id;
            Endpoint = model.Endpoint == null ? null :
                new EndpointApiModel(model.Endpoint);
            EndpointUrl = model.EndpointUrl;
            AuthenticationMethods = model.AuthenticationMethods?
                .Select(p => p == null ? null : new AuthenticationMethodApiModel(p))
                .ToList();
            SiteId = model.SiteId;
            SupervisorId = model.SupervisorId;
            DiscovererId = model.DiscovererId;
            SecurityLevel = model.SecurityLevel;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public EndpointRegistrationModel ToServiceModel() {
            return new EndpointRegistrationModel {
                Id = Id,
                Endpoint = Endpoint?.ToServiceModel(),
                EndpointUrl = EndpointUrl,
                AuthenticationMethods = AuthenticationMethods?
                    .Select(p => p?.ToServiceModel()).ToList(),
                SecurityLevel = SecurityLevel,
                SiteId = SiteId,
                DiscovererId = DiscovererId,
                SupervisorId = SupervisorId
            };
        }

        /// <summary>
        /// Registered identifier of the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Original endpoint url of the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "endpointUrl",
            NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Registered site of the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteId { get; set; }

        /// <summary>
        /// Supervisor that manages the endpoint.
        /// </summary>
        [JsonProperty(PropertyName = "supervisorId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SupervisorId { get; set; }

        /// <summary>
        /// Discoverer that registered the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "discovererId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string DiscovererId { get; set; }

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
        /// Supported authentication methods that can be selected to
        /// obtain a credential and used to interact with the endpoint.
        /// </summary>
        [JsonProperty(PropertyName = "authenticationMethods",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<AuthenticationMethodApiModel> AuthenticationMethods { get; set; }
    }
}
