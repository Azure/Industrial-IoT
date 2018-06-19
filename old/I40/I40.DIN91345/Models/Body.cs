// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Body of the asset administration shell
    /// </summary>
    public class Body {

        /// <summary>
        /// [0..*] Views. If needed stakeholder specific views
        /// can be defined on the elements of the AAS.
        /// </summary>
        [JsonProperty(PropertyName = "views",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<View> Views { get; set; }

        /// <summary>
        /// [0..*] Submodels (splittable). The asset(s) of an
        /// AAS are described by typically one or more submodels.
        /// Resolves to <see cref="Submodel"/>
        /// </summary>
        [JsonProperty(PropertyName = "ref_submodels",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Reference> SubModels { get; set; }

        // TODO: SubModels in Body is serialized as ref or aggregation?  Doc says ref, but no ref_ name...
    }
}