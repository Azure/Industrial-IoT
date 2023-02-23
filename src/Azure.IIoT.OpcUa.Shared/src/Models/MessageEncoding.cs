// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Message encoding
    /// </summary>
    [DataContract]
    [Flags]
    public enum MessageEncoding
    {
        /// <summary>
        /// The encoding is binary.
        /// </summary>
        [EnumMember]
        Binary = 0x1,

        /// <summary>
        /// The encoding is based on Json
        /// </summary>
        [EnumMember]
        Json = 0x2,

        /// <summary>
        /// The encoding is based on Xml
        /// </summary>
        [EnumMember]
        Xml = 0x4,

        /// <summary>
        /// Messages can be decoded to the original data
        /// </summary>
        [EnumMember]
        IsReversible = 0x8,

        /// <summary>
        /// Uadp
        /// </summary>
        [EnumMember]
        Uadp = Binary | IsReversible,

        /// <summary>
        /// Json reversible encoding
        /// </summary>
        [EnumMember]
        JsonReversible = Json | IsReversible,

        /// <summary>
        /// Messages are gzip compressed.
        /// </summary>
        IsGzipCompressed = 0x10,

        /// <summary>
        /// Json gzip
        /// </summary>
        [EnumMember]
        JsonGzip = Json | IsGzipCompressed,

        /// <summary>
        /// Json reversible
        /// </summary>
        [EnumMember]
        JsonReversibleGzip = JsonGzip | IsReversible,
    }
}
