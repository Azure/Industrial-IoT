// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// A property container consists of a set of properties.
    /// </summary>
    public class PropertyContainer : Property {

        /// <summary>
        /// [0..*] Properties belonging to the property container.
        /// A property belongs to exactly one property container.
        /// </summary>
        [JsonProperty(PropertyName = "containedElements",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<Property> Properties { get; set; }
    }
}