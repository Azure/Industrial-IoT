// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// The template dictionary contains templates. Typically the
    /// template dictionary of an AAS contains only templates that
    /// are used within the AAS (or its sub-shells).
    /// </summary>
    public class TemplateDictionary : BaseDictionary {

        /// <summary>
        /// [0..*] Templates contained in the dictionary. Resolves
        /// to a <see cref="Template"/>.
        /// </summary>
        [JsonProperty(PropertyName = "template",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Reference> Items { get; set; }
    }
}