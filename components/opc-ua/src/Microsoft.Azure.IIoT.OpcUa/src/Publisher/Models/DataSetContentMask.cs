// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;

    /// <summary>
    /// Data set message content
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataSetContentMask {

        /// <summary>
        /// Timestamp
        /// </summary>
        Timestamp = 1,

        /// <summary>
        /// Picoseconds (uadp)
        /// </summary>
        PicoSeconds = 2,

        /// <summary>
        /// Metadata version (json)
        /// </summary>
        MetaDataVersion = 4,

        /// <summary>
        /// Status
        /// </summary>
        Status = 8,

        /// <summary>
        /// Dataset writer id (json)
        /// </summary>
        DataSetWriterId = 16,

        /// <summary>
        /// Major version (uadp)
        /// </summary>
        MajorVersion = 32,

        /// <summary>
        /// Minor version (uadp)
        /// </summary>
        MinorVersion = 64,

        /// <summary>
        /// Sequence number
        /// </summary>
        SequenceNumber = 128
    }
}