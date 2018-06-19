// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Single valued category
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PropertySingleValuedCategory {

        /// <summary>
        /// A static property is an atomic data property that does not
        /// change over time.
        /// In eCl@ss this kind of category has the category “Coded Value”.
        /// </summary>
        Static,

        /// <summary>
        /// A parameter property is an atomic data property that is once
        /// set and then typically does not change over time.
        /// This is for example the case for configuration parameters.
        /// </summary>
        Parameter,

        /// <summary>
        /// A runtime value property is an atomic data property that is
        /// calculated during runtime.
        /// </summary>
        RuntimeValue
    }
}