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
        /// Uadp or Binary encoding
        /// </summary>
        [EnumMember]
        Uadp = 0x1,

        /// <summary>
        /// Json encoding (default)
        /// </summary>
        [EnumMember]
        Json = 0x2,

        /// <summary>
        /// Json reversible encoding
        /// </summary>
        [EnumMember]
        JsonReversible = Json | 0x10,

        /// <summary>
        /// Json gzip
        /// </summary>
        [EnumMember]
        JsonGzip = Json | Gzip,

        /// <summary>
        /// Json reversible
        /// </summary>
        [EnumMember]
        JsonReversibleGzip = JsonReversible | JsonGzip,

        /// <summary>
        /// Gzip flag
        /// </summary>
        Gzip = 0x20
    }
}
