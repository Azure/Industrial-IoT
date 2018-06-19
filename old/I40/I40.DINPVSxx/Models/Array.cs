// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Array property (all properties of same type P)
    /// </summary>
    public class Array<P> : ComplexDataProperty where P : Property {

        /// <summary>
        /// Size of the array, i.e. number of elements
        /// contained in the array.
        /// </summary>
        [JsonProperty(PropertyName = "arraySize",
            NullValueHandling = NullValueHandling.Ignore)]
        public int ArraySize { get; set; }

        /// <summary>
        /// [0..*] Elements within the array. The elements
        /// are of the same type P.
        /// </summary>
        [JsonProperty(PropertyName = "element",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<ArrayElement<P>> Elements { get; set; }
    }
}