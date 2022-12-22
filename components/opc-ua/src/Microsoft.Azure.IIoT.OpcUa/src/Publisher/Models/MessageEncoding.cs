// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Message encoding
    /// </summary>
    public enum MessageEncoding {

        /// <summary>
        /// Json non-reversible encoding
        /// </summary>
        Json,

        /// <summary>
        /// Json non-reversible encoding
        /// </summary>
        JsonNonReversible = Json,

        /// <summary>
        /// Uadp or Binary encoding
        /// </summary>
        Uadp,

        /// <summary>
        /// Binary encoding
        /// </summary>
        Binary = Uadp,

        /// <summary>
        /// Json reversible encoding
        /// </summary>
        JsonReversible,
    }
}
