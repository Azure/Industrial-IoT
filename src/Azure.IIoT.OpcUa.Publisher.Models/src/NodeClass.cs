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
        [EnumMember]
#pragma warning disable CA1720 // Identifier contains type name
        Object,
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Variable
        /// </summary>
        [EnumMember]
        Variable,

        /// <summary>
        /// Method class
        /// </summary>
        [EnumMember]
        Method,

        /// <summary>
        /// Object type
        /// </summary>
        [EnumMember]
        ObjectType,

        /// <summary>
        /// Variable type
        /// </summary>
        [EnumMember]
        VariableType,

        /// <summary>
        /// Reference type
        /// </summary>
        [EnumMember]
        ReferenceType,

        /// <summary>
        /// Data type
        /// </summary>
        [EnumMember]
        DataType,

        /// <summary>
        /// View class
        /// </summary>
        [EnumMember]
        View
    }
}
