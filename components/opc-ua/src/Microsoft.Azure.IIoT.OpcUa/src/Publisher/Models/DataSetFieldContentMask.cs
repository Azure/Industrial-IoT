// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;

    /// <summary>
    /// Content for dataset field
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataSetFieldContentMask {

        /// <summary>
        /// Status code
        /// </summary>
        StatusCode = 0x1,

        /// <summary>
        /// Source timestamp
        /// </summary>
        SourceTimestamp = 0x2,

        /// <summary>
        /// Server timestamp
        /// </summary>
        ServerTimestamp = 0x4,

        /// <summary>
        /// Source picoseconds
        /// </summary>
        SourcePicoSeconds = 0x8,

        /// <summary>
        /// Server picoseconds
        /// </summary>
        ServerPicoSeconds = 0x10,

        /// <summary>
        /// Raw value
        /// </summary>
        RawData = 0x20
    }
}