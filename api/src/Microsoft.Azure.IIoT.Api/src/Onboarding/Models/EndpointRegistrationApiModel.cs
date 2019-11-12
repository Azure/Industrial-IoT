// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint registration model
    /// </summary>
    public class EndpointRegistrationApiModel {

        /// <summary>
        /// Registered identifier of the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "id")]
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
        public string SiteId { get; set; }

        /// <summary>
        /// Supervisor that registered the endpoint.
        /// </summary>
        [JsonProperty(PropertyName = "supervisorId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SupervisorId { get; set; }

        /// <summary>
        /// Endpoint information of the registration
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Security level of the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "securityLevel",
            NullValueHandling = NullValueHandling.Ignore)]
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
