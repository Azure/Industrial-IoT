// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Endpoint registration
    /// </summary>
    [DataContract]
    public sealed record class EndpointRegistrationModel
    {
        /// <summary>
        /// Endpoint identifier which is hashed from
        /// the supervisor, site and url.
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public required string Id { get; set; }

        /// <summary>
        /// Original endpoint url of the endpoint
        /// </summary>
        [DataMember(Name = "endpointUrl", Order = 1,
            EmitDefaultValue = false)]
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Registered site of the endpoint
        /// </summary>
        [DataMember(Name = "siteId", Order = 2,
            EmitDefaultValue = false)]
        public string? SiteId { get; set; }

        /// <summary>
        /// Entity that registered and can access the endpoint
        /// </summary>
        [DataMember(Name = "discovererId", Order = 4,
            EmitDefaultValue = false)]
        public string? DiscovererId { get; set; }

        /// <summary>
        /// Endpoint information of the registration
        /// </summary>
        [DataMember(Name = "endpoint", Order = 5)]
        public EndpointModel? Endpoint { get; set; }

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
        public IReadOnlyList<AuthenticationMethodModel>? AuthenticationMethods { get; set; }
    }
}
