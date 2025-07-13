// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Specifies the encoding format for OPC UA Publisher messages.
    /// Can be combined with compression and reversibility flags to control
    /// message format characteristics.
    /// </summary>
    [DataContract]
    [Flags]
    public enum MessageEncoding
    {
        /// <summary>
        /// OPC UA binary encoding format (UADP).
        /// Most efficient format with smallest message size.
        /// Best choice when receivers can handle binary OPC UA encoding.
        /// </summary>
        [EnumMember(Value = "Uadp")]
        Uadp = 0x1,

        /// <summary>
        /// Standard JSON encoding format.
        /// Human-readable format suitable for most messaging systems.
        /// Default encoding when not specified otherwise.
        /// May lose some OPC UA type information.
        /// </summary>
        [EnumMember(Value = "Json")]
        Json = 0x2,

        /// <summary>
        /// XML encoding format.
        /// Most verbose but highly compatible format.
        /// Provides good interoperability with legacy systems.
        /// </summary>
        [EnumMember(Value = "Xml")]
        Xml = 0x4,

        /// <summary>
        /// Apache Avro binary encoding format.
        /// Schema-based encoding that supports evolution.
        /// Provides good balance between size and compatibility.
        /// </summary>
        [EnumMember(Value = "Avro")]
        Avro = 0x8,

        /// <summary>
        /// Flag indicating that messages preserve all OPC UA type information
        /// and can be decoded back to the original data structure.
        /// Use with Json or other encodings to ensure lossless transmission.
        /// </summary>
        [EnumMember(Value = "IsReversible")]
        IsReversible = 0x40,

        /// <summary>
        /// JSON encoding that preserves all OPC UA type information.
        /// Combination of Json and IsReversible flags.
        /// Allows exact reconstruction of original OPC UA data types.
        /// Larger message size than standard JSON encoding.
        /// </summary>
        [EnumMember(Value = "JsonReversible")]
        JsonReversible = Json | IsReversible,

        /// <summary>
        /// Flag indicating that messages should be compressed using GZIP.
        /// Reduces bandwidth usage at the cost of additional processing.
        /// Most effective with text-based encodings like JSON and XML.
        /// </summary>
        IsGzipCompressed = 0x80,

        /// <summary>
        /// GZIP compressed JSON encoding.
        /// Combination of Json and IsGzipCompressed flags.
        /// Provides good compromise between size and readability.
        /// Recommended for bandwidth-constrained scenarios.
        /// </summary>
        [EnumMember(Value = "JsonGzip")]
        JsonGzip = Json | IsGzipCompressed,

        /// <summary>
        /// GZIP compressed Avro encoding.
        /// Combination of Avro and IsGzipCompressed flags.
        /// Maximum compression for binary format.
        /// Best choice for minimal bandwidth usage.
        /// </summary>
        [EnumMember(Value = "AvroGzip")]
        AvroGzip = IsGzipCompressed | Avro,

        /// <summary>
        /// GZIP compressed JSON encoding with full type preservation.
        /// Combines Json, IsReversible and IsGzipCompressed flags.
        /// Ensures lossless data transmission with minimal bandwidth.
        /// Recommended for reliable systems integration.
        /// </summary>
        [EnumMember(Value = "JsonReversibleGzip")]
        JsonReversibleGzip = JsonGzip | IsReversible,
    }
}
