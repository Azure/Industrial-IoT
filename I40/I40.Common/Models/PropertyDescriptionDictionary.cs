// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// The property description dictionary contains property descriptions.
    /// Typically the property description dictionary of an AAS contains
    /// only property descriptions of properties used within submodels
    /// of the AAS (or its sub-shells).
    /// </summary>
    public class PropertyDescriptionDictionary : BaseDictionary,
        IDictionary {

        /// <summary>
        /// [0..*] Set of contained property descriptions within
        /// the dictionary.
        /// Resolves to a <see cref="PropertyDescription"/>
        /// </summary>
        [JsonProperty(PropertyName = "propertyDescription",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Reference> Items { get; set; }
    }
}