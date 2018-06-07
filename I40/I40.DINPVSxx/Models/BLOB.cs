// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// A blob property is a file that is contained with its
    /// source code in the value attribute.
    /// Constraint AAS-010: For properties of categoryBlob a
    /// qualifier of type “mimetype” shall be added.
    /// </summary>
    public class BLOB : Property {

        /// <summary>
        /// The value of the property instance of a blob property.
        /// Note: In contrast to the file property the file is
        /// stored directly as value in the Blob property.
        /// </summary>
        [JsonProperty(PropertyName = "value",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Value { get; set; }
    }
}