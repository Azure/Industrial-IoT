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
        Timestamp = 1,

        /// <summary>
        /// Picoseconds (uadp)
        /// </summary>
        [EnumMember]
        PicoSeconds = 2,

        /// <summary>
        /// Metadata version (json)
        /// </summary>
        [EnumMember]
        MetaDataVersion = 4,

        /// <summary>
        /// Status
        /// </summary>
        [EnumMember]
        Status = 8,

        /// <summary>
        /// Dataset writer id (json)
        /// </summary>
        [EnumMember]
        DataSetWriterId = 16,

        /// <summary>
        /// Major version (uadp)
        /// </summary>
        [EnumMember]
        MajorVersion = 32,

        /// <summary>
        /// Minor version (uadp)
        /// </summary>
        [EnumMember]
        MinorVersion = 64,

        /// <summary>
        /// Sequence number
        /// </summary>
        [EnumMember]
        SequenceNumber = 128
    }
}