// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Node class
    /// </summary>
    [DataContract]
    public enum NodeClass
    {
        /// <summary>
        /// Object class
        /// </summary>
        [EnumMember(Value = "Object")]
#pragma warning disable CA1720 // Identifier contains type name
        Object,
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Variable
        /// </summary>
        [EnumMember(Value = "Variable")]
        Variable,

        /// <summary>
        /// Method class
        /// </summary>
        [EnumMember(Value = "Method")]
        Method,

        /// <summary>
        /// Object type
        /// </summary>
        [EnumMember(Value = "ObjectType")]
        ObjectType,

        /// <summary>
        /// Variable type
        /// </summary>
        [EnumMember(Value = "VariableType")]
        VariableType,

        /// <summary>
        /// Reference type
        /// </summary>
        [EnumMember(Value = "ReferenceType")]
        ReferenceType,

        /// <summary>
        /// Data type
        /// </summary>
        [EnumMember(Value = "DataType")]
        DataType,

        /// <summary>
        /// View class
        /// </summary>
        [EnumMember(Value = "View")]
        View
    }
}
