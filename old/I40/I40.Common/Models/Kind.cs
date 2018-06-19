// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Item kind
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Kind {

        /// <summary>
        /// hardware or software element which specifies
        /// the common attributes shared by all instances
        /// of the type [SOURCE: IEC TR 62390:2005-01, 3.1.25]
        /// </summary>
        Type,

        /// <summary>
        /// concrete, clearly identifiable component of a
        /// certain type
        /// Note 1 to entry: It becomes an individual entity
        /// of a type, for example a device, by defining
        /// specific property values.
        /// Note 2 to entry: In an object oriented view,
        /// an instance denotes an object of a class (of a
        /// type). [SOURCE: IEC 62890:2016, 3.1.16] 65/617/CDV
        /// </summary>
        Instance
    }
}