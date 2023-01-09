// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;

    /// <summary>
    /// Message encoding
    /// </summary>
    [Flags]
    public enum MessageEncoding {

        /// <summary>
        /// Uadp or Binary encoding
        /// </summary>
        Uadp = 0x1,

        /// <summary>
        /// Json encoding (default)
        /// </summary>
        Json = 0x2,

        /// <summary>
        /// Json reversible
        /// </summary>
        JsonReversible = Json | 0x10,

        /// <summary>
        /// Json gzip
        /// </summary>
        JsonGzip = Json | Gzip,

        /// <summary>
        /// Json reversible
        /// </summary>
        JsonReversibleGzip = JsonReversible | JsonGzip,

        /// <summary>
        /// Gzip flag
        /// </summary>
        Gzip = 0x20
    }
}
