// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
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
        [EnumMember(Value = "Unknown")]
        Unknown,

        /// <summary>
        /// Variable
        /// </summary>
        [EnumMember(Value = "Variable")]
        Variable,

        /// <summary>
        /// Data variable
        /// </summary>
        [EnumMember(Value = "DataVariable")]
        DataVariable,

        /// <summary>
        /// Variable
        /// </summary>
        [EnumMember(Value = "Property")]
        Property,

        /// <summary>
        /// Data type
        /// </summary>
        [EnumMember(Value = "DataType")]
        DataType,

        /// <summary>
        /// View class
        /// </summary>
        [EnumMember(Value = "View")]
        View,

        /// <summary>
        /// Regular object type
        /// </summary>
        [EnumMember(Value = "Object")]
#pragma warning disable CA1720 // Identifier contains type name
        Object,
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Type is event type
        /// </summary>
        [EnumMember(Value = "Event")]
        Event,

        /// <summary>
        /// Interface type
        /// </summary>
        [EnumMember(Value = "Interface")]
        Interface
    }
}
