// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Endpoint info
    /// </summary>
    [DataContract]
    public sealed record class EndpointInfoModel
    {
        /// <summary>
        /// Endpoint registration
        /// </summary>
        [DataMember(Name = "registration", Order = 0)]
        [Required]
        public required EndpointRegistrationModel Registration { get; set; }

        /// <summary>
        /// Application id endpoint is registered under.
        /// </summary>
        [DataMember(Name = "applicationId", Order = 1)]
        [Required]
        public required string ApplicationId { get; set; }

        /// <summary>
        /// Last state of the endpoint
        /// </summary>
        [DataMember(Name = "endpointState", Order = 3,
            EmitDefaultValue = false)]
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Last time endpoint was seen
        /// </summary>
        [DataMember(Name = "notSeenSince", Order = 5,
            EmitDefaultValue = false)]
        public DateTimeOffset? NotSeenSince { get; set; }
    }
}
