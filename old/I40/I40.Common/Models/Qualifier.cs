// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// A qualifier is a type-value pair that makes additional
    /// statements w.r.t. the value of the element.
    /// </summary>
    public /* abstract */ class Qualifier : IHasSemanticId {

        /// <summary>
        /// The qualifierType describes the type of the
        /// qualifier that is applied to the element.
        /// </summary>
        [JsonProperty(PropertyName = "qualifierType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string QualifierType { get; set; }

        /// <summary>
        /// The qualifier value is the value of the qualifier.
        /// The value might be complex.
        /// Often the values of a specific qualifier type are
        /// coded values, i.e. a standardized set of values with
        /// defined semantics (i.e. static single valued properties).
        /// In this case the type of the qualifierValue is
        /// a Reference.
        /// </summary>
        [JsonProperty(PropertyName = "qualifierValue",
            NullValueHandling = NullValueHandling.Ignore)]
        public object QualifierValue { get; set; }

        /// <summary>
        /// [0..1] Semantics referred to
        /// </summary>
        [JsonProperty(PropertyName = "id_semantics",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference Semantics { get; set; }
    }
}