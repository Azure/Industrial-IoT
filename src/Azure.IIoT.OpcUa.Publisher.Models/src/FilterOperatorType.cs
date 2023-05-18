// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Filter operator type
    /// </summary>
    [DataContract]
    public enum FilterOperatorType
    {
        /// <summary>
        /// Equals
        /// </summary>
        [EnumMember(Value = "Equals")]
        Equals,

        /// <summary>
        /// Element == null
        /// </summary>
        [EnumMember(Value = "IsNull")]
        IsNull,

        /// <summary>
        /// Greater than
        /// </summary>
        [EnumMember(Value = "GreaterThan")]
        GreaterThan,

        /// <summary>
        /// Less than
        /// </summary>
        [EnumMember(Value = "LessThan")]
        LessThan,

        /// <summary>
        /// Greater than or equal
        /// </summary>
        [EnumMember(Value = "GreaterThanOrEqual")]
        GreaterThanOrEqual,

        /// <summary>
        /// Less than or equal
        /// </summary>
        [EnumMember(Value = "LessThanOrEqual")]
        LessThanOrEqual,

        /// <summary>
        /// String match
        /// </summary>
        [EnumMember(Value = "Like")]
        Like,

        /// <summary>
        /// Logical not
        /// </summary>
        [EnumMember(Value = "Not")]
        Not,

        /// <summary>
        /// Between
        /// </summary>
        [EnumMember(Value = "Between")]
        Between,

        /// <summary>
        /// In list
        /// </summary>
        [EnumMember(Value = "InList")]
        InList,

        /// <summary>
        /// Logical And
        /// </summary>
        [EnumMember(Value = "And")]
        And,

        /// <summary>
        /// Logical Or
        /// </summary>
        [EnumMember(Value = "Or")]
        Or,

        /// <summary>
        /// Cast
        /// </summary>
        [EnumMember(Value = "Cast")]
        Cast,

        /// <summary>
        /// View scope
        /// </summary>
        [EnumMember(Value = "InView")]
        InView,

        /// <summary>
        /// Type test
        /// </summary>
        [EnumMember(Value = "OfType")]
        OfType,

        /// <summary>
        /// Relationship
        /// </summary>
        [EnumMember(Value = "RelatedTo")]
        RelatedTo,

        /// <summary>
        /// Bitwise and
        /// </summary>
        [EnumMember(Value = "BitwiseAnd")]
        BitwiseAnd,

        /// <summary>
        /// Bitwise or
        /// </summary>
        [EnumMember(Value = "BitwiseOr")]
        BitwiseOr
    }
}
