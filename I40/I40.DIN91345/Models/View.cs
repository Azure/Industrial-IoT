// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// A view object in the shell
    /// </summary>
    public class View : BaseReferable, IHasSemanticId {

        // TODO: If this is identification then what is the difference between ref_* and id_*

        /// <summary>
        /// [0..*] Contained elements
        /// Resolve to <see cref="IIdentifiable"/>
        /// </summary>
        [JsonProperty(PropertyName = "ref_containedElement",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Reference> Elements { get; set; }

        /// <summary>
        /// [0..1] Semantics referred to
        /// </summary>
        [JsonProperty(PropertyName = "id_semantics",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference Semantics { get; set; }
    }
}