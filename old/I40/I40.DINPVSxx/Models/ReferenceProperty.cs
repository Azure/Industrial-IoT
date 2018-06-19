// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Reference property referring to a <see cref="IReferable"/>
    /// </summary>
    public class ReferenceProperty : Property {

        /// <summary>
        /// Reference to an element.  Resolves to a
        /// <see cref="IReferable"/> element.
        /// </summary>
        [JsonProperty(PropertyName = "reference")]
        [Required]
        public Reference Reference { get; set; }
    }
}