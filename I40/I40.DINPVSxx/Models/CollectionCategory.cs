// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Multivalue category
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CollectionCategory {

        /// <summary>
        /// A set of properties is a property that describes a set of
        /// elements. The elements again are defined as properties.
        /// Sometimes a set is also called a union type.
        /// </summary>
        Set,

        /// <summary>
        /// A list of properties is a property that describes a list
        /// of elements.
        /// </summary>
        List,

        /// <summary>
        /// A bag is a collection of properties.
        /// </summary>
        Bag
    }
}