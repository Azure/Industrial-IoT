// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A template serves to define additional attributes that need
    /// to be available within the element using the template.
    /// </summary>
    public class Template : BaseIdentifiable {

        /// <summary>
        /// In the template content part of the template the additional
        /// attributes that need to be available within the element
        /// using the template are defined.
        /// </summary>
        [JsonProperty(PropertyName = "templateContent")]
        [Required]
        public ITemplateContent TemplateContent { get; set; }
    }
}