// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Message encoding
    /// </summary>
    [DataContract]
    public enum MessageEncoding {

        /// <summary>
        /// Json non-reversible encoding
        /// </summary>
        [EnumMember]
        Json,

        /// <summary>
        /// Json non-reversible encoding
        /// </summary>
        [EnumMember]
        JsonNonReversible = Json,

        /// <summary>
        /// Uadp or Binary encoding
        /// </summary>
        [EnumMember]
        Uadp,

        /// <summary>
        /// Binary encoding
        /// </summary>
        [EnumMember]
        Binary = Uadp,

        /// <summary>
        /// Json reversible encoding
        /// </summary>
        [EnumMember]
        JsonReversible,
    }
}
