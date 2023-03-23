// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Endpoint query
    /// </summary>
    [DataContract]
    public sealed record class EndpointRegistrationQueryModel
    {
        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        [DataMember(Name = "url", Order = 0,
            EmitDefaultValue = false)]
        public string? Url { get; set; }

        /// <summary>
        /// Certificate thumbprint of the endpoint
        /// </summary>
        [DataMember(Name = "certificate", Order = 1,
            EmitDefaultValue = false)]
        public string? Certificate { get; set; }

        /// <summary>
        /// Security mode to use for communication - null = Best
        /// </summary>
        [DataMember(Name = "securityMode", Order = 2,
            EmitDefaultValue = false)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Endpoint security policy to use - null = Best.
        /// </summary>
        [DataMember(Name = "securityPolicy", Order = 3,
            EmitDefaultValue = false)]
        public string? SecurityPolicy { get; set; }

        /// <summary>
        /// The last state of the activated endpoint
        /// </summary>
        [DataMember(Name = "endpointState", Order = 6,
            EmitDefaultValue = false)]
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Whether to include endpoints that were soft deleted
        /// </summary>
        [DataMember(Name = "includeNotSeenSince", Order = 7,
            EmitDefaultValue = false)]
        public bool? IncludeNotSeenSince { get; set; }

        /// <summary>
        /// Discoverer id to filter with
        /// </summary>
        [DataMember(Name = "discovererId", Order = 8,
            EmitDefaultValue = false)]
        public string? DiscovererId { get; set; }

        /// <summary>
        /// Application id to filter
        /// </summary>
        [DataMember(Name = "applicationId", Order = 9,
            EmitDefaultValue = false)]
        public string? ApplicationId { get; set; }

        /// <summary>
        /// Site or gateway id to filter with
        /// </summary>
        [DataMember(Name = "siteOrGatewayId", Order = 11,
            EmitDefaultValue = false)]
        public string? SiteOrGatewayId { get; set; }
    }
}
