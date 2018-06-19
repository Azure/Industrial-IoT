// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace I40.DIN91345.Models.Templates {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Property container template
    /// </summary>
    public class Template_PropertyContainer : ITemplateContent {

        /// <summary>
        /// [0..1] Carrier
        /// </summary>
        [JsonProperty(PropertyName = "id_carrierAll",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference CarrierAll { get; set; }

        /// <summary>
        /// [0..1] Semantics
        /// </summary>
        [JsonProperty(PropertyName = "id_semanticsAll",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference SemanticsAll { get; set; }

        /// <summary>
        /// [0..*] Qualifiers
        /// </summary>
        [JsonProperty(PropertyName = "qualifierAll",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Qualifier> QualifierAll { get; set; }
    }
}