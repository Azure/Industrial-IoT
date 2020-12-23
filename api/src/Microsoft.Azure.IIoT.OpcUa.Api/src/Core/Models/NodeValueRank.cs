// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Constants defined for the ValueRank attribute.
    /// </summary>
    [Flags]
    [DataContract]
    public enum NodeValueRank {

        /// <summary>
        /// The variable may be a scalar or a one
        /// dimensional array.
        /// </summary>
        [EnumMember]
        ScalarOrOneDimension = -3,

        /// <summary>
        /// The variable may be a scalar or an array of
        /// any dimension.
        /// </summary>
        [EnumMember]
        Any = -2,

        /// <summary>
        /// The variable is always a scalar.
        /// </summary>
        [EnumMember]
        Scalar = -1,

        /// <summary>
        /// The variable is always an array with one or
        /// more dimensions.
        /// </summary>
        [EnumMember]
        OneOrMoreDimensions = 0,

        /// <summary>
        /// The variable is always one dimensional array.
        /// </summary>
        [EnumMember]
        OneDimension = 1,

        /// <summary>
        /// The variable is always an array with two or
        /// more dimensions.
        /// </summary>
        [EnumMember]
        TwoDimensions = 2
    }
}
