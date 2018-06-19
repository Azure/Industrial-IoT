// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.Templates {
    using I40.Common.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Property instance template
    /// </summary>
    public class Template_PropertyInstance : ITemplateContent {

        /// <summary>
        /// [0..1] Carrier
        /// </summary>
        [JsonProperty(PropertyName = "id_carrier",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference Carrier { get; set; }
    }
}