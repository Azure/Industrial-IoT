// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;

    /// <summary>
    /// Network message content
    /// </summary>
    [Flags]
    public enum NetworkMessageContentMask {

        /// <summary>
        /// Publisher id
        /// </summary>
        PublisherId = 0x1,

        /// <summary>
        /// Group header
        /// </summary>
        GroupHeader = 0x2,

        /// <summary>
        /// Writer group id
        /// </summary>
        WriterGroupId = 0x4,

        /// <summary>
        /// Group version
        /// </summary>
        GroupVersion = 0x8,

        /// <summary>
        /// Network message number
        /// </summary>
        NetworkMessageNumber = 0x10,

        /// <summary>
        /// Sequence number
        /// </summary>
        SequenceNumber = 0x20,

        /// <summary>
        /// Payload header
        /// </summary>
        PayloadHeader = 0x40,

        /// <summary>
        /// Timestamp
        /// </summary>
        Timestamp = 0x80,

        /// <summary>
        /// Picoseconds
        /// </summary>
        Picoseconds = 0x100,

        /// <summary>
        /// Dataset class id
        /// </summary>
        DataSetClassId = 0x200,

        /// <summary>
        /// Promoted fields
        /// </summary>
        PromotedFields = 0x400,

        /// <summary>
        /// Network message header
        /// </summary>
        NetworkMessageHeader = 0x800,

        /// <summary>
        /// Dataset message header
        /// </summary>
        DataSetMessageHeader = 0x1000,

        /// <summary>
        /// Single dataset messages
        /// </summary>
        SingleDataSetMessage = 0x2000,

        /// <summary>
        /// Reply to
        /// </summary>
        ReplyTo = 0x4000
    }
}