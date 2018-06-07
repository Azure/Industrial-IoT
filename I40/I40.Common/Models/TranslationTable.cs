// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Entry in translation table
    /// </summary>
    public class TranslationTable : BaseIdentifiable {

        /// <summary>
        /// [0..*] Set of contained translations for lookup
        /// </summary>
        [JsonProperty(PropertyName = "translations",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Translation> Items { get; set; }
    }
}