// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Asset header model
    /// </summary>
    public class Header {

        /// <summary>
        /// [1..*] Assets, ordered
        /// </summary>
        [JsonProperty(PropertyName = "assets",
            ItemIsReference = false)]
        [Required]
        public List<Asset> Assets { get; set; }
    }
}