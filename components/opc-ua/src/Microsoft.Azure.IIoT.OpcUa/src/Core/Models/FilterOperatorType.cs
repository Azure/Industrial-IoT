// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Filter operator type
    /// </summary>
    public enum FilterOperatorType {

        /// <summary>
        /// Equals
        /// </summary>
        Equals,

        /// <summary>
        /// Element == null
        /// </summary>
        IsNull,

        /// <summary>
        /// Greater than
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Less than
        /// </summary>
        LessThan,

        /// <summary>
        /// Greater than or equal
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Less than or equal
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// String match
        /// </summary>
        Like,

        /// <summary>
        /// Logical not
        /// </summary>
        Not,

        /// <summary>
        /// Between
        /// </summary>
        Between,

        /// <summary>
        /// In list
        /// </summary>
        InList,

        /// <summary>
        /// Logical And
        /// </summary>
        And,

        /// <summary>
        /// Logical Or
        /// </summary>
        Or,

        /// <summary>
        /// Cast
        /// </summary>
        Cast,

        /// <summary>
        /// View scope
        /// </summary>
        InView,

        /// <summary>
        /// Type test
        /// </summary>
        OfType,

        /// <summary>
        /// Relationship
        /// </summary>
        RelatedTo,

        /// <summary>
        /// Bitwise and
        /// </summary>
        BitwiseAnd,

        /// <summary>
        /// Bitwise or
        /// </summary>
        BitwiseOr
    }
}