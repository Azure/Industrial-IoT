// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Replace historic data
    /// </summary>
    public class ReplaceValuesDetailsApiModel {

        /// <summary>
        /// Values to replace
        /// </summary>
        [JsonProperty(PropertyName = "values")]
        [Required]
        public List<HistoricValueApiModel> Values { get; set; }
    }
}
