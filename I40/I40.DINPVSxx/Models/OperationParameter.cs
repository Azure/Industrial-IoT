// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// An operation parameter is a property that is used as input
    /// or output parameter of an operation.
    /// </summary>
    public class OperationParameter : Property {

        /// <summary>
        /// Reference to a Property.
        /// </summary>
        [JsonProperty(PropertyName = "ref_Property")]
        [Required]
        public Property Property { get; set; }
    }
}