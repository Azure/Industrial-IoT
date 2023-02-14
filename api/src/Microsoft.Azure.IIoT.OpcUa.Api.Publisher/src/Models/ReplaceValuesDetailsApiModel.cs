// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Replace historic data
    /// </summary>
    [DataContract]
    public class ReplaceValuesDetailsApiModel {

        /// <summary>
        /// Values to replace
        /// </summary>
        [DataMember(Name = "values", Order = 0)]
        [Required]
        public List<HistoricValueApiModel> Values { get; set; }
    }
}
