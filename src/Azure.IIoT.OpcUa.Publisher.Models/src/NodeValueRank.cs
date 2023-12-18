// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Constants defined for the ValueRank attribute.
    /// </summary>
    [Flags]
    [DataContract]
#pragma warning disable CA2217 // Do not mark enums with FlagsAttribute
    public enum NodeValueRank
#pragma warning restore CA2217 // Do not mark enums with FlagsAttribute
    {
        /// <summary>
        /// The variable may be a scalar or a one
        /// dimensional array.
        /// </summary>
        [EnumMember(Value = "ScalarOrOneDimension")]
        ScalarOrOneDimension = -3,

        /// <summary>
        /// The variable may be a scalar or an array of
        /// any dimension.
        /// </summary>
        [EnumMember(Value = "Any")]
        Any = -2,

        /// <summary>
        /// The variable is always a scalar.
        /// </summary>
        [EnumMember(Value = "Scalar")]
        Scalar = Any | OneDimension,

        /// <summary>
        /// The variable is always an array with one or
        /// more dimensions.
        /// </summary>
        [EnumMember(Value = "OneOrMoreDimensions")]
        OneOrMoreDimensions = 0,

        /// <summary>
        /// The variable is always one dimensional array.
        /// </summary>
        [EnumMember(Value = "OneDimension")]
        OneDimension = 1,

        /// <summary>
        /// The variable is always an array with two or
        /// more dimensions.
        /// </summary>
        [EnumMember(Value = "TwoDimensions")]
        TwoDimensions = 2
    }
}
