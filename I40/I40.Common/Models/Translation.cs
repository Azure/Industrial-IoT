// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Entry in translation table
    /// </summary>
    public class Translation {

        /// <summary>
        /// Original text
        /// </summary>
        [JsonProperty(PropertyName = "original")]
        [Required]
        public string OriginalText { get; set; }

        /// <summary>
        /// Translated text
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        [Required]
        public string TranslatedText { get; set; }
    }
}