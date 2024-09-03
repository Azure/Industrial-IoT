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
    /// Application with optional list of endpoints
    /// </summary>
    [DataContract]
    public sealed record class ApplicationRegistrationModel
    {
        /// <summary>
        /// Application information
        /// </summary>
        [DataMember(Name = "application", Order = 0)]
        [Required]
        public required ApplicationInfoModel Application { get; set; }

        /// <summary>
        /// List of endpoints for it
        /// </summary>
        [DataMember(Name = "endpoints", Order = 1,
            EmitDefaultValue = false)]
        public IReadOnlyList<EndpointRegistrationModel>? Endpoints { get; set; }
    }
}
