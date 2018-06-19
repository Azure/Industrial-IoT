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
    /// Composite shell
    /// </summary>
    public class CompositeAssetAdministrationShell : AssetAdministrationShell {

        /// <summary>
        /// [1..*] sub shells
        /// Resolve to <see cref="AssetAdministrationShell"/>
        /// </summary>
        [JsonProperty(PropertyName = "subShells",
            NullValueHandling = NullValueHandling.Ignore)]
        [Required]
        public List<Reference> SubShells { get; set; }
    }
}