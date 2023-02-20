// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// The node type
    /// </summary>
    [DataContract]
    public enum NodeType {

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
        Object,

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
