// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// The node type
    /// </summary>
    [DataContract]
    public enum NodeType
    {
        /// <summary>
        /// Unknown
        /// </summary>
        [EnumMember]
        Unknown,

        /// <summary>
        /// Variable
        /// </summary>
        [EnumMember]
        Variable,

        /// <summary>
        /// Data variable
        /// </summary>
        [EnumMember]
        DataVariable,

        /// <summary>
        /// Variable
        /// </summary>
        [EnumMember]
        Property,

        /// <summary>
        /// Data type
        /// </summary>
        [EnumMember]
        DataType,

        /// <summary>
        /// View class
        /// </summary>
        [EnumMember]
        View,

        /// <summary>
        /// Regular object type
        /// </summary>
        [EnumMember]
#pragma warning disable CA1720 // Identifier contains type name
        Object,
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Type is event type
        /// </summary>
        [EnumMember]
        Event,

        /// <summary>
        /// Interface type
        /// </summary>
        [EnumMember]
        Interface
    }
}
