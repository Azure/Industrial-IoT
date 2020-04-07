// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;

    /// <summary>
    /// Names defined for the ValueRank attribute.
    /// </summary>
    [Flags]
    public enum NodeValueRank {

        /// <summary>
        /// The variable may be a scalar or a one
        /// dimensional array.
        /// </summary>
        ScalarOrOneDimension = -3,

        /// <summary>
        /// The variable may be a scalar or an array
        /// of any dimension.
        /// </summary>
        Any = -2,

        /// <summary>
        /// The variable is always a scalar.
        /// </summary>
        Scalar = -1,

        /// <summary>
        /// The variable is always an array with one
        /// or more dimensions.
        /// </summary>
        OneOrMoreDimensions = 0,

        /// <summary>
        /// The variable is always one dimensional
        /// array.
        /// </summary>
        OneDimension = 1,

        /// <summary>
        /// The variable is always an array with
        /// two or more dimensions.
        /// </summary>
        TwoDimensions = 2
    }

}
