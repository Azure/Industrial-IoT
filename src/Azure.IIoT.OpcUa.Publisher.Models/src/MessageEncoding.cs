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
        /// The encoding is Binary
        /// </summary>
        [EnumMember(Value = "Uadp")]
        Uadp = 0x1,

        /// <summary>
        /// The encoding is Json
        /// </summary>
        [EnumMember(Value = "Json")]
        Json = 0x2,

        /// <summary>
        /// The encoding is Xml
        /// </summary>
        [EnumMember(Value = "Xml")]
        Xml = 0x4,

        /// <summary>
        /// The encoding is Avro
        /// </summary>
        [EnumMember(Value = "Avro")]
        Avro = 0x8,

        /// <summary>
        /// Messages can be decoded to the original data
        /// </summary>
        [EnumMember(Value = "IsReversible")]
        IsReversible = 0x40,

        /// <summary>
        /// Json reversible encoding
        /// </summary>
        [EnumMember(Value = "JsonReversible")]
        JsonReversible = Json | IsReversible,

        /// <summary>
        /// Messages are gzip compressed.
        /// </summary>
        IsGzipCompressed = 0x80,

        /// <summary>
        /// Json gzip
        /// </summary>
        [EnumMember(Value = "JsonGzip")]
        JsonGzip = Json | IsGzipCompressed,

        /// <summary>
        /// Avro Gzip
        /// </summary>
        [EnumMember(Value = "AvroGzip")]
        AvroGzip = IsGzipCompressed | Avro,

        /// <summary>
        /// Json reversible
        /// </summary>
        [EnumMember(Value = "JsonReversibleGzip")]
        JsonReversibleGzip = JsonGzip | IsReversible,
    }
}
