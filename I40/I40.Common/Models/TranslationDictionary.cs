// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Translation dictionary for localization purposes. Note that 
    /// the idShort is a language identifier, such as de or en-US.
    /// </summary>
    public class TranslationDictionary : BaseDictionary, IDictionary {

        /// <summary>
        /// [0..*] Set of referenced translation tables
        /// Resolves to <see cref="TranslationTable"/>
        /// </summary>
        [JsonProperty(PropertyName = "ref_translations",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Reference> Items { get; set; }
    }
}