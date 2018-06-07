// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <inheritdoc/>
    public abstract class BaseAsset : BaseIdentifiable, IHasTemplate {

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "id_hasTemplate",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Reference> /* Template */ HasTemplates { get; set; }
    }
}