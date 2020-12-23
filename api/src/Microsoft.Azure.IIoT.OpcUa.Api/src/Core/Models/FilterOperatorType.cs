// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Filter operator type
    /// </summary>
    [DataContract]
    public enum FilterOperatorType {

        /// <summary>
        /// Equals
        /// </summary>
        [EnumMember]
        Equals,

        /// <summary>
        /// Element == null
        /// </summary>
        [EnumMember]
        IsNull,

        /// <summary>
        /// Greater than
        /// </summary>
        [EnumMember]
        GreaterThan,

        /// <summary>
        /// Less than
        /// </summary>
        [EnumMember]
        LessThan,

        /// <summary>
        /// Greater than or equal
        /// </summary>
        [EnumMember]
        GreaterThanOrEqual,

        /// <summary>
        /// Less than or equal
        /// </summary>
        [EnumMember]
        LessThanOrEqual,

        /// <summary>
        /// String match
        /// </summary>
        [EnumMember]
        Like,

        /// <summary>
        /// Logical not
        /// </summary>
        [EnumMember]
        Not,

        /// <summary>
        /// Between
        /// </summary>
        [EnumMember]
        Between,

        /// <summary>
        /// In list
        /// </summary>
        [EnumMember]
        InList,

        /// <summary>
        /// Logical And
        /// </summary>
        [EnumMember]
        And,

        /// <summary>
        /// Logical Or
        /// </summary>
        [EnumMember]
        Or,

        /// <summary>
        /// Cast
        /// </summary>
        [EnumMember]
        Cast,

        /// <summary>
        /// View scope
        /// </summary>
        [EnumMember]
        InView,

        /// <summary>
        /// Type test
        /// </summary>
        [EnumMember]
        OfType,

        /// <summary>
        /// Relationship
        /// </summary>
        [EnumMember]
        RelatedTo,

        /// <summary>
        /// Bitwise and
        /// </summary>
        [EnumMember]
        BitwiseAnd,

        /// <summary>
        /// Bitwise or
        /// </summary>
        [EnumMember]
        BitwiseOr
    }
}