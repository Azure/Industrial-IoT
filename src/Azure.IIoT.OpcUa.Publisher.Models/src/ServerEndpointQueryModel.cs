// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Endpoint model
    /// </summary>
    [DataContract]
    public sealed record class ServerEndpointQueryModel
    {
        /// <summary>
        /// Discovery url to use to query
        /// </summary>
        [DataMember(Name = "discoveryUrl", Order = 0)]
        public string? DiscoveryUrl { get; set; }

        /// <summary>
        /// Endpoint url that should match the found endpoint
        /// </summary>
        [DataMember(Name = "url", Order = 1)]
        public string? Url { get; set; }

        /// <summary>
        /// Endpoint must support this Security Mode.
        /// </summary>
        [DataMember(Name = "securityMode", Order = 2,
            EmitDefaultValue = false)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Endpoint must support this Security policy.
        /// </summary>
        [DataMember(Name = "securityPolicy", Order = 3,
            EmitDefaultValue = false)]
        public string? SecurityPolicy { get; set; }

        /// <summary>
        /// Endpoint must match with this certificate thumbprint
        /// </summary>
        [DataMember(Name = "certificate", Order = 4,
            EmitDefaultValue = false)]
        public string? Certificate { get; set; }

        /// <summary>
        /// Use selected publisher for discovery or if not
        /// specified the first publisher that can discover
        /// </summary>
        [DataMember(Name = "discovererId", Order = 5,
            EmitDefaultValue = false)]
        public string? DiscovererId { get; set; }
    }
}
