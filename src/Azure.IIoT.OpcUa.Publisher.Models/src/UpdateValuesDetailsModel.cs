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
    /// Insert, upsert, or update historic values
    /// </summary>
    [DataContract]
    public sealed record class UpdateValuesDetailsModel
    {
        /// <summary>
        /// Values to insert
        /// </summary>
        [DataMember(Name = "values", Order = 0)]
        [Required]
        public required IReadOnlyList<HistoricValueModel> Values { get; set; }
    }
}
