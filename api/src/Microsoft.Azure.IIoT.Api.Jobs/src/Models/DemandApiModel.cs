// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Demand model
    /// </summary>
    public class DemandApiModel {

        /// <summary>
        /// Key
        /// </summary>
        [JsonProperty(PropertyName = "key")]
        [Required]
        public string Key { get; set; }

        /// <summary>
        /// Match operator
        /// </summary>
        [JsonProperty(PropertyName = "operator",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(DemandOperators.Equals)]
        public DemandOperators? Operator { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [JsonProperty(PropertyName = "value",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Value { get; set; }
    }
}