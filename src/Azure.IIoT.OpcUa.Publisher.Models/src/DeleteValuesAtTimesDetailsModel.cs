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
    /// Deletes data at times
    /// </summary>
    [DataContract]
    public sealed record class DeleteValuesAtTimesDetailsModel
    {
        /// <summary>
        /// The timestamps to delete
        /// </summary>
        [DataMember(Name = "reqTimes", Order = 0)]
        [Required]
        public required IReadOnlyList<DateTime> ReqTimes { get; set; }
    }
}
