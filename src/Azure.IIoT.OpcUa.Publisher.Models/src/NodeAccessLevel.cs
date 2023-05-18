// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Flags that can be set for the AccessLevel attribute.
    /// </summary>
    [Flags]
    [DataContract]
    public enum NodeAccessLevel
    {
        /// <summary>
        /// No access
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0x0,

        /// <summary>
        /// The current value of the Variable may be read.
        /// </summary>
        [EnumMember(Value = "CurrentRead")]
        CurrentRead = 0x1,

        /// <summary>
        /// The current value of the Variable may be written.
        /// </summary>
        [EnumMember(Value = "CurrentWrite")]
        CurrentWrite = 0x2,

        /// <summary>
        /// The history for the Variable may be read.
        /// </summary>
        [EnumMember(Value = "HistoryRead")]
        HistoryRead = 0x4,

        /// <summary>
        /// The history for the Variable may be updated.
        /// </summary>
        [EnumMember(Value = "HistoryWrite")]
        HistoryWrite = 0x8,

        /// <summary>
        /// Indicates if the Variable generates
        /// SemanticChangeEvents when its value changes.
        /// </summary>
        [EnumMember(Value = "SemanticChange")]
        SemanticChange = 0x10,

        /// <summary>
        /// Indicates if the current StatusCode of the
        /// value is writable.
        /// </summary>
        [EnumMember(Value = "StatusWrite")]
        StatusWrite = 0x20,

        /// <summary>
        /// Indicates if the current SourceTimestamp is
        /// writable.
        /// </summary>
        [EnumMember(Value = "TimestampWrite")]
        TimestampWrite = 0x40,

        /// <summary>
        /// Reads are not atomic.
        /// </summary>
        [EnumMember(Value = "NonatomicRead")]
        NonatomicRead = 0x100,

        /// <summary>
        /// Writes are not atomic
        /// </summary>
        [EnumMember(Value = "NonatomicWrite")]
        NonatomicWrite = 0x200,

        /// <summary>
        /// Writes cannot be performed with IndexRange.
        /// </summary>
        [EnumMember(Value = "WriteFullArrayOnly")]
        WriteFullArrayOnly = 0x400,
    }
}
