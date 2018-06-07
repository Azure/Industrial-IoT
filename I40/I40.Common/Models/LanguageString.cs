// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Mulit-Language, structured. 
    /// </summary>
    public class LanguageString : BaseReferable {

        /// <summary>
        /// A language identifier, such as de or en-US.
        /// </summary>
        [JsonProperty(PropertyName = "langId")]
        [Required]
        public string LangId { get; set; }

        /// <summary>
        /// Text
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        [Required]
        public string Text { get; set; }
    }
}