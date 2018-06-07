// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A manifest of a package of packagable items
    /// </summary>
    public class Package : BaseReferable {

        /// <summary>
        /// Meta data of the package
        /// </summary>
        [JsonProperty(PropertyName = "administrativeInformation",
            NullValueHandling = NullValueHandling.Ignore)]
        public AdministrativeInformation AdministrativeInformation { get; set; }

        /// <summary>
        /// Packagable items as content of the package
        /// </summary>
        [JsonProperty(PropertyName = "content")]
        [Required]
        public List<IPackageable> Content { get; set; }
    }
}