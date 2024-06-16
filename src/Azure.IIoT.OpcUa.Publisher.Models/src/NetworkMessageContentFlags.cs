// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Network message content
    /// </summary>
    [DataContract]
    [Flags]
    public enum NetworkMessageContentFlags
    {
        /// <summary>
        /// Publisher id
        /// </summary>
        [EnumMember(Value = "PublisherId")]
        PublisherId = 0x1,

        /// <summary>
        /// Group header
        /// </summary>
        [EnumMember(Value = "GroupHeader")]
        GroupHeader = 0x2,

        /// <summary>
        /// Writer group id
        /// </summary>
        [EnumMember(Value = "WriterGroupId")]
        WriterGroupId = 0x4,

        /// <summary>
        /// Group version
        /// </summary>
        [EnumMember(Value = "GroupVersion")]
        GroupVersion = 0x8,

        /// <summary>
        /// Network message number
        /// </summary>
        [EnumMember(Value = "NetworkMessageNumber")]
        NetworkMessageNumber = 0x10,

        /// <summary>
        /// Sequence number
        /// </summary>
        [EnumMember(Value = "SequenceNumber")]
        SequenceNumber = 0x20,

        /// <summary>
        /// Payload header
        /// </summary>
        [EnumMember(Value = "PayloadHeader")]
        PayloadHeader = 0x40,

        /// <summary>
        /// Timestamp
        /// </summary>
        [EnumMember(Value = "Timestamp")]
        Timestamp = 0x80,

        /// <summary>
        /// Picoseconds
        /// </summary>
        [EnumMember(Value = "Picoseconds")]
        Picoseconds = 0x100,

        /// <summary>
        /// Dataset class id
        /// </summary>
        [EnumMember(Value = "DataSetClassId")]
        DataSetClassId = 0x200,

        /// <summary>
        /// Promoted fields
        /// </summary>
        [EnumMember(Value = "PromotedFields")]
        PromotedFields = 0x400,

        /// <summary>
        /// Network message header
        /// </summary>
        [EnumMember(Value = "NetworkMessageHeader")]
        NetworkMessageHeader = 0x800,

        /// <summary>
        /// Dataset message header
        /// </summary>
        [EnumMember(Value = "DataSetMessageHeader")]
        DataSetMessageHeader = 0x1000,

        /// <summary>
        /// Single dataset messages
        /// </summary>
        [EnumMember(Value = "SingleDataSetMessage")]
        SingleDataSetMessage = 0x2000,

        /// <summary>
        /// Reply to
        /// </summary>
        [EnumMember(Value = "ReplyTo")]
        ReplyTo = 0x4000,

        /// <summary>
        /// Wrap messages in array (publisher extension)
        /// </summary>
        [EnumMember(Value = "UseArrayEnvelope")]
        UseArrayEnvelope = 0x1000000,

        /// <summary>
        /// Use compatibility mode with 2.8 (publisher extension)
        /// </summary>
        [EnumMember(Value = "UseCompatibilityMode")]
        UseCompatibilityMode = 0x2000000,

        /// <summary>
        /// Monitored item message (publisher extension)
        /// </summary>
        [EnumMember(Value = "MonitoredItemMessage")]
        MonitoredItemMessage = 0x8000000,
    }
}
