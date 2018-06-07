// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Records properties represent heterogeneous elements
    /// belonging together is a structured hierarchical way.
    /// The elements again are properties.
    /// </summary>
    public class Record : ComplexDataProperty {

        /// <summary>
        /// [1..*] ordered. Elements within the record.
        /// The elements may be of heterogenous type.
        /// </summary>
        [JsonProperty(PropertyName = "elements",
            ItemIsReference = false)]
        [Required]
        public List<RecordElement> Elements { get; set; }
    }
}