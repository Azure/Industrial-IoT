// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Flags that can be set for the AccessLevel attribute.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeAccessLevel {

        /// <summary>
        /// The current value of the Variable may be read.
        /// </summary>
        CurrentRead = 0x1,

        /// <summary>
        /// The current value of the Variable may be written.
        /// </summary>
        CurrentWrite = 0x2,

        /// <summary>
        /// The history for the Variable may be read.
        /// </summary>
        HistoryRead = 0x4,

        /// <summary>
        /// The history for the Variable may be updated.
        /// </summary>
        HistoryWrite = 0x8,

        /// <summary>
        /// Indicates if the Variable generates
        /// SemanticChangeEvents when its value changes.
        /// </summary>
        SemanticChange = 0x10,

        /// <summary>
        /// Indicates if the current StatusCode of the
        /// value is writable.
        /// </summary>
        StatusWrite = 0x20,

        /// <summary>
        /// Indicates if the current SourceTimestamp is
        /// writable.
        /// </summary>
        TimestampWrite = 0x40
    }
}
