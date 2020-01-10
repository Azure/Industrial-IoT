// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Node class
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeClass {

        /// <summary>
        /// Object class
        /// </summary>
        Object,

        /// <summary>
        /// Variable
        /// </summary>
        Variable,

        /// <summary>
        /// Method class
        /// </summary>
        Method,

        /// <summary>
        /// Object type
        /// </summary>
        ObjectType,

        /// <summary>
        /// Variable type
        /// </summary>
        VariableType,

        /// <summary>
        /// Reference type
        /// </summary>
        ReferenceType,

        /// <summary>
        /// Data type
        /// </summary>
        DataType,

        /// <summary>
        /// View class
        /// </summary>
        View
    }
}
