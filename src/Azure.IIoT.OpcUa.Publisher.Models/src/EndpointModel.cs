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
    /// Endpoint model
    /// </summary>
    [DataContract]
    public sealed record class EndpointModel
    {
        /// <summary>
        /// Endpoint url to use to connect with
        /// </summary>
        [DataMember(Name = "url", Order = 0)]
        [Required]
        public required string Url { get; set; }

        /// <summary>
        /// Alternative endpoint urls that can be used for
        /// accessing and validating the server
        /// </summary>
        [DataMember(Name = "alternativeUrls", Order = 1,
            EmitDefaultValue = false)]
        public IReadOnlySet<string>? AlternativeUrls { get; set; }

        /// <summary>
        /// Security Mode to use for communication.
        /// default to best.
        /// </summary>
        [DataMember(Name = "securityMode", Order = 2,
            EmitDefaultValue = false)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security policy uri to use for communication.
        /// default to best.
        /// </summary>
        [DataMember(Name = "securityPolicy", Order = 3,
            EmitDefaultValue = false)]
        public string? SecurityPolicy { get; set; }

        /// <summary>
        /// Endpoint certificate thumbprint
        /// </summary>
        [DataMember(Name = "certificate", Order = 4,
            EmitDefaultValue = false)]
        public string? Certificate { get; set; }
    }
}
