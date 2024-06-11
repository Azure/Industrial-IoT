// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Data set message content
    /// </summary>
    [DataContract]
    [Flags]
    public enum DataSetMessageContentFlags
    {
        /// <summary>
        /// Timestamp
        /// </summary>
        [EnumMember(Value = "Timestamp")]
        Timestamp = 0x1,

        /// <summary>
        /// Picoseconds (uadp)
        /// </summary>
        [EnumMember(Value = "PicoSeconds")]
        PicoSeconds = 0x2,

        /// <summary>
        /// Metadata version (json)
        /// </summary>
        [EnumMember(Value = "MetaDataVersion")]
        MetaDataVersion = 0x4,

        /// <summary>
        /// Status
        /// </summary>
        [EnumMember(Value = "Status")]
        Status = 0x8,

        /// <summary>
        /// Dataset writer id (json)
        /// </summary>
        [EnumMember(Value = "DataSetWriterId")]
        DataSetWriterId = 0x10,

        /// <summary>
        /// Major version (uadp)
        /// </summary>
        [EnumMember(Value = "MajorVersion")]
        MajorVersion = 0x20,

        /// <summary>
        /// Minor version (uadp)
        /// </summary>
        [EnumMember(Value = "MinorVersion")]
        MinorVersion = 0x40,

        /// <summary>
        /// Sequence number
        /// </summary>
        [EnumMember(Value = "SequenceNumber")]
        SequenceNumber = 0x80,

        /// <summary>
        /// Default Uadp
        /// </summary>
        [EnumMember(Value = "DefaultUadp")]
        DefaultUadp = SequenceNumber |
            DataSetWriterId | Timestamp |
            MajorVersion | MinorVersion |
            Status | PicoSeconds,

        /// <summary>
        /// Message type (json)
        /// </summary>
        [EnumMember(Value = "MessageType")]
        MessageType = 0x100,

        /// <summary>
        /// Dataset writer name (json)
        /// </summary>
        [EnumMember(Value = "DataSetWriterName")]
        DataSetWriterName = 0x200,

        /// <summary>
        /// Default non-reversible json
        /// </summary>
        [EnumMember(Value = "DefaultJson")]
        DefaultJson = MessageType | DataSetWriterName |
            SequenceNumber | DataSetWriterId |
            Timestamp | MetaDataVersion | Status,

        /// <summary>
        /// Reversible encoding (json)
        /// </summary>
        [EnumMember(Value = "ReversibleFieldEncoding")]
        ReversibleFieldEncoding = 0x400,
    }
}
