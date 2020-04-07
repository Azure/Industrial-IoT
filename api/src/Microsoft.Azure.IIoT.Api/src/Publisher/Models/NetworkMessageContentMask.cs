// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Network message content
    /// </summary>
    [DataContract]
    public enum NetworkMessageContentMask {

        /// <summary>
        /// Publisher id
        /// </summary>
        [EnumMember]
        PublisherId = 1,

        /// <summary>
        /// Group header
        /// </summary>
        [EnumMember]
        GroupHeader = 2,

        /// <summary>
        /// Writer group id
        /// </summary>
        [EnumMember]
        WriterGroupId = 4,

        /// <summary>
        /// Group version
        /// </summary>
        [EnumMember]
        GroupVersion = 8,

        /// <summary>
        /// Network message number
        /// </summary>
        [EnumMember]
        NetworkMessageNumber = 16,

        /// <summary>
        /// Sequence number
        /// </summary>
        [EnumMember]
        SequenceNumber = 32,

        /// <summary>
        /// Payload header
        /// </summary>
        [EnumMember]
        PayloadHeader = 64,

        /// <summary>
        /// Timestamp
        /// </summary>
        [EnumMember]
        Timestamp = 128,

        /// <summary>
        /// Picoseconds
        /// </summary>
        [EnumMember]
        Picoseconds = 256,

        /// <summary>
        /// Dataset class id
        /// </summary>
        [EnumMember]
        DataSetClassId = 512,

        /// <summary>
        /// Promoted fields
        /// </summary>
        [EnumMember]
        PromotedFields = 1024,

        /// <summary>
        /// Network message header
        /// </summary>
        [EnumMember]
        NetworkMessageHeader = 2048,

        /// <summary>
        /// Dataset message header
        /// </summary>
        [EnumMember]
        DataSetMessageHeader = 4096,

        /// <summary>
        /// Single dataset messages
        /// </summary>
        [EnumMember]
        SingleDataSetMessage = 8192,

        /// <summary>
        /// Reply to
        /// </summary>
        [EnumMember]
        ReplyTo = 16384
    }
}