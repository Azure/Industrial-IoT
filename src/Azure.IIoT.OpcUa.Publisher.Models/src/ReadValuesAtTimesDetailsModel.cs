// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Read data at specified times
    /// </summary>
    [DataContract]
    public sealed record class ReadValuesAtTimesDetailsModel
    {
        /// <summary>
        /// Requested datums
        /// </summary>
        [DataMember(Name = "reqTimes", Order = 0)]
        [Required]
        public required IReadOnlyList<DateTime> ReqTimes { get; set; }

        /// <summary>
        /// Whether to use simple bounds
        /// </summary>
        [DataMember(Name = "useSimpleBounds", Order = 1,
            EmitDefaultValue = false)]
        public bool? UseSimpleBounds { get; set; }
    }
}
