// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception deviation type
    /// </summary>
    [DataContract]
    public enum ExceptionDeviationType
    {
        /// <summary>
        /// Absolute value
        /// </summary>
        [EnumMember(Value = "AbsoluteValue")]
        AbsoluteValue,

        /// <summary>
        /// Percent of value
        /// </summary>
        [EnumMember(Value = "PercentOfValue")]
        PercentOfValue,

        /// <summary>
        /// Percent of a range
        /// </summary>
        [EnumMember(Value = "PercentOfRange")]
        PercentOfRange,

        /// <summary>
        /// Percent of range
        /// </summary>
        [EnumMember(Value = "PercentOfEURange")]
        PercentOfEURange
    }
}
