// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Node class
    /// </summary>
    [DataContract]
    public enum NodeClass {

        /// <summary>
        /// Object class
        /// </summary>
        [EnumMember]
        Object,

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
