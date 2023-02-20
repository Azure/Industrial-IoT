// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Replace historic data
    /// </summary>
    [DataContract]
    public record class ReplaceValuesDetailsModel {

        /// <summary>
        /// Values to replace
        /// </summary>
        [DataMember(Name = "values", Order = 0)]
        [Required]
        public List<HistoricValueModel> Values { get; set; }
    }
}
