// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;

    /// <summary>
    /// Data set message content
    /// </summary>
    [Flags]
    public enum DataSetContentMask {

        /// <summary>
        /// Timestamp
        /// </summary>
        Timestamp = 0x1,

        /// <summary>
        /// Picoseconds (uadp)
        /// </summary>
        PicoSeconds = 0x2,

        /// <summary>
        /// Metadata version (json)
        /// </summary>
        MetaDataVersion = 0x4,

        /// <summary>
        /// Status
        /// </summary>
        Status = 0x8,

        /// <summary>
        /// Dataset writer id (json)
        /// </summary>
        DataSetWriterId = 0x10,

        /// <summary>
        /// Major version (uadp)
        /// </summary>
        MajorVersion = 0x20,

        /// <summary>
        /// Minor version (uadp)
        /// </summary>
        MinorVersion = 0x40,

        /// <summary>
        /// Sequence number
        /// </summary>
        SequenceNumber = 0x80
    }
}