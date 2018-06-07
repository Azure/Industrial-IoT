
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;

    /// <inheritdoc/>
    public abstract class BaseSemanticModel : BaseAsset, IHasSemanticId {

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "id_semantics",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference Semantics { get; set; }
    }
}