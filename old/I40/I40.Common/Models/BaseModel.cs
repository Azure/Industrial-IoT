
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;

    /// <inheritdoc/>
    public abstract class BaseModel : BaseSemanticModel,
        IQualifiable {

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "qualifier",
            NullValueHandling = NullValueHandling.Ignore)]
        public Qualifier Qualifier { get; set; }
    }
}