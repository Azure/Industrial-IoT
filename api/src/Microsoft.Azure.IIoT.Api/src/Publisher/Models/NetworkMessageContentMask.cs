// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Network message content
    /// </summary>
    [DataContract]
    [Flags]
    public enum NetworkMessageContentMask {

        /// <summary>
        /// Publisher id
        /// </summary>
        [EnumMember]
        PublisherId = 0x1,

        /// <summary>
        /// Group header
        /// </summary>
        [EnumMember]
        GroupHeader = 0x2,

        /// <summary>
        /// Writer group id
        /// </summary>
        [EnumMember]
        WriterGroupId = 0x4,

        /// <summary>
        /// Group version
        /// </summary>
        [EnumMember]
        GroupVersion = 0x8,

        /// <summary>
        /// Network message number
        /// </summary>
        [EnumMember]
        NetworkMessageNumber = 0x10,

        /// <summary>
        /// Sequence number
        /// </summary>
        [EnumMember]
        SequenceNumber = 0x20,

        /// <summary>
        /// Payload header
        /// </summary>
        [EnumMember]
        PayloadHeader = 0x40,

        /// <summary>
        /// Timestamp
        /// </summary>
        [EnumMember]
        Timestamp = 0x80,

        /// <summary>
        /// Picoseconds
        /// </summary>
        [EnumMember]
        Picoseconds = 0x100,

        /// <summary>
        /// Dataset class id
        /// </summary>
        [EnumMember]
        DataSetClassId = 0x200,

        /// <summary>
        /// Promoted fields
        /// </summary>
        [EnumMember]
        PromotedFields = 0x400,

        /// <summary>
        /// Network message header
        /// </summary>
        [EnumMember]
        NetworkMessageHeader = 0x800,

        /// <summary>
        /// Dataset message header
        /// </summary>
        [EnumMember]
        DataSetMessageHeader = 0x1000,

        /// <summary>
        /// Single dataset messages
        /// </summary>
        [EnumMember]
        SingleDataSetMessage = 0x2000,

        /// <summary>
        /// Reply to
        /// </summary>
        [EnumMember]
        ReplyTo = 0x4000,

        /// <summary>
        /// Monitored item message (publisher extension)
        /// </summary>
        [EnumMember]
        MonitoredItemMessage = 0x8000000,
    }
}