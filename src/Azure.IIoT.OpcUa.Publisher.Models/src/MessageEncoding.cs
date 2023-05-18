// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
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
        [EnumMember(Value = "Binary")]
        Binary = 0x1,

        /// <summary>
        /// The encoding is based on Json
        /// </summary>
        [EnumMember(Value = "Json")]
        Json = 0x2,

        /// <summary>
        /// The encoding is based on Xml
        /// </summary>
        [EnumMember(Value = "Xml")]
        Xml = 0x4,

        /// <summary>
        /// Messages can be decoded to the original data
        /// </summary>
        [EnumMember(Value = "IsReversible")]
        IsReversible = 0x8,

        /// <summary>
        /// Uadp
        /// </summary>
        [EnumMember(Value = "Uadp")]
        Uadp = Binary | IsReversible,

        /// <summary>
        /// Json reversible encoding
        /// </summary>
        [EnumMember(Value = "JsonReversible")]
        JsonReversible = Json | IsReversible,

        /// <summary>
        /// Messages are gzip compressed.
        /// </summary>
        IsGzipCompressed = 0x10,

        /// <summary>
        /// Json gzip
        /// </summary>
        [EnumMember(Value = "JsonGzip")]
        JsonGzip = Json | IsGzipCompressed,

        /// <summary>
        /// Json reversible
        /// </summary>
        [EnumMember(Value = "JsonReversibleGzip")]
        JsonReversibleGzip = JsonGzip | IsReversible,
    }
}
