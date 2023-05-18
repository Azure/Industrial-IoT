// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Flags that are set by the historian when
    /// returning archived values.
    /// </summary>
    [Flags]
    [DataContract]
    public enum AdditionalData
    {
        /// <summary>
        /// No additional data
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0x0,

        /// <summary>
        /// A data value which was calculated with an
        /// incomplete interval.
        /// </summary>
        [EnumMember(Value = "Partial")]
        Partial = 0x4,

        /// <summary>
        /// A raw data value that hides other data at
        /// the same timestamp.
        /// </summary>
        [EnumMember(Value = "ExtraData")]
        ExtraData = 0x8,

        /// <summary>
        /// Multiple values match the aggregate criteria
        /// (i.e. multiple minimum values at different
        /// timestamps within the same interval)
        /// </summary>
        [EnumMember(Value = "MultipleValues")]
        MultipleValues = 0x10
    }
}
