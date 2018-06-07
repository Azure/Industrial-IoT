// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// An array element is an element within an array.
    /// </summary>
    public class ArrayElement<P> : ComplexDataProperty
        where P : Property {

        /// <summary>
        /// Index of the array element.
        /// </summary>
        [JsonProperty(PropertyName = "index")]
        [Required]
        public int Index { get; set; }

        /// <summary>
        /// Array element itself.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        [Required]
        public P Value { get; set; }
    }
}