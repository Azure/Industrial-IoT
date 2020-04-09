// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint registration model
    /// </summary>
    [DataContract]
    public class EndpointRegistrationApiModel {

        /// <summary>
        /// Registered identifier of the endpoint
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        public string Id { get; set; }

        /// <summary>
        /// Original endpoint url of the endpoint
        /// </summary>
        [DataMember(Name = "endpointUrl", Order = 1,
            EmitDefaultValue = false)]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Registered site of the endpoint
        /// </summary>
        [DataMember(Name = "siteId", Order = 2,
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Supervisor that can manage the endpoint.
        /// </summary>
        [DataMember(Name = "supervisorId", Order = 3,
            EmitDefaultValue = false)]
        public string SupervisorId { get; set; }

        /// <summary>
        /// Discoverer that registered the endpoint
        /// </summary>
        [DataMember(Name = "discovererId", Order = 4,
            EmitDefaultValue = false)]
        public string DiscovererId { get; set; }

        /// <summary>
        /// Endpoint information of the registration
        /// </summary>
        [DataMember(Name = "endpoint", Order = 5)]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Security level of the endpoint
        /// </summary>
        [DataMember(Name = "securityLevel", Order = 6,
            EmitDefaultValue = false)]
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// Supported authentication methods that can be selected to
        /// obtain a credential and used to interact with the endpoint.
        /// </summary>
        [DataMember(Name = "authenticationMethods", Order = 7,
            EmitDefaultValue = false)]
        public List<AuthenticationMethodApiModel> AuthenticationMethods { get; set; }
    }
}
