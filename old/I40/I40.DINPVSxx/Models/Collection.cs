// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Many elements either ordered or not
    /// </summary>
    public class Collection : ComplexDataProperty {

        /// <summary>
        /// Category type
        /// </summary>
        [JsonProperty(PropertyName = "category")]
        [Required]
        public CollectionCategory Category { get; set; }

        /// <summary>
        /// Elements contained in the multiple values
        /// property.
        /// </summary>
        [JsonProperty(PropertyName = "containedElements",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<Property> ContainedElements { get; set; }

        /// <summary>
        /// If ordered=false then the elements in the
        /// multiple values properts are not ordered.
        /// If ordered=true then the elements in the
        /// multiple values properts are ordered.
        /// </summary>
        [JsonProperty(PropertyName = "ordered")]
        [Required]
        public bool Ordered { get; set; }
    }
}