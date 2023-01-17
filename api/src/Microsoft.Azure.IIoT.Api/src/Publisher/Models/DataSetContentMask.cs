// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Data set message content
    /// </summary>
    [DataContract]
    [Flags]
    public enum DataSetContentMask {

        /// <summary>
        /// Timestamp
        /// </summary>
        [EnumMember]
        Timestamp = 0x1,

        /// <summary>
        /// Picoseconds (uadp)
        /// </summary>
        [EnumMember]
        PicoSeconds = 0x2,

        /// <summary>
        /// Metadata version (json)
        /// </summary>
        [EnumMember]
        MetaDataVersion = 0x4,

        /// <summary>
        /// Status
        /// </summary>
        [EnumMember]
        Status = 0x8,

        /// <summary>
        /// Dataset writer id (json)
        /// </summary>
        [EnumMember]
        DataSetWriterId = 0x10,

        /// <summary>
        /// Major version (uadp)
        /// </summary>
        [EnumMember]
        MajorVersion = 0x20,

        /// <summary>
        /// Minor version (uadp)
        /// </summary>
        [EnumMember]
        MinorVersion = 0x40,

        /// <summary>
        /// Sequence number
        /// </summary>
        [EnumMember]
        SequenceNumber = 0x80,

        /// <summary>
        /// Message type (json)
        /// </summary>
        [EnumMember]
        MessageType = 0x100,

        /// <summary>
        /// Dataset writer name (json)
        /// </summary>
        [EnumMember]
        DataSetWriterName = 0x200,

        /// <summary>
        /// Reversible encoding (json)
        /// </summary>
        [EnumMember]
        ReversibleFieldEncoding = 0x400,
    }
}